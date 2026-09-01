#!/usr/bin/env bash
set -Eeuo pipefail

repository_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
release_workflow="$repository_root/.github/workflows/release.yml"
prepare_workflow="$repository_root/.github/workflows/prepare-release.yml"
build_workflow="$repository_root/.github/workflows/Build_And_Test.yml"
docs_dockerfile="$repository_root/src/MudX.Docs.Hybrid/MudX.Docs.Hybrid/Dockerfile"
solution_file="$repository_root/src/MudX.slnx"

fail() {
    echo "FAIL $current_test: $1" >&2
    return 1
}

step_line() {
    local name=$1
    grep -nFx "      - name: $name" "$release_workflow" | cut -d: -f1
}

step_body() {
    local name=$1
    awk -v marker="      - name: $name" '
        $0 == marker { found = 1; next }
        found && /^      - name:/ { exit }
        found { print }
    ' "$release_workflow"
}

step_script() {
    local name=$1
    step_body "$name" | awk '
        /^        run: \|$/ { found = 1; next }
        found { sub(/^          /, ""); print }
    '
}

job_body() {
    local workflow=$1 name=$2
    awk -v marker="  $name:" '
        $0 == marker { found = 1; next }
        found && /^  [[:alnum:]_-]+:$/ { exit }
        found { print }
    ' "$workflow"
}

require_contains() {
    local text=$1 expected=$2 message=$3
    grep -Fq -- "$expected" <<<"$text" || fail "$message"
}

require_not_contains() {
    local text=$1 rejected=$2 message=$3
    if grep -Fq -- "$rejected" <<<"$text"; then
        fail "$message"
    fi
}

test_guard_order() {
    local guard create image assets main symbols stable notes finalize
    guard=$(step_line "Guard stable against historical release retries")
    create=$(step_line "Create missing draft GitHub release")
    image=$(step_line "Verify or publish provenance-bound image")
    assets=$(step_line "Upload or repair draft assets only")
    main=$(step_line "Publish missing NuGet main package")
    symbols=$(step_line "Publish missing NuGet symbol package")
    stable=$(step_line "Promote verified digest to stable")
    notes=$(step_line "Record idempotent checksums and provenance in draft notes")
    finalize=$(step_line "Finalize draft GitHub release last")
    for mutation in "$create" "$image" "$assets" "$main" "$symbols" "$stable" "$notes" "$finalize"; do
        (( guard < mutation )) || fail "historical-release rejection must precede every publication mutation"
    done
}

test_prepare_merge_identity() {
    local workflow name_config email_config merge
    workflow=$(<"$prepare_workflow")
    name_config=$(grep -nF 'git config user.name "github-actions[bot]"' "$prepare_workflow" | head -n 1 | cut -d: -f1 || true)
    email_config=$(grep -nF 'git config user.email "41898282+github-actions[bot]@users.noreply.github.com"' "$prepare_workflow" | head -n 1 | cut -d: -f1 || true)
    merge=$(grep -nF 'git merge --no-edit origin/dev' "$prepare_workflow" | head -n 1 | cut -d: -f1 || true)
    [[ -n "$name_config" && -n "$email_config" && -n "$merge" ]] ||
        fail "prepare workflow must configure the release bot identity and support resumed-branch merges"
    (( name_config < merge && email_config < merge )) ||
        fail "release bot identity must be configured before a resumed branch can create a merge commit"
    require_contains "$workflow" 'git merge --no-edit origin/dev' "prepare workflow must retain resumed-branch merging"
}

test_exact_release_sdk() {
    local workflow body
    for workflow in "$prepare_workflow" "$release_workflow"; do
        body=$(<"$workflow")
        require_contains "$body" 'dotnet-version: 10.0.400' "release-producing workflows must pin the supported SDK exactly"
        require_not_contains "$body" 'dotnet-version: 10.0.x' "release-producing workflows must not float across servicing SDKs"
    done
}

test_exact_draft_assets() {
    local body
    body=$(step_body "Upload or repair draft assets only")
    require_contains "$body" 'releases/$release_id/assets?per_page=100' "draft asset inventory must enumerate every remote asset"
    require_contains "$body" '--method DELETE' "unexpected draft assets must be removed"
    require_contains "$body" 'diff --unified' "draft assets must be verified as an exact set after upload"
    require_not_contains "$body" '|| true' "draft asset API failures must not be converted to success"
}

test_draft_asset_api_failure_runtime() (
    set -Eeuo pipefail
    local fixture status
    fixture=$(mktemp -d)
    trap 'rm -rf -- "$fixture"' EXIT
    mkdir -p "$fixture/bin" "$fixture/release-artifacts/release-assets"
    : >"$fixture/release-artifacts/release-assets/MudX.9.9.0.nupkg"

    printf '%s\n' \
        '#!/usr/bin/env bash' \
        'set -Eeuo pipefail' \
        'if [[ "$1" == api && "$2" == repos/MudXtra/MudX/releases/tags/v9.9.0 ]]; then' \
        '    printf "123\n"' \
        'elif [[ "$1" == api && "$2" == --paginate ]]; then' \
        '    echo "simulated asset inventory API failure" >&2' \
        '    exit 42' \
        'elif [[ "$1" == release && "$2" == upload ]]; then' \
        '    exit 0' \
        'else' \
        '    printf "unexpected gh invocation: %s\n" "$*" >&2' \
        '    exit 43' \
        'fi' \
        >"$fixture/bin/gh"
    chmod +x "$fixture/bin/gh"

    set +e
    step_script "Upload or repair draft assets only" | (cd "$fixture" && env \
        PATH="$fixture/bin:$PATH" \
        GITHUB_REPOSITORY=MudXtra/MudX \
        TAG=v9.9.0 \
        bash -s) >"$fixture/step.out" 2>"$fixture/step.err"
    status=$?
    set -e

    [[ "$status" -eq 42 ]] ||
        fail "draft asset inventory API failure must abort with its failing status, received $status"
    grep -Fq 'simulated asset inventory API failure' "$fixture/step.err" ||
        fail "draft asset inventory failed for an unexpected reason"
)

test_guard_drafts() {
    local body
    body=$(step_body "Guard stable against historical release retries")
    require_contains "$body" '.prerelease == false' "guard must exclude prereleases"
    require_not_contains "$body" '.draft == false' "guard must include stable-SemVer draft releases"
}

test_no_op_stable() {
    local body
    body=$(step_body "Verify published release and no-op")
    require_contains "$body" 'docker://$IMAGE_REPOSITORY:sha-$MERGE_SHA' "published no-op must verify the immutable commit tag"
    require_contains "$body" 'docker://$IMAGE_REPOSITORY:$VERSION' "published no-op must verify the immutable version tag"
    require_not_contains "$body" 'docker://$IMAGE_REPOSITORY:stable' "historical published no-op must not require mutable stable"
}

test_reproducible_oci() {
    local body dockerfile
    body=$(step_body "Build exact-SHA OCI image with provenance and SBOM")
    dockerfile=$(<"$docs_dockerfile")
    require_contains "$body" 'source_date_epoch=' "OCI build must derive a stable source timestamp"
    require_contains "$body" 'SOURCE_DATE_EPOCH' "OCI build must pass SOURCE_DATE_EPOCH"
    require_contains "$body" 'rewrite-timestamp=true' "OCI exporter must normalize layer timestamps"
    require_contains "$dockerfile" 'ARG SOURCE_DATE_EPOCH=0' "local Docker builds need a deterministic timestamp default"
    require_contains "$dockerfile" 'staticwebassets.endpoints.json' "docs publish must normalize generated endpoint metadata"
    require_contains "$dockerfile" 'Last-Modified' "docs publish must replace volatile endpoint timestamps"
}

test_final_head_approval() {
    local body
    body=$(step_body "Fail closed unless final review decision is approved")
    require_contains "$body" 'PR_HEAD_SHA' "approval gate must receive the final PR head SHA"
    require_contains "$body" '/reviews' "approval gate must inspect individual reviews"
    require_contains "$body" '.commit_id' "approval gate must bind approval to a commit"
}

test_nuget_content_types() {
    local initial after
    initial=$(step_body "Check NuGet main and symbol package identities")
    after=$(step_body "Verify NuGet main and symbol identities after publication")
    for body in "$initial" "$after"; do
        require_contains "$body" 'xml.etree.ElementTree' "NuGet comparison must parse content-type declarations"
        require_contains "$body" "'[Content_Types].xml'" "NuGet comparison must retain normalized content types"
        require_not_contains "$body" "n not in {'.signature.p7s', '[Content_Types].xml'}" "NuGet comparison must not discard the entire content-types document"
    done
}

test_immutable_inspection_errors() {
    local body
    body=$(step_body "Verify or publish provenance-bound image")
    require_contains "$body" 'manifest unknown' "immutable tag inspection must recognize confirmed manifest absence"
    require_contains "$body" 'name unknown' "immutable tag inspection must recognize confirmed repository absence"
    require_not_contains "$body" '2>/dev/null || true' "immutable tag inspection must not convert arbitrary errors to absence"
}

test_reject_prerelease() {
    local body
    body=$(step_body "Detect draft versus published GitHub release")
    require_contains "$body" '.prerelease' "published release detection must inspect prerelease state"
    require_contains "$body" 'existing release must not be marked prerelease' "every existing stable release must reject prerelease metadata"
}

test_prerelease_draft_runtime() (
    set -Eeuo pipefail
    local fixture
    fixture=$(mktemp -d)
    trap 'rm -rf -- "$fixture"' EXIT
    mkdir -p "$fixture/bin"
    : >"$fixture/github-output"

    printf '%s\n' \
        '#!/usr/bin/env bash' \
        'set -Eeuo pipefail' \
        'output=' \
        'while (($#)); do' \
        '    case "$1" in' \
        '        --output) output=$2; shift 2 ;;' \
        '        *) shift ;;' \
        '    esac' \
        'done' \
        '[[ -n "$output" ]]' \
        'printf "%s" "$STUB_GITHUB_RESPONSE" >"$output"' \
        'printf "200"' \
        >"$fixture/bin/curl"
    chmod +x "$fixture/bin/curl"

    if step_script "Detect draft versus published GitHub release" | env \
        PATH="$fixture/bin:$PATH" \
        GH_TOKEN=fixture-token \
        GITHUB_REPOSITORY=MudXtra/MudX \
        TAG=v9.9.0 \
        MERGE_SHA=fixture-merge-sha \
        GITHUB_OUTPUT="$fixture/github-output" \
        STUB_GITHUB_RESPONSE='{"draft":true,"prerelease":true,"target_commitish":"fixture-merge-sha"}' \
        bash -s >"$fixture/detect.out" 2>"$fixture/detect.err"; then
        fail "prerelease draft was accepted: $(<"$fixture/github-output")"
    fi

    grep -Fq 'existing release must not be marked prerelease' "$fixture/detect.err" ||
        fail "prerelease draft failed for the wrong reason: $(tail -n 1 "$fixture/detect.err")"
    [[ ! -s "$fixture/github-output" ]] ||
        fail "prerelease draft wrote state before rejection"
)

test_portable_tar_reader() {
    local body
    body=$(step_body "Build exact-SHA OCI image with provenance and SBOM")
    require_not_contains "$body" 'extractall(' "OCI reader must not depend on version-sensitive extractall filters"
    require_contains "$body" 'extractfile(' "OCI reader must read validated archive members without extracting"
}

test_oci_reader_runtime() (
    set -Eeuo pipefail
    local fixture archive_root nested_digest application_digest config_digest
    fixture=$(mktemp -d)
    trap 'rm -rf -- "$fixture"' EXIT
    archive_root="$fixture/archive"
    mkdir -p "$archive_root/blobs/sha256" "$fixture/release-artifacts/release-assets"

    config_digest="sha256:$(printf 'c%.0s' {1..64})"
    printf '{"schemaVersion":2,"config":{"digest":"%s"},"layers":[]}' "$config_digest" \
        >"$fixture/application-manifest.json"
    application_digest=$(sha256sum "$fixture/application-manifest.json" | cut -d' ' -f1)
    cp "$fixture/application-manifest.json" "$archive_root/blobs/sha256/$application_digest"

    printf '{"schemaVersion":2,"manifests":[{"mediaType":"application/vnd.oci.image.manifest.v1+json","digest":"sha256:%s","platform":{"architecture":"amd64","os":"linux"}}]}' \
        "$application_digest" >"$fixture/nested-index.json"
    nested_digest=$(sha256sum "$fixture/nested-index.json" | cut -d' ' -f1)
    cp "$fixture/nested-index.json" "$archive_root/blobs/sha256/$nested_digest"

    printf '{"schemaVersion":2,"manifests":[{"mediaType":"application/vnd.oci.image.index.v1+json","digest":"sha256:%s"}]}' \
        "$nested_digest" >"$archive_root/index.json"
    tar -C "$archive_root" -cf "$fixture/release-artifacts/docs-image.oci.tar" index.json blobs

    if ! step_body "Build exact-SHA OCI image with provenance and SBOM" | awk '
        /^          python3 - <<'\''PY'\''$/ { found = 1; next }
        found && /^          PY$/ { exit }
        found { sub(/^          /, ""); print }
    ' | (cd "$fixture" && python3 -) >"$fixture/reader.out" 2>"$fixture/reader.err"; then
        fail "embedded OCI reader failed on a nested image index: $(tail -n 1 "$fixture/reader.err")"
    fi

    [[ "$(<"$fixture/release-artifacts/release-assets/image-application-manifest-digest.txt")" == "sha256:$application_digest" ]] ||
        fail "embedded OCI reader wrote the wrong application manifest identity"
    [[ "$(<"$fixture/release-artifacts/release-assets/image-config-digest.txt")" == "$config_digest" ]] ||
        fail "embedded OCI reader wrote the wrong config identity"
    [[ "$(<"$fixture/release-artifacts/provenance-index-digest.txt")" == "sha256:$nested_digest" ]] ||
        fail "embedded OCI reader wrote the wrong provenance index identity"
)

test_ci_contract_wiring() {
    require_contains "$(<"$build_workflow")" 'Validate release workflow contracts' "PR CI must run the release workflow contracts"
    require_contains "$(<"$build_workflow")" 'bash tests/ReleasePipelineTests/release-workflow-contracts.sh' "PR CI must execute the dependency-free contract"
}

test_token_created_release_ci_dispatch() {
    local build prepare release_contracts verified dispatch
    build=$(<"$build_workflow")
    prepare=$(<"$prepare_workflow")
    release_contracts=$(job_body "$build_workflow" "release-contracts")
    require_contains "$build" 'workflow_dispatch:' "CI must allow explicit dispatch for github.token-created release PRs"
    require_contains "$release_contracts" "github.event_name == 'workflow_dispatch'" "explicitly dispatched release branches must run release contracts"
    require_contains "$prepare" 'actions: write' "prepare publication must be allowed to dispatch CI"
    require_contains "$prepare" 'gh workflow run Build_And_Test.yml --ref "$RELEASE_BRANCH"' "prepare publication must dispatch CI for the release branch"
    verified=$(grep -nF '[[ "$(git rev-parse FETCH_HEAD)" == "$PREPARED_COMMIT" ]] || exit 1' "$prepare_workflow" | head -n 1 | cut -d: -f1 || true)
    dispatch=$(grep -nF 'gh workflow run Build_And_Test.yml --ref "$RELEASE_BRANCH"' "$prepare_workflow" | head -n 1 | cut -d: -f1 || true)
    [[ -n "$verified" && -n "$dispatch" ]] || fail "prepared commit verification and CI dispatch must both exist"
    (( verified < dispatch )) || fail "CI dispatch must follow exact prepared commit verification"
}

test_release_version_change_runtime() (
    set -Eeuo pipefail
    local fixture base_sha merge_sha status
    fixture=$(mktemp -d)
    trap 'rm -rf -- "$fixture"' EXIT
    mkdir -p "$fixture/bin" "$fixture/src/MudX"

    printf '<Project><PropertyGroup><Version>9.5.0</Version><Description>base</Description></PropertyGroup></Project>\n' \
        >"$fixture/src/MudX/MudX.csproj"
    git -C "$fixture" init --quiet
    git -C "$fixture" config user.name fixture
    git -C "$fixture" config user.email fixture@example.invalid
    git -C "$fixture" add src/MudX/MudX.csproj
    git -C "$fixture" commit --quiet -m base
    base_sha=$(git -C "$fixture" rev-parse HEAD)

    printf '<Project><PropertyGroup><Version>9.5.0</Version><Description>changed</Description></PropertyGroup></Project>\n' \
        >"$fixture/src/MudX/MudX.csproj"
    git -C "$fixture" add src/MudX/MudX.csproj
    git -C "$fixture" commit --quiet -m merge
    merge_sha=$(git -C "$fixture" rev-parse HEAD)

    printf '%s\n' '#!/usr/bin/env bash' 'printf "9.5.0\\n"' >"$fixture/bin/dotnet"
    chmod +x "$fixture/bin/dotnet"

    set +e
    step_script "Derive and validate release identity" | (cd "$fixture" && env \
        PATH="$fixture/bin:$PATH" \
        BASE_SHA="$base_sha" \
        MERGE_SHA="$merge_sha" \
        GITHUB_ENV="$fixture/github-env" \
        bash -s) >"$fixture/step.out" 2>"$fixture/step.err"
    status=$?
    set -e

    [[ "$status" -ne 0 ]] || fail "release identity accepted a project change without a Version change"
    grep -Fq 'Release PR did not change the MudX Version' "$fixture/step.err" ||
        fail "unchanged Version failed for an unexpected reason: $(tail -n 1 "$fixture/step.err")"
)

test_solution_workflow_items() {
    python3 - "$solution_file" "$repository_root" <<'PY'
import pathlib
import sys
import xml.etree.ElementTree as ET

solution = pathlib.Path(sys.argv[1])
repository_root = pathlib.Path(sys.argv[2])
expected = {
    "../.github/workflows/Build_And_Test.yml",
    "../.github/workflows/auto-assign.yml",
    "../.github/workflows/old-Build_And_Deploy.yml",
    "../.github/workflows/old-Deploy.yml",
    "../.github/workflows/old-Update_MudX_Version.yml",
    "../.github/workflows/old-deploy-mudx-nuget.yml",
    "../.github/workflows/prepare-release.yml",
    "../.github/workflows/release.yml",
}
actual = {
    item.attrib["Path"]
    for item in ET.parse(solution).getroot().iter("File")
    if item.attrib["Path"].startswith("../.github/workflows/")
}
missing = sorted(expected - actual)
unexpected = sorted(actual - expected)
if missing or unexpected:
    raise SystemExit(f"workflow solution items differ: missing={missing}, unexpected={unexpected}")

missing_files = sorted(
    path for path in actual if not (solution.parent / path).resolve().is_file()
)
if missing_files:
    raise SystemExit(f"workflow solution items do not exist: {missing_files}")
PY
}

tests=(
    prepare_merge_identity
    exact_release_sdk
    exact_draft_assets
    draft_asset_api_failure_runtime
    guard_order
    guard_drafts
    no_op_stable
    reproducible_oci
    final_head_approval
    nuget_content_types
    immutable_inspection_errors
    reject_prerelease
    prerelease_draft_runtime
    portable_tar_reader
    oci_reader_runtime
    ci_contract_wiring
    token_created_release_ci_dispatch
    release_version_change_runtime
    solution_workflow_items
)

requested=("${@:-${tests[@]}}")
for current_test in "${requested[@]}"; do
    if ! declare -F "test_$current_test" >/dev/null; then
        echo "Unknown test: $current_test" >&2
        exit 2
    fi
    "test_$current_test"
    echo "PASS $current_test"
done
