#!/usr/bin/env bash
set -euo pipefail

# test.sh — Deploy BlazorPong to KIND and run Playwright tests
# Usage: ./test.sh
#
# Prerequisites:
#   - kind, kubectl, docker installed
#   - Node.js and npm installed (for Playwright)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../" && pwd)"
K8S_DIR="$SCRIPT_DIR"
TESTS_DIR="$REPO_ROOT/src/tests-e2e"
CLUSTER_NAME="blazorpong"

WEBAPP_PORT=6350
SIGNALR_PORT=6351

cleanup() {
  echo ""
  echo "▶ Cleaning up port-forward processes…"
  # Kill background port-forward processes
  kill "$PF_WEBAPP_PID" 2>/dev/null || true
  kill "$PF_SIGNALR_PID" 2>/dev/null || true
}
trap cleanup EXIT

echo "=== BlazorPong K8s Test Runner (KIND) ==="

# ── 1. Deploy (reuse deploy.sh) ──────────────────────────────────────────
echo "▶ Deploying to KIND cluster…"
bash "$K8S_DIR/deploy.sh"

# ── 2. Patch GameHubEndpoint for host-side browser access ─────────────────
# The webapp serves GameHubEndpoint to the Blazor WebAssembly client (browser).
# With port-forward, the browser runs on the host, so it needs localhost URLs.
echo "▶ Patching webapp GameHubEndpoint for port-forward access…"
kubectl set env deployment/webapp -n blazorpong \
  "GameHubEndpoint=http://localhost:${SIGNALR_PORT}/gamehub"

echo "▶ Waiting for webapp rollout after patching…"
kubectl rollout status deployment/webapp -n blazorpong --timeout=120s

# ── 3. Start port-forwarding ─────────────────────────────────────────────
echo ""
echo "▶ Starting port-forward for webapp (localhost:${WEBAPP_PORT} → svc/webapp:8080)…"
kubectl port-forward -n blazorpong svc/webapp "${WEBAPP_PORT}:8080" &
PF_WEBAPP_PID=$!

echo "▶ Starting port-forward for signalr (localhost:${SIGNALR_PORT} → svc/signalr:8080)…"
kubectl port-forward -n blazorpong svc/signalr "${SIGNALR_PORT}:8080" &
PF_SIGNALR_PID=$!

# Give port-forwards a moment to establish
sleep 3

# ── 4. Verify services are reachable ─────────────────────────────────────
echo "▶ Verifying webapp is reachable…"
for i in $(seq 1 30); do
  if curl -sf --max-time 10 "http://localhost:${WEBAPP_PORT}/" > /dev/null 2>&1; then
    echo "  ✅ webapp is reachable at http://localhost:${WEBAPP_PORT}"
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "  ❌ webapp is not reachable after 30 attempts"
    kubectl get pods -n blazorpong
    kubectl logs -n blazorpong -l app.kubernetes.io/name=webapp --tail=30
    exit 1
  fi
  sleep 2
done

echo "▶ Verifying signalr is reachable…"
for i in $(seq 1 30); do
  if curl -sf --max-time 10 "http://localhost:${SIGNALR_PORT}/" > /dev/null 2>&1; then
    echo "  ✅ signalr is reachable at http://localhost:${SIGNALR_PORT}"
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "  ❌ signalr is not reachable after 30 attempts"
    kubectl get pods -n blazorpong
    kubectl logs -n blazorpong -l app.kubernetes.io/name=signalr --tail=30
    exit 1
  fi
  sleep 2
done

# ── 5. Run Playwright tests ──────────────────────────────────────────────
echo ""
echo "▶ Running Playwright tests…"
cd "$TESTS_DIR"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
  npm install
fi

# Run tests with BASE_URL pointing to the port-forwarded webapp
BASE_URL="http://localhost:${WEBAPP_PORT}" npx playwright test

echo ""
echo "=== ✅ All tests passed ==="
