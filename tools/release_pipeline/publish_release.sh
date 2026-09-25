#!/usr/bin/env bash
set -euo pipefail
: "${VERSION:?}" "${SHA:?}" "${PRODUCER:?}" "${IMAGE:?}" "${PACKAGE_ID:?}" "${PUBLIC_URL:?}" "${NUGET_API_KEY:?}"
GUARD=tools/release_pipeline/release_guard.py
ROOT=release-artifacts
mapfile -t mains < <(find "$ROOT/packages" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg')
mapfile -t symbols < <(find "$ROOT/packages" -maxdepth 1 -type f -name '*.snupkg')
test "${#mains[@]}" -eq 1 && test "${#symbols[@]}" -eq 1
main=${mains[0]}; symbol=${symbols[0]}; expected=$(jq -r .image_digest "$ROOT/manifest.json")
symbol_sha=$(sha256sum "$symbol" | cut -d' ' -f1)
marker="<!-- mudx-symbol-sha256:$symbol_sha -->"

verify_release_assets() {
  local names expected_names
  names=$(gh release view "v$VERSION" --json assets --jq '.assets | map(.name) | sort | join("\n")')
  expected_names=$(printf '%s\n%s\n' "$(basename "$main")" "$(basename "$symbol")" | sort)
  [[ "$names" = "$expected_names" ]] || return 1
  rm -rf /tmp/release-assets; mkdir /tmp/release-assets
  gh release download "v$VERSION" -D /tmp/release-assets || return 1
  cmp -s "$main" "/tmp/release-assets/$(basename "$main")" || return 1
  cmp -s "$symbol" "/tmp/release-assets/$(basename "$symbol")" || return 1
}
newest_finalized() {
  gh release list --limit 100 --json tagName,isDraft,isPrerelease --jq '.[]|select(.isDraft==false and .isPrerelease==false)|.tagName|ltrimstr("v")' |
    grep -E '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$' | sort -V | tail -1
}

sudo apt-get update
sudo apt-get install -y skopeo
echo "$GHCR_TOKEN" | skopeo login ghcr.io -u "$GITHUB_ACTOR" --password-stdin

image_state=absent
if current=$(skopeo inspect "docker://$IMAGE:$VERSION" --format '{{.Digest}}' 2>/tmp/image-inspect.err); then
  [[ "$current" = "$expected" ]] && image_state=match || image_state=mismatch
elif ! grep -Eqi 'manifest unknown|not found|404' /tmp/image-inspect.err; then
  cat /tmp/image-inspect.err >&2; exit 1
fi

package_url="https://api.nuget.org/v3-flatcontainer/$PACKAGE_ID/$VERSION/$PACKAGE_ID.$VERSION.nupkg"
code=$(curl --retry 3 -sS -w '%{http_code}' -o /tmp/published.nupkg "$package_url")
main_state=missing
if [[ "$code" = 200 ]]; then
  if python3 "$GUARD" package-equivalent --candidate "$main" --published /tmp/published.nupkg; then main_state=equivalent; else main_state=mismatch; fi
elif [[ "$code" != 404 ]]; then
  echo "NuGet inspection failed with HTTP $code" >&2; exit 1
fi

release_state=missing; symbols_state=missing; release_id=''
if release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease,body 2>/dev/null); then
  release_id=$(jq -r .databaseId <<<"$release_json")
  test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA" || release_state=mismatch
  verify_release_assets || release_state=mismatch
  if [[ "$release_state" != mismatch ]]; then
    if [[ "$(jq -r .isPrerelease <<<"$release_json")" = true ]]; then release_state=mismatch
    elif [[ "$(jq -r .isDraft <<<"$release_json")" = true ]]; then
      release_state=draft
      body=$(jq -r '.body // ""' <<<"$release_json")
      if grep -Fqx "$marker" <<<"$body"; then symbols_state=published
      elif grep -Fq '<!-- mudx-symbol-sha256:' <<<"$body"; then release_state=mismatch
      fi
    else release_state=exact
    fi
  fi
fi

deployed=$(curl --retry 2 -fsS -H 'Cache-Control: no-cache' "$PUBLIC_URL/revision?release=$PRODUCER&probe=$GITHUB_RUN_ID-$GITHUB_RUN_ATTEMPT" || true)
deploy_state=missing; [[ "$deployed" = "$SHA" ]] && deploy_state=match
if [[ "$release_state" = exact ]]; then
  newest=$(newest_finalized)
  if [[ "$newest" != "$VERSION" ]]; then echo 'A newer finalized release exists; exact older release is a no-op.'; exit 0; fi
  [[ "$image_state" = match && "$main_state" = equivalent && "$deploy_state" = match ]] || { echo 'Finalized latest release conflicts with external state.' >&2; exit 1; }
  latest=$(skopeo inspect "docker://$IMAGE:latest" --format '{{.Digest}}' 2>/dev/null || true)
  if [[ "$latest" != "$expected" ]]; then skopeo copy "docker://$IMAGE@$expected" "docker://$IMAGE:latest"; fi
  test "$(skopeo inspect "docker://$IMAGE:latest" --format '{{.Digest}}')" = "$expected"
  echo 'Release already finalized with exact retained candidate; resume reconciled latest and stopped.'; exit 0
fi
plan=$(python3 "$GUARD" resume-plan --image "$image_state" --main "$main_state" --symbols "$symbols_state" --deploy "$deploy_state" --release "$release_state")

if [[ "$(jq -r .image <<<"$plan")" = publish ]]; then
  skopeo copy "oci-archive:$ROOT/image.oci.tar" "docker://$IMAGE:$VERSION"
fi
test "$(skopeo inspect "docker://$IMAGE:$VERSION" --format '{{.Digest}}')" = "$expected"

if [[ "$(jq -r .main <<<"$plan")" = publish ]]; then
  dotnet nuget push "$main" --source https://api.nuget.org/v3/index.json --no-symbols
  code=000
  for _ in {1..30}; do
    code=$(curl -sS -w '%{http_code}' -o /tmp/published.nupkg "$package_url")
    [[ "$code" = 200 ]] && break
    sleep 10
  done
  [[ "$code" = 200 ]]
  python3 "$GUARD" package-equivalent --candidate "$main" --published /tmp/published.nupkg
fi

if [[ "$release_state" = missing ]]; then
  if gh release view "v$VERSION" >/dev/null 2>&1; then echo 'Release appeared concurrently; rerun resume to authenticate it.' >&2; exit 1; fi
  if gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --silent 2>/dev/null; then
    test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA" || { echo 'Existing tag targets a different commit.' >&2; exit 1; }
    gh release create "v$VERSION" --draft --verify-tag --generate-notes "$main" "$symbol"
  else
    gh release create "v$VERSION" --draft --target "$SHA" --generate-notes "$main" "$symbol"
  fi
  release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease)
  [[ "$(jq -r .isDraft <<<"$release_json")" = true && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
  release_id=$(jq -r .databaseId <<<"$release_json")
  test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA"
  verify_release_assets
  release_state=draft
fi

if [[ "$symbols_state" = missing ]]; then
  if ! dotnet nuget push "$symbol" --source https://api.nuget.org/v3/index.json; then
    echo 'Symbol publication outcome cannot be proven by NuGet public APIs. Manually reconcile the retained .snupkg; do not use skip-duplicate or rebuild.' >&2; exit 1
  fi
  body=$(gh release view "v$VERSION" --json body --jq '.body // ""')
  gh api --method PATCH "repos/$GITHUB_REPOSITORY/releases/$release_id" -f body="$body
$marker" >/dev/null
  grep -Fqx "$marker" <<<"$(gh release view "v$VERSION" --json body --jq '.body // ""')"
fi

if [[ "$(jq -r .deploy <<<"$plan")" = deploy ]]; then
  : "${HOST:?}" "${USER:?}" "${PORT:?}" "${KNOWN_HOSTS:?}" "${DEPLOY_KEY:?}"
  [[ "$PORT" =~ ^[1-9][0-9]*$ ]]
  install -m700 -d ~/.ssh; install -m600 /dev/null ~/.ssh/key
  printf '%s\n' "$DEPLOY_KEY" > ~/.ssh/key; printf '%s\n' "$KNOWN_HOSTS" > ~/.ssh/known_hosts
  run=${PRODUCER%/*}
  set +e
  ssh -i ~/.ssh/key -o BatchMode=yes -o StrictHostKeyChecking=yes -p "$PORT" "$USER@$HOST" -- deploy "$run" "$VERSION" "$SHA" "$expected"
  ssh_rc=$?; set -e
  deployed=''
  for _ in {1..30}; do
    deployed=$(curl -fsS -H 'Cache-Control: no-cache' "$PUBLIC_URL/revision?release=$PRODUCER&probe=$RANDOM" || true)
    [[ "$deployed" = "$SHA" ]] && break
    sleep 5
  done
  if [[ "$deployed" != "$SHA" ]]; then echo "Deployment outcome unresolved after SSH exit $ssh_rc; inspect host journal before retry." >&2; exit 1; fi
fi

release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease,body)
[[ "$(jq -r .databaseId <<<"$release_json")" = "$release_id" && "$(jq -r .isDraft <<<"$release_json")" = true && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
grep -Fqx "$marker" <<<"$(jq -r '.body // ""' <<<"$release_json")"
test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA"
verify_release_assets
prior_final=$(newest_finalized || true)
if [[ -n "$prior_final" && "$(printf '%s\n%s\n' "$VERSION" "$prior_final" | sort -V | tail -1)" != "$VERSION" ]]; then
  echo 'A newer finalized stable release exists; refusing to finalize an older draft.' >&2; exit 1
fi
gh api --method PATCH "repos/$GITHUB_REPOSITORY/releases/$release_id" -F draft=false >/dev/null
release_json=$(gh release view "v$VERSION" --json isDraft,isPrerelease)
[[ "$(jq -r .isDraft <<<"$release_json")" = false && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
[[ "$(newest_finalized)" = "$VERSION" ]] || { echo 'A newer finalized stable release exists; refusing to move latest backward.' >&2; exit 1; }
skopeo copy "docker://$IMAGE@$expected" "docker://$IMAGE:latest"
test "$(skopeo inspect "docker://$IMAGE:latest" --format '{{.Digest}}')" = "$expected"
