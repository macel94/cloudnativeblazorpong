#!/usr/bin/env bash
set -euo pipefail

# deploy.sh — Deploy BlazorPong to a local Kubernetes cluster (minikube)
# Usage: ./deploy.sh
#
# Prerequisites:
#   - minikube installed
#   - kubectl installed

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR"

echo "=== BlazorPong Kubernetes Deployment ==="

# ── 1. Start minikube if not running ──────────────────────────────────────
if ! minikube status --format='{{.Host}}' 2>/dev/null | grep -q Running; then
  echo "▶ Starting minikube…"
  minikube start --driver=docker --memory=4096 --cpus=2
else
  echo "✅ minikube is already running"
fi

# ── 2. Apply all manifests using kustomize ────────────────────────────────
echo "▶ Applying Kubernetes manifests…"
kubectl apply -k "$K8S_DIR"

# ── 3. Wait for data stores to be ready ──────────────────────────────────
echo "▶ Waiting for Redis to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/redis --timeout=120s

echo "▶ Waiting for Azure SQL to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/azuresql --timeout=180s

# ── 4. Wait for DB init job to complete ───────────────────────────────────
echo "▶ Waiting for DB init job to complete…"
kubectl wait --namespace=blazorpong --for=condition=complete job/db-init --timeout=300s || {
  echo "⚠️ DB init job may still be running. Checking status…"
  kubectl get jobs -n blazorpong
  kubectl logs -n blazorpong job/db-init --tail=50
}

# ── 5. Wait for observability stack ──────────────────────────────────────
echo "▶ Waiting for observability stack…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/tempo --timeout=120s
kubectl wait --namespace=blazorpong --for=condition=available deployment/loki --timeout=120s
kubectl wait --namespace=blazorpong --for=condition=available deployment/prometheus --timeout=120s
kubectl wait --namespace=blazorpong --for=condition=available deployment/collector --timeout=120s
kubectl wait --namespace=blazorpong --for=condition=available deployment/grafana --timeout=120s

# ── 6. Wait for application ──────────────────────────────────────────────
echo "▶ Waiting for SignalR to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/signalr --timeout=180s

echo "▶ Waiting for Webapp to be ready…"
kubectl wait --namespace=blazorpong --for=condition=available deployment/webapp --timeout=180s

# ── 7. Show status ───────────────────────────────────────────────────────
echo ""
echo "=== Deployment Status ==="
kubectl get all -n blazorpong

echo ""
echo "=== Access the application ==="
echo "Run: kubectl port-forward -n blazorpong svc/webapp 6350:8080"
echo "Then open: http://localhost:6350"
echo ""
echo "Or use minikube tunnel for Gateway API access"
