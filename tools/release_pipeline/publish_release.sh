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
expected_release_assets=("$main" "$symbol" "$archive" "$checksums" "$manifest")
missing_release_assets=()

inspect_release_asset_subset() {
  local names download name path status
  declare -A expected=() observed=()
  missing_release_assets=()
  for path in "${expected_release_assets[@]}"; do expected["$(basename "$path")"]="$path"; done
  names=$(gh release view "v$VERSION" --json assets --jq '.assets | map(.name) | sort | join("\n")')
  download=$(mktemp -d)
  if [[ -n "$names" ]]; then
    gh release download "v$VERSION" -D "$download" || { rm -rf "$download"; return 1; }
  fi
  status=0
  while IFS= read -r name; do
    [[ -z "$name" ]] && continue
    if [[ -z "${expected[$name]+present}" || -n "${observed[$name]+present}" ]]; then status=1; break; fi
    observed["$name"]=1
    cmp -s "${expected[$name]}" "$download/$name" || { status=1; break; }
  done <<<"$names"
  if [[ "$status" -eq 0 ]]; then
    for path in "${expected_release_assets[@]}"; do
      name=$(basename "$path")
      [[ -n "${observed[$name]+present}" ]] || missing_release_assets+=("$path")
    done
    if [[ "${#missing_release_assets[@]}" -eq 0 ]]; then
      (cd "$download" && sha256sum -c SHA256SUMS) || status=1
    fi
  fi
  rm -rf "$download"
  return "$status"
}

verify_release_assets() {
  inspect_release_asset_subset && [[ "${#missing_release_assets[@]}" -eq 0 ]]
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
  if [[ "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" != "$SHA" ]] ||
     [[ "$(jq -r .isPrerelease <<<"$release_json")" = true ]] ||
     ! inspect_release_asset_subset; then
    release_state=mismatch
  elif [[ "$(jq -r .isDraft <<<"$release_json")" = true ]]; then
    release_state=draft
  elif [[ "${#missing_release_assets[@]}" -eq 0 ]]; then
    release_state=exact
  else
    release_state=mismatch
  fi
  if [[ "$release_state" != mismatch ]]; then
    body=$(jq -r '.body // ""' <<<"$release_json")
    marker_args=(symbol-marker --body "$body" --expected "$marker")
    if [[ "$release_state" != exact ]]; then marker_args+=(--allow-missing); fi
    symbols_state=$(python3 "$GUARD" "${marker_args[@]}")
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
    gh release create "v$VERSION" --draft --verify-tag --generate-notes
  else
    gh release create "v$VERSION" --draft --target "$SHA" --generate-notes
  fi
  release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease)
  [[ "$(jq -r .isDraft <<<"$release_json")" = true && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
  release_id=$(jq -r .databaseId <<<"$release_json")
  test "$(gh api "repos/$GITHUB_REPOSITORY/commits/v$VERSION" --jq .sha)" = "$SHA"
  missing_release_assets=("${expected_release_assets[@]}")
  release_state=draft
fi

if [[ "$release_state" = draft && "${#missing_release_assets[@]}" -gt 0 ]]; then
  gh release upload "v$VERSION" "${missing_release_assets[@]}"
fi
if [[ "$release_state" = draft ]]; then
  verify_release_assets
fi

if [[ "$symbols_state" = missing ]]; then
  if ! dotnet nuget push "$symbol" --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY"; then
    echo 'Symbol publication outcome cannot be proven by NuGet public APIs. Manually reconcile the retained .snupkg; do not use skip-duplicate or rebuild.' >&2; exit 1
  fi
  body=$(gh release view "v$VERSION" --json body --jq '.body // ""')
  [[ "$(python3 "$GUARD" symbol-marker --body "$body" --expected "$marker" --allow-missing)" = missing ]]
  gh api --method PATCH "repos/$GITHUB_REPOSITORY/releases/$release_id" -f body="$body
$marker" >/dev/null
  body=$(gh release view "v$VERSION" --json body --jq '.body // ""')
  [[ "$(python3 "$GUARD" symbol-marker --body "$body" --expected "$marker")" = published ]]
fi

release_json=$(gh release view "v$VERSION" --json databaseId,isDraft,isPrerelease,body)
[[ "$(jq -r .databaseId <<<"$release_json")" = "$release_id" && "$(jq -r .isDraft <<<"$release_json")" = true && "$(jq -r .isPrerelease <<<"$release_json")" = false ]]
body=$(jq -r '.body // ""' <<<"$release_json")
[[ "$(python3 "$GUARD" symbol-marker --body "$body" --expected "$marker")" = published ]]
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
