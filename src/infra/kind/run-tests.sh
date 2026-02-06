#!/usr/bin/env bash
# run-tests.sh - Run Playwright tests against the Kind cluster
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
TESTS_DIR="${REPO_ROOT}/src/tests-e2e"
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "=== Setting up port-forward in background ==="

# Find the Envoy proxy service
PROXY_SVC=""
PROXY_NS="envoy-gateway-system"

for ns in envoy-gateway-system blazorpong; do
  SVC=$(kubectl get svc -n "${ns}" \
    -l gateway.envoyproxy.io/owning-gateway-name=blazorpong-gateway \
    -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)
  if [ -n "${SVC}" ]; then
    PROXY_SVC="${SVC}"
    PROXY_NS="${ns}"
    break
  fi
done

if [ -z "${PROXY_SVC}" ]; then
  echo "ERROR: Could not find Envoy proxy service"
  echo "Available services:"
  kubectl get svc --all-namespaces
  exit 1
fi

# Start port-forward in background
kubectl port-forward -n "${PROXY_NS}" "svc/${PROXY_SVC}" 8080:8080 &
PF_PID=$!
trap 'kill ${PF_PID} 2>/dev/null || true' EXIT

# Wait for port-forward to be ready
echo "Waiting for port-forward..."
for i in $(seq 1 30); do
  if curl -sf "${BASE_URL}" > /dev/null 2>&1; then
    echo "Port-forward ready!"
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "ERROR: Port-forward did not become ready in time"
    exit 1
  fi
  sleep 1
done

echo "=== Running Playwright tests ==="
cd "${TESTS_DIR}"
npm install
npx playwright install --with-deps chromium
BASE_URL="${BASE_URL}" npx playwright test

echo "=== Tests complete ==="
