#!/usr/bin/env bash
set -euo pipefail
: "$VERSION" "$SHA" "$PRODUCER" "$PACKAGE_ID" "$NUGET_API_KEY"
GUARD=tools/release_pipeline/release_guard.py
ROOT=release-artifacts
main=$(find "$ROOT/packages" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg')
symbol=$(find "$ROOT/packages" -maxdepth 1 -type f -name '*.snupkg')
test "$(printf '%s\n' "$main" | grep -c .)" -eq 1
test "$(printf '%s\n' "$symbol" | grep -c .)" -eq 1
archive="$ROOT/MudX-$VERSION-linux-amd64.tar.gz"
checksums="$ROOT/SHA256SUMS"
manifest="$ROOT/manifest.json"
for file in "$archive" "$checksums" "$manifest"; do test -s "$file"; done
symbol_sha=$(sha256sum "$symbol" | cut -d' ' -f1)
marker="<!-- mudx-symbol-sha256:$symbol_sha -->"

verify_release_assets() {
  local names expected_names download status
  names=$(gh release view "v$VERSION" --json assets --jq '.assets | map(.name) | sort | join("\n")')
  expected_names=$(printf '%s\n' "$(basename "$main")" "$(basename "$symbol")" "$(basename "$archive")" "$(basename "$checksums")" "$(basename "$manifest")" | LC_ALL=C sort)
  [[ "$names" = "$expected_names" ]] || return 1
  download=$(mktemp -d)
  gh release download "v$VERSION" -D "$download" || { rm -rf "$download"; return 1; }
  set +e
  cmp -s "$main" "$download/$(basename "$main")" &&
    cmp -s "$symbol" "$download/$(basename "$symbol")" &&
    cmp -s "$archive" "$download/$(basename "$archive")" &&
    cmp -s "$checksums" "$download/$(basename "$checksums")" &&
    cmp -s "$manifest" "$download/$(basename "$manifest")" &&
    (cd "$download" && sha256sum -c SHA256SUMS)
  status=$?
  set -e
  rm -rf "$download"
  return "$status"
}
newest_finalized() {
  gh release list --limit 100 --json tagName,isDraft,isPrerelease --jq '.[]|select(.isDraft==false and .isPrerelease==false)|.tagName|ltrimstr("v")' |
    grep -E '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$' | sort -V | tail -1
}

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
    elif [[ "$(jq -r .isDraft <<<"$release_json")" = true ]]; then release_state=draft
    else release_state=exact
    fi
    body=$(jq -r '.body // ""' <<<"$release_json")
    if grep -Fqx "$marker" <<<"$body"; then symbols_state=published
    elif grep -Fq '<!-- mudx-symbol-sha256:' <<<"$body"; then release_state=mismatch
    elif [[ "$release_state" = exact ]]; then release_state=mismatch
    fi
  fi
fi
plan=$(python3 "$GUARD" resume-plan --main "$main_state" --symbols "$symbols_state" --release "$release_state")
if [[ "$release_state" = exact ]]; then
  echo 'Release already finalized with the exact retained candidate.'
  exit 0
fi

if [[ "$(jq -r .main <<<"$plan")" = publish ]]; then
  dotnet nuget push "$main" --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY" --no-symbols
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
    gh release create "v$VERSION" --draft --verify-tag --generate-notes "$main" "$symbol" "$archive" "$checksums" "$manifest"
  else
    gh release create "v$VERSION" --draft --target "$SHA" --generate-notes "$main" "$symbol" "$archive" "$checksums" "$manifest"
  fi
  release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease)
  [[ "$(jq -r .isDraft <<<"$release_json")" = true && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
  release_id=$(jq -r .databaseId <<<"$release_json")
  test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA"
  verify_release_assets
  release_state=draft
fi

if [[ "$symbols_state" = missing ]]; then
  if ! dotnet nuget push "$symbol" --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY"; then
    echo 'Symbol publication outcome cannot be proven by NuGet public APIs. Manually reconcile the retained .snupkg; do not use skip-duplicate or rebuild.' >&2; exit 1
  fi
  body=$(gh release view "v$VERSION" --json body --jq '.body // ""')
  gh api --method PATCH "repos/$GITHUB_REPOSITORY/releases/$release_id" -f body="$body
$marker" >/dev/null
  grep -Fqx "$marker" <<<"$(gh release view "v$VERSION" --json body --jq '.body // ""')"
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
