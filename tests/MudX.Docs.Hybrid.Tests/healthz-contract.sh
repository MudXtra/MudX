#!/usr/bin/env bash
set -Eeuo pipefail

repository_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
dotnet_bin=${DOTNET_BIN:-dotnet}
port=${MUDX_HEALTH_TEST_PORT:-5187}
base_url="http://127.0.0.1:${port}"
log_file=$(mktemp)
body_file=$(mktemp)

cleanup() {
    if [[ -n "${host_pid:-}" ]]; then
        kill "$host_pid" 2>/dev/null || true
        wait "$host_pid" 2>/dev/null || true
    fi
    rm -f "$log_file" "$body_file"
}
trap cleanup EXIT

"$dotnet_bin" run \
    --project "$repository_root/src/MudX.Docs.Hybrid/MudX.Docs.Hybrid/MudX.Docs.Hybrid.csproj" \
    --no-build \
    --no-restore \
    --urls "$base_url" >"$log_file" 2>&1 &
host_pid=$!

ready=false
for _ in $(seq 1 60); do
    if curl --silent --output /dev/null "$base_url/"; then
        ready=true
        break
    fi
    if ! kill -0 "$host_pid" 2>/dev/null; then
        break
    fi
    sleep 0.25
done

if [[ "$ready" != true ]]; then
    echo "Docs hybrid host did not become ready" >&2
    sed -n '1,200p' "$log_file" >&2
    exit 1
fi

status=$(curl --silent --show-error --output "$body_file" --write-out '%{http_code}' "$base_url/healthz")
if [[ "$status" != "200" ]]; then
    echo "Expected /healthz HTTP 200, received $status" >&2
    exit 1
fi

if [[ "$(cat "$body_file")" != "Healthy" ]]; then
    echo "Expected /healthz body 'Healthy'" >&2
    exit 1
fi

echo "PASS /healthz returned HTTP 200 with body Healthy"
