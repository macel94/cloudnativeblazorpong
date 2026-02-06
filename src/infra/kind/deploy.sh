#!/usr/bin/env bash
# deploy.sh - Bootstrap a Kind cluster with Gateway API and deploy cloudnativeblazorpong
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLUSTER_NAME="${KIND_CLUSTER_NAME:-blazorpong}"

echo "=== Step 1: Create Kind cluster ==="
if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
  echo "Cluster '${CLUSTER_NAME}' already exists. Deleting..."
  kind delete cluster --name "${CLUSTER_NAME}"
fi
kind create cluster --name "${CLUSTER_NAME}" --config "${SCRIPT_DIR}/kind-config.yaml"

echo "=== Step 2: Pre-load container images ==="
echo "Pulling images on the Docker host and loading into Kind..."
IMAGES=(
  "redis:latest"
  "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-web:latest"
  "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-signalr:latest"
  "mcr.microsoft.com/mssql/server:2025-latest"
  "mcr.microsoft.com/mssql-tools:latest"
  "otel/opentelemetry-collector-contrib:latest"
)
for img in "${IMAGES[@]}"; do
  docker pull "${img}" || true
done
kind load docker-image --name "${CLUSTER_NAME}" "${IMAGES[@]}"

echo "=== Step 3: Install Gateway API CRDs ==="
kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.2.1/standard-install.yaml

echo "=== Step 4: Install Envoy Gateway ==="
# Use --server-side --force-conflicts to handle large CRDs and re-applies
kubectl apply --server-side --force-conflicts \
  -f https://github.com/envoyproxy/gateway/releases/download/v1.3.2/install.yaml
echo "Waiting for Envoy Gateway to be ready..."
kubectl wait --namespace envoy-gateway-system \
  deployment/envoy-gateway \
  --for=condition=Available \
  --timeout=300s

echo "=== Step 5: Apply Kubernetes manifests ==="
kubectl apply -f "${SCRIPT_DIR}/namespace.yaml"
kubectl apply -f "${SCRIPT_DIR}/redis.yaml"
kubectl apply -f "${SCRIPT_DIR}/azuresql.yaml"
kubectl apply -f "${SCRIPT_DIR}/collector.yaml"

echo "Waiting for Azure SQL to be ready..."
kubectl wait --namespace blazorpong \
  deployment/azuresql \
  --for=condition=Available \
  --timeout=180s

echo "=== Step 6: Run DB init job ==="
# Delete previous job if it exists (jobs are immutable)
kubectl delete job db-init --namespace blazorpong --ignore-not-found
kubectl apply -f "${SCRIPT_DIR}/db-init-job.yaml"
echo "Waiting for DB init job to complete..."
kubectl wait --namespace blazorpong \
  job/db-init \
  --for=condition=Complete \
  --timeout=300s

echo "=== Step 7: Deploy application services ==="
kubectl apply -f "${SCRIPT_DIR}/signalr.yaml"
kubectl apply -f "${SCRIPT_DIR}/webapp.yaml"

echo "Waiting for signalr to be ready..."
kubectl wait --namespace blazorpong \
  deployment/signalr \
  --for=condition=Available \
  --timeout=120s

echo "Waiting for webapp to be ready..."
kubectl wait --namespace blazorpong \
  deployment/webapp \
  --for=condition=Available \
  --timeout=120s

echo "=== Step 8: Deploy Gateway API resources ==="
kubectl apply -f "${SCRIPT_DIR}/gateway.yaml"

echo "Waiting for Gateway to be programmed..."
sleep 10
kubectl get gateway -n blazorpong

echo ""
echo "=== Deployment complete ==="
echo ""
echo "To access the app, run the port-forward script:"
echo "  ${SCRIPT_DIR}/port-forward.sh"
echo ""
echo "To run Playwright tests:"
echo "  ${SCRIPT_DIR}/run-tests.sh"
