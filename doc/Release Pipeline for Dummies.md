# MudX Release Pipeline for Dummies

This guide describes the **proposed** release-only workflow in .github/workflows/Release_MudX.yml. Its local guards and publication helper are tested, but no real release has been run with this proposal yet. Merging the proposal does not publish anything.

## What the workflow does

One owner starts **Release MudX**. That single dispatch is the authorization for this sequence:

1. select a stable version that is the same as the project version or higher, while rejecting every published or reserved version;
2. when the selected version is the same, validate the already-reserved source at the exact dev checkout SHA and require a successful Build_And_Test.yml push run for that SHA;
3. when the selected version is higher, push a version-only branch to MudXtra/MudX and open a pull request to dev;
4. for that higher-version PR, have the distinct MudXBot account revalidate and approve its exact head;
5. wait for a successful Build_And_Test.yml PR run for that exact head, merge it atomically, and require successful push CI for the exact merge SHA;
6. build packages and a loadable linux/amd64 Docker archive from the one selected source SHA;
7. publish the NuGet packages and a GitHub Release.

The release workflow does **not** repeat the unit-test suite. It trusts only an authentic successful Build_And_Test.yml run after checking repository, workflow path, event, SHA, status, and conclusion. Missing, pending, skipped, cancelled, failed, or wrong-identity runs cannot advance publication. Coveralls alone is not a test gate.

There is no SSH, server deployment, systemd action, GHCR push, latest tag, or production restart in this release path.

## Administrator prerequisites

Configure these before the first real run:

### Repository configuration

- Repository variable MUDX_RELEASE_ACTORS: comma-separated GitHub logins allowed to dispatch and rerun releases.
- Repository or organization secret GH_BOT_TOKEN: belongs to MudXBot, which uses a separate identity and token from the coordinator, and can approve a higher-version PR.
- Repository or organization secret NUGET_KEY: can publish MudX.MudBlazor.Extension to NuGet.

### Coordination environment

The version-pr job is bound to the existing mudx-release-coordination environment on dev. Configure its environment secret GH_RELEASE_COORDINATOR_TOKEN and environment variable MUDX_BOT_LOGIN. The inspected environment has a dev-only branch policy and no required reviewers or wait timer, so this binding exposes coordination values without adding another human approval. Do not move, recreate, or read the secret to use this workflow.

GH_RELEASE_COORDINATOR_TOKEN is a user token for the release coordinator. It reads Actions runs and, only for a higher version, pushes the version branch and creates and merges the PR in MudXtra/MudX. Grant normal repository or organization authorization for MudXtra/MudX with Contents read/write, Pull requests read/write, and Actions read (or the fine-grained equivalent). No workflow-file write permission is needed because a generated commit changes only the version file. An existing token with these permissions can be reused; no fork access is required.

The coordinator credential must be able to trigger the downstream PR and push CI. Events created with the default GITHUB_TOKEN do not start new workflow runs; use a non-default credential such as a PAT (or an App credential only in an implementation that supports App identity). The current workflow expects a user token because it verifies both coordinator and Bot identities with gh api user; it does not claim GitHub App token compatibility.

GitHub creates GITHUB_TOKEN automatically for each run; do not create another secret for it or broaden the repository default from read to read/write. The publish and resume-publish jobs explicitly request Actions read and Contents write, and that per-job Contents write permission authorizes GitHub Release creation, asset upload, and finalization. During an authorized real run, verify that Set up job → GITHUB_TOKEN Permissions shows Contents: write. Organization restrictions or tag rules can still block actual operations, so repository metadata alone is not live release proof. The separate setting that lets Actions approve pull requests is unrelated to GitHub Release publishing.

Environment policy can change independently; verify it before a real release. Do not weaken environment policy, branch protection, required review, token isolation, or repository access to make a run pass.

## Exact clicks for a new release

1. Open the upstream MudX repository on GitHub.
2. Select **Actions**.
3. Select **Release MudX**.
4. Select **Run workflow** on branch dev.
5. Leave **mode** as release.
6. For an already-set unpublished project version, enter that exact same version in **custom_version**. To reserve a higher version, enter the higher stable version or choose patch, minor, or major.
7. Select **Run workflow** once.

An explicit same version releases the already-reserved source after exact selected-SHA push CI and creates no version PR. A higher version retains the complete version-only PR, MudXBot approval, PR CI, atomic merge, and merged-SHA push CI flow. Lower versions and versions already published or reserved are rejected. Do not start another release while the workflow or its version PR is active.

## Expected Actions sequence

The release run shows these jobs:

- **version-pr** — for the same version, validates the exact already-versioned dev SHA and its push CI without a PR; for a higher version, runs the complete MudXtra/MudX version PR, MudXBot approval, PR CI, merge, and merged-SHA CI flow.
- **build** — all shipped MudX target frameworks, docs build, package checks, and creation of the versioned linux/amd64 Docker archive.
- **publish** — retained-manifest verification, NuGet publication, GitHub draft asset readback, symbol checkpoint, and final release.

Build_And_Test.yml remains the trusted CI workflow and retains the existing MudX product test and coverage work.

## Success evidence and downloads

A successful run has:

- for a higher version, a merged version-only PR and successful Build_And_Test.yml PR run for its exact head SHA;
- a successful Build_And_Test.yml push run for the exact selected source SHA (the unchanged dev SHA for the same version, or the exact merge SHA for a higher version);
- one NuGet package and one symbol package;
- a non-draft, non-prerelease GitHub Release tagged v<version> at the exact selected source SHA;
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

The revision response must be the exact selected source SHA recorded by the release run.

## Limited failure recovery

- **Same-version source gate:** inspect the exact selected dev SHA and its push CI; do not create a version PR or substitute a newer branch SHA.
- **Before a higher-version PR merges:** inspect the same PR and exact CI run. Do not allocate another version or open a replacement release PR.
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
