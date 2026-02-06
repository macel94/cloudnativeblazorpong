#!/usr/bin/env bash
# teardown.sh - Delete the Kind cluster
set -euo pipefail

CLUSTER_NAME="${KIND_CLUSTER_NAME:-blazorpong}"

echo "Deleting Kind cluster '${CLUSTER_NAME}'..."
kind delete cluster --name "${CLUSTER_NAME}"
echo "Done."
