#!/usr/bin/env bash
set -euo pipefail

# deploy.sh — Deploy BlazorPong to a local Kubernetes cluster using KIND
# Usage: ./deploy.sh
#
# Prerequisites:
#   - kind installed  (https://kind.sigs.k8s.io)
#   - kubectl installed
#   - docker installed

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR"
CLUSTER_NAME="blazorpong"

echo "=== BlazorPong Kubernetes Deployment (KIND) ==="

# ── 1. Create KIND cluster if not running ─────────────────────────────────
if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
  echo "✅ KIND cluster '${CLUSTER_NAME}' already exists"
else
  echo "▶ Creating KIND cluster '${CLUSTER_NAME}'…"
  kind create cluster --name "$CLUSTER_NAME" --config "$K8S_DIR/kind-config.yaml" --wait 120s
fi

# ── 2. Pre-pull and load images into KIND ─────────────────────────────────
# KIND runs its own containerd, so images must be loaded from the host Docker daemon.
IMAGES=(
  "redis:latest"
  "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-web:latest"
  "ghcr.io/macel94/cloudnativeblazorpong/blazorpong-signalr:latest"
  "mcr.microsoft.com/mssql/server:2025-latest"
  "mcr.microsoft.com/dotnet/sdk:8.0"
  "otel/opentelemetry-collector-contrib:latest"
  "grafana/tempo:latest"
  "grafana/loki:latest"
  "prom/prometheus:latest"
  "grafana/grafana:latest"
)

echo "▶ Pulling images on host and loading into KIND…"
for img in "${IMAGES[@]}"; do
  docker pull "$img" -q 2>/dev/null || echo "  ⚠️  Could not pull $img (may already be cached)"
done

kind load docker-image --name "$CLUSTER_NAME" "${IMAGES[@]}"

# ── 3. Install Gateway API CRDs ───────────────────────────────────────────
echo "▶ Installing Gateway API CRDs…"
kubectl apply -f https://github.com/kubernetes-sigs/gateway-api/releases/download/v1.2.1/standard-install.yaml 2>/dev/null || {
  echo "⚠️  Could not install Gateway API CRDs (no internet?). Gateway resources will be skipped."
}

# ── 4. Apply all manifests using kustomize ────────────────────────────────
echo "▶ Applying Kubernetes manifests…"
kubectl apply -k "$K8S_DIR"

# ── 5. Wait for data stores to be ready ──────────────────────────────────
echo "▶ Waiting for Redis to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/redis --timeout=120s

echo "▶ Waiting for Azure SQL to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/azuresql --timeout=300s

# ── 6. Wait for DB init job to complete ───────────────────────────────────
echo "▶ Waiting for DB init job to complete…"
kubectl wait --namespace=blazorpong --for=condition=complete job/db-init --timeout=600s || {
  echo "⚠️  DB init job may still be running. Checking status…"
  kubectl get jobs -n blazorpong
  kubectl logs -n blazorpong job/db-init --tail=50
}

# ── 7. Wait for observability stack ──────────────────────────────────────
echo "▶ Waiting for observability stack…"
for dep in tempo loki prometheus collector grafana; do
  kubectl wait --namespace=blazorpong --for=condition=available "deployment/$dep" --timeout=120s
done

# ── 8. Wait for application ──────────────────────────────────────────────
echo "▶ Waiting for SignalR to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/signalr --timeout=180s

echo "▶ Waiting for Webapp to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/webapp --timeout=180s

# ── 9. Show status ───────────────────────────────────────────────────────
echo ""
echo "=== Deployment Status ==="
kubectl get all -n blazorpong

echo ""
echo "=== Access the application ==="
echo "Option 1 — port-forward (simplest):"
echo "  kubectl port-forward -n blazorpong svc/webapp 6350:8080 &"
echo "  kubectl port-forward -n blazorpong svc/signalr 6351:8080 &"
echo "  open http://localhost:6350"
echo ""
echo "Option 2 — run the Playwright tests:"
echo "  ./test.sh"
