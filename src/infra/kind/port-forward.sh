#!/usr/bin/env bash
# port-forward.sh - Forward the Envoy Gateway proxy port to localhost:8080
set -euo pipefail

echo "=== Setting up port-forward to Envoy Gateway ==="

# Get the Envoy proxy service name in the envoy-gateway-system namespace
PROXY_SVC=$(kubectl get svc -n envoy-gateway-system \
  -l gateway.envoyproxy.io/owning-gateway-name=blazorpong-gateway \
  -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)

if [ -z "${PROXY_SVC}" ]; then
  echo "Gateway proxy service not found. Checking all namespaces..."
  PROXY_NS=$(kubectl get svc --all-namespaces \
    -l gateway.envoyproxy.io/owning-gateway-name=blazorpong-gateway \
    -o jsonpath='{.items[0].metadata.namespace}' 2>/dev/null || true)
  PROXY_SVC=$(kubectl get svc --all-namespaces \
    -l gateway.envoyproxy.io/owning-gateway-name=blazorpong-gateway \
    -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)
  if [ -z "${PROXY_SVC}" ]; then
    echo "ERROR: Could not find Envoy proxy service for blazorpong-gateway"
    echo "Available services:"
    kubectl get svc --all-namespaces
    exit 1
  fi
else
  PROXY_NS="envoy-gateway-system"
fi

echo "Forwarding ${PROXY_NS}/${PROXY_SVC}:8080 -> localhost:8080"
echo "Press Ctrl+C to stop."
kubectl port-forward -n "${PROXY_NS}" "svc/${PROXY_SVC}" 8080:8080
