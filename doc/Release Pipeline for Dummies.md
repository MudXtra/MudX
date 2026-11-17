# MudX Release Pipeline for Dummies

`.github/workflows/Release_MudX.yml` (**Release MudX**) releases MudX when the version changes. There is nothing to start by hand.

## How to release

1. Open a PR to `dev` that changes `<Version>` in `src/MudX/MudX.csproj` (stable `X.Y.Z` only).
2. Merge it. The workflow runs on the push to `dev`.

Ordinary merges that do not change the version find everything already released and stop after the **plan** job.

## What it does

1. **plan** reads `<Version>` at the pushed commit and checks three things: is it on NuGet, is there a published GitHub Release `vX.Y.Z`, is there a GHCR image tagged `X.Y.Z`. If all three exist, it stops. It fails if the version is lower than the highest published version and not already on NuGet.
2. **build** generates static assets, builds net8.0/net9.0/net10.0 and the docs, runs `tests/MudX.UnitTests`, packs, and checks the packaged static assets.
3. **nuget** publishes the `.nupkg` and `.snupkg` with `--skip-duplicate` (only if NuGet does not have the version yet).
4. **github-release** creates a published release `vX.Y.Z` tagged at the exact commit, with generated notes and the packages attached. A leftover draft release for the same tag is reused and published. It never moves an existing tag that points to a different commit, and fails if the published tag does not resolve to the planned commit.
5. **image** runs after nuget (skipped nuget is fine, a failed one blocks it), builds the docs-site image and pushes `ghcr.io/mudxtra/mudx/mudxdocwebsite:X.Y.Z` (and `:latest` when this is the newest version) after checking its version and revision labels.

Each stage runs only if it is still needed, so re-running after a partial failure finishes the missing parts and nothing else.

## Re-running

Open **Actions → Release MudX → Run workflow** on `dev`. If everything is already released it is a no-op.

## Secrets

- `NUGET_USERNAME`: nuget.org profile name for Trusted Publishing (see below). The workflow fails on MudXtra/MudX if it is missing, and skips NuGet with a warning on forks.
- `GITHUB_TOKEN` (automatic): creates the GitHub Release and pushes the GHCR image.

No `NUGET_KEY` is used.

## One-time nuget.org setup (owner)

1. nuget.org → account → **Trusted Publishing** → add a policy: Repository Owner `MudXtra`, Repository `MudX`, Workflow File `Release_MudX.yml`, Environment blank. The policy owner must be the account that owns `MudX.MudBlazor.Extension`.
2. Add the repository secret `NUGET_USERNAME` = that nuget.org profile name.
3. After the first successful trusted-publishing release, delete the `NUGET_KEY` secret (until then it is the fallback if the policy is wrong).
4. GHCR: in the package `mudxtra/mudx/mudxdocwebsite` → Package settings → **Manage Actions access**, give `MudXtra/MudX` the **Write** role, or the image push fails.

The policy binds to the workflow file name, so keep the file name and keep the NuGet push inside `Release_MudX.yml`.

## No longer used

`mudxtra-bot`, `GH_BOT_TOKEN`, `GH_RELEASE_COORDINATOR_TOKEN`, `MUDX_RELEASE_ACTORS`, `MUDX_BOT_LOGIN` and the `mudx-release-coordination` environment are not used by the workflow. An administrator can remove them.

## Docker image visibility

The GHCR package is private, so pulling needs authentication (`docker login ghcr.io` with a token that has `read:packages`) unless the package is made public in the package settings.
