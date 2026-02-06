#!/usr/bin/env bash
# port-forward.sh - Forward webapp and signalr services to localhost
# This sets up two port-forwards:
#   - webapp  -> localhost:8080 (main web app)
#   - signalr -> localhost:8081 (for direct SignalR access)
# If Gateway API is deployed, it forwards the Gateway proxy instead.
set -euo pipefail

cleanup() {
  echo ""
  echo "Stopping port-forwards..."
  jobs -p | xargs -r kill 2>/dev/null || true
}
trap cleanup EXIT

echo "=== Setting up port-forwards ==="

# Try Gateway proxy first
PROXY_SVC=""
for ns in envoy-gateway-system blazorpong; do
  SVC=$(kubectl get svc -n "${ns}" \
    -l gateway.envoyproxy.io/owning-gateway-name=blazorpong-gateway \
    -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)
  if [ -n "${SVC}" ]; then
    PROXY_SVC="${SVC}"
    echo "Found Gateway proxy: ${ns}/${SVC}"
    echo "Forwarding ${ns}/${SVC}:8080 -> localhost:8080"
    kubectl port-forward -n "${ns}" "svc/${SVC}" 8080:8080 &
    echo "Press Ctrl+C to stop."
    wait
    exit 0
  fi
done

# Fallback: forward webapp and signalr directly
echo "No Gateway proxy found. Forwarding services directly."
echo "  webapp:8080  -> localhost:8080"
echo "  signalr:8080 -> localhost:8081"

kubectl port-forward -n blazorpong svc/webapp 8080:8080 &
kubectl port-forward -n blazorpong svc/signalr 8081:8080 &

echo "Press Ctrl+C to stop."
wait
