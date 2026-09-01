# Releasing MudX

MudX releases use a reviewed pull request, unprivileged build jobs, same-run attested artifacts, and a separately approved publication job. No workflow approves or merges its own pull request.

## Required repository setup

Before enabling this pipeline, create a GitHub Environment named `release` and configure required human reviewers. Do not add deployment-branch rules that exclude the current release PR. Keep branch protection on `dev` configured to require approving reviews.

The workflow also verifies the merged pull request's final `reviewDecision` is `APPROVED` and that an approval targets the final pull-request head commit, requires the `release` label, and requires the head repository to equal `MudXtra/MudX`. The protected Environment is a second human gate before publication credentials become available. These settings are external approval gates; source changes do not configure them.

## Workflow order and PR #65

1. Merge this infrastructure change into `dev` after review.
2. Keep the already-open same-repository PR #65 green and approved. Apply or retain its `release` label only when it is ready to publish. PR #65 does not need a `release/*` head branch; same-repository provenance, final approval, merge into `dev`, the label, and the protected Environment are the release identity.
3. Merge PR #65 into `dev`. `release.yml` checks out its exact merge SHA and derives the version from `src/MudX/MudX.csproj`.
4. For later releases, run **Prepare MudX release** with stable SemVer such as `9.9.0`. Do not include `v`, prerelease data, or build metadata.
5. Review and approve the generated `release/<version>` PR, then merge it into `dev` after applying the normal release gates.

The four `old-*` workflows are disabled historical references and must not be run.

## Credential boundary

`prepare-release.yml` performs versioning, generation, build, and tests in a `contents: read` job with `persist-credentials: false`. It uploads a checksummed Git bundle named with both the workflow run and producing attempt, and exposes that exact name as a job output. A separate write-capable job downloads the producer-owned bundle, verifies it, pushes only the prepared commit when needed, and creates or repairs the PR. It does not execute repository build code.

`release.yml` has four jobs:

1. `gate` verifies merged, labelled, same-repository, approved PR provenance.
2. `build` checks out the exact merge with `contents: read`, no persisted credential, and no publication secret. It regenerates, builds, tests, packs, validates static assets, and builds one OCI image.
3. `attest` creates GitHub artifact provenance in a job isolated from repository execution.
4. `publish` downloads and verifies the producer-owned checksums and attestations, then waits on the protected `release` Environment. A failed-job rerun consumes the retained successful prerequisite's artifact through job outputs; a full workflow rerun produces a distinct attempt-qualified artifact. GitHub, GHCR, and NuGet credentials are scoped only to the steps that use them.

## Release identity and provenance

The build emits main and symbol NuGet packages, checksums, an OCI image, OCI provenance/SBOM attestations, source/workflow/revision labels, and the expected Linux/amd64 application-manifest and image-config digests. Docker SDK/runtime bases are pinned by digest.

Publication verifies GitHub artifact attestations before use. The complete OCI archive retains run-specific provenance/SBOM descriptors and is separately checksummed and attested, but those descriptors are not deployment identity because equivalent rebuilds may produce different provenance-index digests. Source timestamps are normalized so clean builds of the same merge commit reproduce the checked Linux/amd64 application manifest and config identities. Commit and version tags remain immutable; `stable` is the mutable deployment selector for the newest eligible stable-SemVer release. An existing `sha-<full-merge-sha>` image is reusable only when its application manifest, config digest, and OCI source/workflow/revision labels match the attested build. Old versioned images are never deleted.

The final release notes contain one delimited metadata block with version, merge SHA, application-manifest/config digests, NuGet SHA-256, and the separate provenance contract. Retries replace that block instead of appending duplicates.

## Retry behavior

- A prepare retry after branch push but before PR creation verifies and reuses the prepared branch, skips an unnecessary push, and still creates or repairs the PR.
- Re-running only failed jobs reuses the exact artifact name exported by the successful producing job. Re-running the full workflow creates a new attempt-qualified artifact without colliding with the prior attempt.
- Before any publication mutation, the workflow rejects a stable-SemVer release older than any non-prerelease draft or published release. A missing GitHub release is then created as a draft. An existing draft is reusable only when `target_commitish` is the exact merge SHA. Draft assets may then be repaired with replacement enabled; an already-published stable-SemVer release is rejected if GitHub marks it as a prerelease.
- A public release is never clobbered. Its checksum manifest must be byte-identical to the current run-attempt's attested manifest; its exact assets and checksums must pass, and its metadata must match this build's version, merge SHA, NuGet checksum, and image/config digests. It then performs no publication mutation.
- Main and symbol NuGet publication are separate required outcomes. Main-package duplicates are accepted only when payload entries match after excluding NuGet.org's repository-signing `.signature.p7s` and normalizing only signature-related declarations from `[Content_Types].xml`; every non-signature declaration remains part of the comparison. Symbol identity is downloaded independently from NuGet.org's v2 `symbolpackage` endpoint and compared without signing exclusions. If the main package exists but symbols do not, the `.snupkg` is pushed explicitly and both identities are downloaded and checked before `stable` can move.
- `stable` moves only after NuGet succeeds and is verified back to the expected digest. The GitHub release is finalized last, and the resulting Git tag is peeled if necessary and verified to resolve to the exact merge SHA.

## Debian host templates

The templates under `deploy/mudx-docs/` preserve container name `MudX`, bridge networking, `restart: unless-stopped`, and the health contract. Port 4560 is bound only to loopback as `127.0.0.1:4560:8080`; the public reverse proxy remains the only intended external route.

`mudx-docs.env` is the single authoritative deployment record consumed by Compose:

```dotenv
MUDX_DOCS_IMAGE=ghcr.io/mudxtra/mudx/mudxdocwebsite@sha256:<64-hex>
CURRENT_DIGEST=sha256:<64-hex>
PREVIOUS_DIGEST=
```

The updater obtains a nonblocking host `flock`, pulls `:stable` while the current container runs, resolves the repository digest, deploys the candidate with `docker compose up -d --wait`, and atomically replaces that one file only after health succeeds. If interrupted after candidate health but before the atomic rename, the old authoritative record remains valid and the next serialized run safely converges it. Normal health failure restores the prior digest.

The systemd unit adds filesystem/kernel/home restrictions, a private temporary directory, restrictive umask, and no-new-privileges. Docker socket access remains root-equivalent; the hardening limits unrelated host access but cannot sandbox Docker itself.

## Install and cutover gates

Installation, private-GHCR authentication, cutover, timer enabling, and rollback are explicit operator-approved production changes and are not performed by repository workflows.

1. Create `/opt/mudx-docs`, copy the Compose file, updater, and env example, and set the three authoritative values.
2. Authenticate Docker using the host credential store with package-read access. Never place a PAT in tracked files, the env file, unit, or shell history.
3. Validate with `docker compose --env-file mudx-docs.env -f compose.yml config`.
4. Pull the selected digest while the legacy `/MudX` remains running.
5. At the approved cutover, stop and rename the legacy container, then run `docker compose ... up -d --wait`.
6. Verify `http://127.0.0.1:4560/healthz` returns HTTP 200 with `Healthy`, then verify the public proxy route.
7. Install and verify the systemd templates only after cutover. Enabling `mudx-docs-update.timer` is a separate approval gate.

## Troubleshooting and rollback

- **Gate does not run:** confirm same-repository head, merged state, `release` label, and final `APPROVED` review decision.
- **Publication waits:** confirm the protected `release` Environment and required reviewers are configured.
- **Attestation or image mismatch:** stop. Do not retag or overwrite the existing commit/version image; investigate the workflow run and manifest provenance.
- **NuGet mismatch:** stop. Repository signing differences are already normalized; a remaining difference is a payload mismatch.
- **Updater reports another run:** wait for the timer/manual invocation holding the lock; do not bypass serialization.
- **Candidate health fails:** inspect Compose logs. The updater redeploys the prior authoritative image.
- **Interrupted after health:** rerun the updater. It redeploys/verifies the candidate and atomically advances the one authority file.

For manual rollback, select `PREVIOUS_DIGEST`, create a complete temporary three-line env record with that digest as both image and current value, validate it through Compose, deploy with `--wait`, and atomically replace `mudx-docs.env` only after health succeeds. Never move an immutable version tag to roll back.
