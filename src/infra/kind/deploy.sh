#!/usr/bin/env bash
# deploy.sh - Bootstrap a Kind cluster and deploy cloudnativeblazorpong
# Usage: ./deploy.sh [--skip-cluster] [--skip-images]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_NAME="${KIND_CLUSTER_NAME:-blazorpong}"
SKIP_CLUSTER=false
SKIP_IMAGES=false

for arg in "$@"; do
  case "$arg" in
    --skip-cluster) SKIP_CLUSTER=true ;;
    --skip-images)  SKIP_IMAGES=true ;;
  esac
done

# --- Step 1: Create Kind cluster ---
if [ "$SKIP_CLUSTER" = false ]; then
  echo "=== Step 1: Create Kind cluster ==="
  if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
    echo "Cluster '${CLUSTER_NAME}' already exists. Deleting..."
    kind delete cluster --name "${CLUSTER_NAME}"
  fi
  kind create cluster --name "${CLUSTER_NAME}" --config "${SCRIPT_DIR}/kind-config.yaml"
else
  echo "=== Step 1: Skipping cluster creation ==="
fi

# --- Step 2: Pre-load container images ---
if [ "$SKIP_IMAGES" = false ]; then
  echo "=== Step 2: Pre-load container images ==="
  IMAGES=(
    "redis:latest"
    "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-web:latest"
    "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-signalr:latest"
    "mcr.microsoft.com/mssql/server:2025-latest"
    "mcr.microsoft.com/mssql-tools:latest"
    "otel/opentelemetry-collector-contrib:latest"
  )
  for img in "${IMAGES[@]}"; do
    docker pull "${img}" 2>&1 | tail -1 || true
  done
  kind load docker-image --name "${CLUSTER_NAME}" "${IMAGES[@]}"
else
  echo "=== Step 2: Skipping image loading ==="
fi

# --- Step 3: Apply core manifests ---
echo "=== Step 3: Apply Kubernetes manifests ==="
kubectl apply -f "${SCRIPT_DIR}/namespace.yaml"
kubectl apply -f "${SCRIPT_DIR}/redis.yaml"
kubectl apply -f "${SCRIPT_DIR}/azuresql.yaml"
kubectl apply -f "${SCRIPT_DIR}/collector.yaml"

echo "Waiting for Redis..."
kubectl wait --namespace blazorpong deployment/redis --for=condition=Available --timeout=120s

echo "Waiting for Azure SQL to be ready..."
kubectl wait --namespace blazorpong deployment/azuresql --for=condition=Available --timeout=180s

# --- Step 4: Run DB init job ---
echo "=== Step 4: Run DB init job ==="
kubectl delete job db-init --namespace blazorpong --ignore-not-found
kubectl apply -f "${SCRIPT_DIR}/db-init-job.yaml"
echo "Waiting for DB init job to complete (this downloads sqlpackage, may take a few minutes)..."
kubectl wait --namespace blazorpong job/db-init --for=condition=Complete --timeout=600s

# --- Step 5: Deploy application services ---
echo "=== Step 5: Deploy application services ==="
kubectl apply -f "${SCRIPT_DIR}/signalr.yaml"
kubectl apply -f "${SCRIPT_DIR}/webapp.yaml"

echo "Waiting for signalr to be ready..."
kubectl wait --namespace blazorpong deployment/signalr --for=condition=Available --timeout=120s

echo "Waiting for webapp to be ready..."
kubectl wait --namespace blazorpong deployment/webapp --for=condition=Available --timeout=120s

# --- Step 6: Optionally deploy Gateway API ---
echo "=== Step 6: Deploy Gateway API resources (optional) ==="
echo "Attempting to install Gateway API CRDs and Envoy Gateway..."
if kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.2.1/standard-install.yaml 2>/dev/null; then
  if kubectl apply --server-side --force-conflicts \
    -f https://github.com/envoyproxy/gateway/releases/download/v1.3.2/install.yaml 2>/dev/null; then
    echo "Waiting for Envoy Gateway..."
    if kubectl wait --namespace envoy-gateway-system deployment/envoy-gateway \
      --for=condition=Available --timeout=300s 2>/dev/null; then
      kubectl apply -f "${SCRIPT_DIR}/gateway.yaml"
      echo "Gateway API deployed successfully."
    else
      echo "WARNING: Envoy Gateway did not become ready. Skipping Gateway API."
    fi
  else
    echo "WARNING: Could not install Envoy Gateway. Skipping Gateway API."
  fi
else
  echo "WARNING: Could not install Gateway API CRDs. Skipping Gateway API."
fi

echo ""
echo "=== Deployment complete ==="
echo ""
echo "All pods:"
kubectl get pods -n blazorpong
echo ""
echo "All services:"
kubectl get svc -n blazorpong
echo ""
echo "To access the app, run the port-forward script:"
echo "  ${SCRIPT_DIR}/port-forward.sh"
echo ""
echo "To run Playwright tests:"
echo "  ${SCRIPT_DIR}/run-tests.sh"
