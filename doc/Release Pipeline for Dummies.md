# MudX Release Pipeline for Dummies

This guide describes the **proposed** release-only workflow in .github/workflows/Release_MudX.yml. Its local guards and publication helper are tested, but no real release has been run with this proposal yet. Merging the proposal does not publish anything.

## What the workflow does

One owner starts **Release MudX**. That single dispatch is the authorization for this sequence:

1. calculate a new stable version;
2. push a version-only branch to the configured fork;
3. open a pull request to upstream dev;
4. have the distinct MudXBot account revalidate and approve the exact PR head;
5. wait for a successful upstream Build_And_Test.yml run for that exact PR SHA;
6. merge the still-unchanged PR;
7. wait for a successful upstream Build_And_Test.yml push run for the exact merge SHA;
8. build packages and a loadable linux/amd64 Docker archive from that merge SHA;
9. publish the NuGet packages and a GitHub Release.

The release workflow does **not** repeat the unit-test suite. It trusts only an authentic successful Build_And_Test.yml run after checking repository, workflow path, event, SHA, status, and conclusion. Missing, pending, skipped, cancelled, failed, or wrong-identity runs cannot advance publication. Coveralls alone is not a test gate.

There is no SSH, server deployment, systemd action, GHCR push, latest tag, or production restart in this release path.

## Administrator prerequisites

Configure these before the first real run:

### Repository variables

- MUDX_RELEASE_ACTORS: comma-separated GitHub logins allowed to dispatch and rerun releases.
- MUDX_BOT_LOGIN: the expected MudXBot login.
- MUDX_RELEASE_FORK: the coordinator-owned fork in owner/repository form, for example versile2/MudX.

### Repository secrets

- GH_RELEASE_COORDINATOR_TOKEN: can push a branch to the configured fork and create/merge the upstream version PR.
- GH_BOT_TOKEN: belongs to the distinct MudXBot account and can approve the upstream PR.
- NUGET_KEY: can publish MudX.MudBlazor.Extension to NuGet.

The repository must also allow the workflow GITHUB_TOKEN to create and finalize GitHub Releases. The proposal intentionally uses no GitHub Environment, so it introduces no second human publication approval. Do not weaken branch protection, required review, token isolation, or repository access to make a run pass.

## Exact clicks for a new release

1. Open the upstream MudX repository on GitHub.
2. Select **Actions**.
3. Select **Release MudX**.
4. Select **Run workflow** on branch dev.
5. Leave **mode** as release.
6. Choose patch, minor, or major; optionally enter a higher exact stable SemVer in **custom_version**.
7. Select **Run workflow** once.

Do not choose an actual version until a real release is authorized. Do not start another release while the workflow or its version PR is active.

## Expected Actions sequence

The release run shows these jobs:

- **version-pr** — fork branch, exact version-only PR, MudXBot approval, exact PR CI, merge, exact merged-SHA CI.
- **build** — all shipped MudX target frameworks, docs build, package checks, and creation of the versioned linux/amd64 Docker archive.
- **publish** — retained-manifest verification, NuGet publication, GitHub draft asset readback, symbol checkpoint, and final release.

Build_And_Test.yml remains the trusted CI workflow and retains the existing MudX product test and coverage work.

## Success evidence and downloads

A successful run has:

- a merged version-only PR;
- a successful Build_And_Test.yml PR run for its exact head SHA;
- a successful Build_And_Test.yml push run for the exact merged SHA;
- one NuGet package and one symbol package;
- a non-draft, non-prerelease GitHub Release tagged v<version> at the exact merged SHA;
- these exact release assets:
  - MudX.MudBlazor.Extension.<version>.nupkg
  - MudX.MudBlazor.Extension.<version>.snupkg
  - MudX-<version>-linux-amd64.tar.gz
  - SHA256SUMS
  - manifest.json

The publisher downloads the release assets and compares their bytes before finalizing the release. It never uses --skip-duplicate to hide a conflict.

## Load and run the Docker download

After downloading the archive and SHA256SUMS into one directory:

    archive="MudX-<version>-linux-amd64.tar.gz"
    grep -Fx "$(sha256sum "$archive")" SHA256SUMS
    gzip -dc "$archive" | docker load
    docker run --rm -p 8080:8080 mudx:<version>

Then check:

    http://127.0.0.1:8080/healthz
    http://127.0.0.1:8080/revision

The revision response must be the merged source SHA recorded by the release run.

## Limited failure recovery

- **Before the version PR merges:** inspect the same PR and exact CI run. Do not allocate another version or open a replacement release PR.
- **Before a retained build artifact exists:** use normal GitHub job diagnostics. The resume mode cannot rebuild a candidate.
- **After the retained artifact exists but publication fails:** rerun **Release MudX** with mode=resume, entering the original producer run ID and attempt. Resume authenticates that completed owner-dispatched run and its exact retained manifest, then continues only missing publication stages.
- **Existing NuGet package, tag, draft, release asset, or SHA conflicts:** stop and reconcile manually. Do not rebuild the published version, delete evidence, or use skip-duplicate.
- **Uncertain symbol publication:** stop for manual reconciliation because NuGet does not provide sufficient public symbol-state proof.

A real run, credential readiness, repository permissions, PR approval behavior, and GitHub/NuGet publication remain live proof that this proposal has not performed.

## Workflow inventory

Kept:

- Release_MudX.yml — the single owner-initiated release path.
- Build_And_Test.yml — trusted PR/dev product tests and coverage.
- auto-assign.yml — unrelated issue assignment automation.

Removed as superseded duplicates:

- Update_MudX_Version.yml
- deploy-mudx-nuget.yml
- Build_And_Deploy.yml
- Deploy.yml

Historical deployment source under deploy/ remains repository history/source, but nothing in the proposed release workflow invokes it.
