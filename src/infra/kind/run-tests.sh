#!/usr/bin/env bash
# run-tests.sh - Run Playwright tests against the Kind cluster
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
TESTS_DIR="${REPO_ROOT}/src/tests-e2e"
BASE_URL="${BASE_URL:-http://localhost:8080}"

cleanup() {
  echo "Cleaning up port-forwards..."
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
    kubectl port-forward -n "${ns}" "svc/${SVC}" 8080:8080 &
    break
  fi
done

if [ -z "${PROXY_SVC}" ]; then
  echo "No Gateway proxy found. Port-forwarding services directly."
  kubectl port-forward -n blazorpong svc/webapp 8080:8080 &
  kubectl port-forward -n blazorpong svc/signalr 8081:8080 &
fi

# Wait for port-forward to be ready
echo "Waiting for port-forward..."
for i in $(seq 1 30); do
  if curl -sf "${BASE_URL}" > /dev/null 2>&1; then
    echo "Port-forward ready!"
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "ERROR: Port-forward did not become ready in time"
    kubectl get pods -n blazorpong
    exit 1
  fi
  sleep 1
done

echo "=== Running Playwright tests ==="
cd "${TESTS_DIR}"
npm install
npx playwright install --with-deps chrome
BASE_URL="${BASE_URL}" npx playwright test

echo "=== Tests complete ==="
