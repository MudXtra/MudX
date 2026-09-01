#!/usr/bin/env bash
set -Eeuo pipefail

script_directory=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
docker_bin=${DOCKER_BIN:-docker}
compose_file=${COMPOSE_FILE:-$script_directory/compose.yml}
env_file=${ENV_FILE:-$script_directory/mudx-docs.env}
lock_file=${LOCK_FILE:-/run/lock/mudx-docs-update.lock}
image_repository=${IMAGE_REPOSITORY:-ghcr.io/mudxtra/mudx/mudxdocwebsite}
stable_reference="$image_repository:stable"

candidate_env=""
next_env=""

cleanup() {
    rm -f \
        "${candidate_env:-}" \
        "${next_env:-}"
}
trap cleanup EXIT

fail() {
    echo "ERROR: $*" >&2
    exit 1
}

new_temporary_file() {
    local destination=$1
    local temporary
    temporary=$(mktemp "${destination}.tmp.XXXXXX")
    printf '%s\n' "$temporary"
}

read_single_value() {
    local key=$1
    local -a values
    mapfile -t values < <(sed -n "s/^${key}=//p" "$env_file")
    ((${#values[@]} == 1)) || fail "ENV_FILE must contain exactly one $key entry"
    printf '%s\n' "${values[0]}"
}

validate_immutable_reference() {
    local reference=$1
    local digest=${reference#"$image_repository"@}
    [[ "$reference" == "$image_repository@$digest" && "$digest" =~ ^sha256:[0-9a-f]{64}$ ]] ||
        fail "Image reference is not an immutable digest for $image_repository"
}

compose_up() {
    local selected_env=$1
    "$docker_bin" compose \
        --env-file "$selected_env" \
        -f "$compose_file" \
        up -d --wait
}

[[ -f "$compose_file" ]] || fail "Compose file not found: $compose_file"
[[ -f "$env_file" ]] || fail "Environment file not found: $env_file"
mkdir -p "$(dirname "$lock_file")"
exec 9>"$lock_file"
flock -n 9 || fail "Another MudX docs update is already running"

current_reference=$(read_single_value MUDX_DOCS_IMAGE)
recorded_current=$(read_single_value CURRENT_DIGEST)
previous_digest=$(read_single_value PREVIOUS_DIGEST)
validate_immutable_reference "$current_reference"
current_digest=${current_reference#*@}
[[ "$recorded_current" == "$current_digest" ]] || fail "CURRENT_DIGEST does not match MUDX_DOCS_IMAGE"
[[ -z "$previous_digest" || "$previous_digest" =~ ^sha256:[0-9a-f]{64}$ ]] || fail "PREVIOUS_DIGEST is invalid"

echo "Pulling the stable MudX docs image"
"$docker_bin" pull "$stable_reference"

repo_digests=$(
    "$docker_bin" image inspect \
        --format '{{range .RepoDigests}}{{println .}}{{end}}' \
        "$stable_reference"
)
candidate_reference=$(printf '%s\n' "$repo_digests" | awk -v prefix="$image_repository@sha256:" 'index($0, prefix) == 1 { print; exit }')
[[ -n "$candidate_reference" ]] || fail "Stable image did not resolve to an immutable repository digest"
validate_immutable_reference "$candidate_reference"
candidate_digest=${candidate_reference#*@}

if [[ "$candidate_digest" == "$current_digest" ]]; then
    echo "MudX docs is already running the stable digest; no update required"
    exit 0
fi

candidate_env=$(new_temporary_file "$env_file")
printf 'MUDX_DOCS_IMAGE=%s\nCURRENT_DIGEST=%s\nPREVIOUS_DIGEST=%s\n' \
    "$candidate_reference" "$candidate_digest" "$current_digest" >"$candidate_env"

echo "Deploying candidate digest $candidate_digest"
if ! compose_up "$candidate_env"; then
    echo "Candidate failed health validation; restoring prior digest $current_digest" >&2
    if ! compose_up "$env_file"; then
        fail "Candidate failed and rollback could not be confirmed healthy"
    fi
    fail "Candidate failed health validation; prior digest restored"
fi

if [[ ${MUDX_UPDATE_FAILPOINT:-} == after-health ]]; then
    fail "Injected interruption after candidate health succeeded"
fi

next_env=$(new_temporary_file "$env_file")
printf 'MUDX_DOCS_IMAGE=%s\nCURRENT_DIGEST=%s\nPREVIOUS_DIGEST=%s\n' \
    "$candidate_reference" "$candidate_digest" "$current_digest" >"$next_env"
mv "$next_env" "$env_file"

echo "MudX docs updated to digest $candidate_digest"
