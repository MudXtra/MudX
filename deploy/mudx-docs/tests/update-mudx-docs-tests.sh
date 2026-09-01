#!/usr/bin/env bash
set -Eeuo pipefail

repository_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)
update_script="$repository_root/deploy/mudx-docs/update-mudx-docs.sh"
image_repository="ghcr.io/mudxtra/mudx/mudxdocwebsite"
old_digest="sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
previous_digest="sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
new_digest="sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
secret_marker="must-not-appear-in-output"

fail() { echo "FAIL: $*" >&2; exit 1; }

assert_authority() {
    local fixture=$1 current=$2 previous=$3
    local expected="MUDX_DOCS_IMAGE=$image_repository@$current
CURRENT_DIGEST=$current
PREVIOUS_DIGEST=$previous"
    [[ -f "$fixture/mudx-docs.env" ]] || fail "missing authoritative env file"
    [[ "$(cat "$fixture/mudx-docs.env")" == "$expected" ]] || fail "authoritative env record mismatch"
    ! find "$fixture" -type f -name '*.tmp.*' -print -quit | grep -q . || fail "temporary file remained"
}

create_fixture() {
    local fixture
    fixture=$(mktemp -d)
    mkdir -p "$fixture/bin"
    : >"$fixture/compose.yml"
    printf 'MUDX_DOCS_IMAGE=%s@%s\nCURRENT_DIGEST=%s\nPREVIOUS_DIGEST=%s\n' \
        "$image_repository" "$old_digest" "$old_digest" "$previous_digest" >"$fixture/mudx-docs.env"
    cat >"$fixture/bin/docker" <<'FAKE_DOCKER'
#!/usr/bin/env bash
set -Eeuo pipefail
echo "$*" >>"$FAKE_DOCKER_LOG"
if [[ "$1" == pull ]]; then
    [[ "$FAKE_DOCKER_SCENARIO" != pull-failure ]] || exit 42
    exit 0
fi
if [[ "$1" == image && "$2" == inspect ]]; then
    case "$FAKE_DOCKER_SCENARIO" in
        missing-digest) exit 0 ;;
        invalid-digest) printf '%s\n' "$FAKE_IMAGE_REPOSITORY:stable" ;;
        *) printf '%s@%s\n' "$FAKE_IMAGE_REPOSITORY" "$FAKE_NEW_DIGEST" ;;
    esac
    exit 0
fi
if [[ "$1" == compose ]]; then
    env_file=""
    shift
    while (($#)); do
        if [[ "$1" == --env-file ]]; then env_file=$2; shift 2; continue; fi
        shift
    done
    [[ -n "$env_file" ]] || exit 43
    deployed_image=$(sed -n 's/^MUDX_DOCS_IMAGE=//p' "$env_file")
    printf 'deployed=%s\n' "$deployed_image" >>"$FAKE_DOCKER_LOG"
    if [[ "$FAKE_DOCKER_SCENARIO" == health-failure && "$deployed_image" == *"@$FAKE_NEW_DIGEST" && ! -f "$FAKE_HEALTH_FAILED" ]]; then
        : >"$FAKE_HEALTH_FAILED"
        exit 44
    fi
    exit 0
fi
exit 45
FAKE_DOCKER
    chmod +x "$fixture/bin/docker"
    printf '%s\n' "$fixture"
}

run_update() {
    local fixture=$1 scenario=$2 candidate=$3 output=$4 failpoint=${5:-}
    FAKE_DOCKER_SCENARIO="$scenario" FAKE_DOCKER_LOG="$fixture/docker.log" \
    FAKE_IMAGE_REPOSITORY="$image_repository" FAKE_NEW_DIGEST="$candidate" \
    FAKE_HEALTH_FAILED="$fixture/health-failed" FAKE_SECRET_VALUE="$secret_marker" \
    DOCKER_BIN="$fixture/bin/docker" COMPOSE_FILE="$fixture/compose.yml" \
    ENV_FILE="$fixture/mudx-docs.env" LOCK_FILE="$fixture/update.lock" \
    IMAGE_REPOSITORY="$image_repository" MUDX_UPDATE_FAILPOINT="$failpoint" \
    "$update_script" >"$output" 2>&1
}

test_same_digest_no_op() {
    local fixture output
    fixture=$(create_fixture); output="$fixture/output.log"
    run_update "$fixture" same-digest "$old_digest" "$output" || fail "same digest should succeed"
    assert_authority "$fixture" "$old_digest" "$previous_digest"
    ! grep -q '^compose ' "$fixture/docker.log" || fail "same digest invoked compose"
    ! grep -q "$secret_marker" "$output" || fail "output leaked secret"
    rm -rf "$fixture"
}

test_successful_promotion() {
    local fixture output
    fixture=$(create_fixture); output="$fixture/output.log"
    run_update "$fixture" success "$new_digest" "$output" || fail "promotion should succeed"
    assert_authority "$fixture" "$new_digest" "$old_digest"
    grep -q "deployed=$image_repository@$new_digest" "$fixture/docker.log" || fail "candidate was not deployed"
    rm -rf "$fixture"
}

test_failure_preserves_authority() {
    local scenario
    for scenario in pull-failure health-failure missing-digest invalid-digest; do
        local fixture output
        fixture=$(create_fixture); output="$fixture/output.log"
        if run_update "$fixture" "$scenario" "$new_digest" "$output"; then fail "$scenario unexpectedly succeeded"; fi
        assert_authority "$fixture" "$old_digest" "$previous_digest"
        if [[ "$scenario" == health-failure ]]; then
            grep -q "deployed=$image_repository@$old_digest" "$fixture/docker.log" || fail "rollback was not deployed"
        fi
        rm -rf "$fixture"
    done
}

test_post_health_interruption_recovers() {
    local fixture output
    fixture=$(create_fixture); output="$fixture/output.log"
    if run_update "$fixture" success "$new_digest" "$output" after-health; then fail "fault injection unexpectedly succeeded"; fi
    assert_authority "$fixture" "$old_digest" "$previous_digest"
    grep -q "deployed=$image_repository@$new_digest" "$fixture/docker.log" || fail "candidate did not become healthy before interruption"
    run_update "$fixture" success "$new_digest" "$output" || fail "recovery run failed"
    assert_authority "$fixture" "$new_digest" "$old_digest"
    rm -rf "$fixture"
}

test_lock_serializes_runs() {
    local fixture output holder
    fixture=$(create_fixture); output="$fixture/output.log"
    flock "$fixture/update.lock" -c 'sleep 30' & holder=$!
    sleep 0.2
    if run_update "$fixture" success "$new_digest" "$output"; then kill "$holder" 2>/dev/null || true; fail "concurrent update unexpectedly succeeded"; fi
    kill "$holder" 2>/dev/null || true
    wait "$holder" 2>/dev/null || true
    assert_authority "$fixture" "$old_digest" "$previous_digest"
    [[ ! -f "$fixture/docker.log" ]] || fail "locked invocation reached Docker"
    rm -rf "$fixture"
}

test_invalid_current_repository() {
    local fixture output
    fixture=$(create_fixture); output="$fixture/output.log"
    sed -i 's/^MUDX_DOCS_IMAGE=ghcr\.io/MUDX_DOCS_IMAGE=ghcrXio/' "$fixture/mudx-docs.env"
    if run_update "$fixture" success "$new_digest" "$output"; then fail "invalid repository unexpectedly succeeded"; fi
    rm -rf "$fixture"
}

[[ -x "$update_script" ]] || fail "missing executable updater"
test_same_digest_no_op
test_successful_promotion
test_failure_preserves_authority
test_post_health_interruption_recovers
test_lock_serializes_runs
test_invalid_current_repository
echo "PASS update script contract"
