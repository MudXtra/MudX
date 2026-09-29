# Release Pipeline for Dummies

## MudX edition

This guide explains the repository's release workflow. The workflow and host-side files must be reviewed and configured before the first production release; committing them does not install anything on a server.

## One-minute guide

1. Open **Actions → Release MudX** on the protected `dev` branch.
2. Choose **patch**, **minor**, or **major**. A canonical stable-version override is available for exceptional cases.
3. Start the workflow using an authorized release account.
4. The workflow creates a version-only pull request. MudXBot approves that exact head only after the workflow verifies the initiator, branch, and full diff. Required checks and branch protection still decide when it can merge.
5. Review the protected-environment summary for the exact version, source commit, package checksums, and image digest.
6. Approve publication and deployment only if those identities are correct.
7. Read the final result. A package upload, image upload, or started SSH command is not by itself a successful release.

To resume partial publication, dispatch the same workflow in **resume** mode and enter the original producer run ID and attempt. Resume downloads that retained artifact, authenticates its original run, manifest, paths, checksums, version, source SHA, and image digest, and never runs the build or version-allocation jobs.

## What the workflow binds together

A release has one stable version and one merged source SHA. The build produces:

- `MudX.MudBlazor.Extension` packages for the existing .NET 8, 9, and 10 targets;
- generated static assets and docs;
- a website image labeled with the exact source revision;
- a manifest containing the source SHA, version, producer run/attempt, image digest, and file checksums.

The protected publication job accepts only that producer-bound manifest. It reconciles the immutable image and main NuGet package before publishing anything missing. Published NuGet content must match every package entry exactly; only NuGet's documented repository-signature entry, `.signature.p7s`, may differ. It creates an authenticated draft release containing the exact package assets, publishes symbols explicitly, records the symbol package checksum in that draft, deploys the same digest, verifies and finalizes the GitHub release, and only then updates the compatibility `latest` tag from the immutable digest. A failure after an irreversible upload is reported as partial completion; it is not described as a rollback.

All third-party Actions used by this workflow are pinned to immutable commit SHAs with readable major-version comments.

## Version pull-request safety

The generated pull request is not trusted because of its title or label. Before MudXBot approves it, the workflow verifies:

- both the original and rerun actors are in the release allowlist;
- the coordinator and MudXBot tokens identify distinct accounts;
- the current head is still the head created by this release run;
- the complete diff changes only the single `<Version>` value in `src/MudX/MudX.csproj`;
- the value is canonical stable SemVer and matches the calculated release.

The fetched pull-request commit and fresh API head are checked against the expected SHA before approval, and the head is queried again immediately before auto-merge. A changed head invalidates the authorization. If checks or merge protection take too long, continue the same pull request instead of allocating another version.

## Server deployment model

Deployment remains SSH-based. The key is restricted to the fixed `mudx-deploy` command; callers provide only a monotonic sequence, canonical version, and SHA-256 digest. The wrapper hard-codes the image repository and runtime allowlist. It does not accept arbitrary Docker flags, paths, mounts, commands, or rollback targets.

The host transaction does the following:

1. takes a host-wide lock and rejects stale or conflicting releases;
2. pulls the candidate digest while the current container is still available, or records an explicit no-prior state for a first installation;
3. journals each stage durably;
4. disables the previous container's restart policy before stopping it;
5. starts the candidate with restart policy `no`, checks `/healthz`, and records the committed state;
6. restores the retained previous container if candidate verification fails.

The supplied systemd units are designed for systemd—not Docker restart policy—to be the single restart owner. They manage the existing production container name, `MudX`, so preparing the source does not require a live rename. An explicit systemd start transition clears a prior stopped marker once before the continuous supervisor launches. The supervisor never clears that marker, so a newer planned stop remains dominant even when it completes before supervisor initialization. The supervisor waits for the container to exit, applies the two-second restart delay after every observed exit, and only then restarts it. Every deploy, journal reconciliation, explicit start, and planned service stop coordinates through the same host lock and durable maintenance intent. Reconciliation repairs an interrupted journal before the supervisor starts the recovered container.

A planned stop owns a single 120-second handoff budget across the stop-intent and common locks, then has a separate 40-second Docker-operation budget; the unit’s 165-second stop timeout covers both budgets plus a five-second manager margin. Deployment Docker calls are individually bounded at 70 seconds and recovery shares a 40-second budget, so an in-flight call plus stopped rollback and margin fits inside the handoff budget. Deploy checks the durable stop intent after every blocking boundary and serializes the final promotion/commit against stop intent. If stop arrives during deployment, the deployment fails, restores naming without restarting the prior container, and leaves Docker restart disabled. If either stop budget expires, the stopped marker remains durable and a later explicit systemd start is required.

Deployment rejects while journal recovery is pending, including an otherwise exact duplicate release. Deployment maintenance intent is cleared only after a successful deployment or successful rollback; a failed rollback leaves it for the supervisor’s next successful reconciliation. A terminal successful deployment, rollback, or reconciliation consumes its journal so a later boot cannot replay completed recovery; failed recovery preserves both journal and maintenance evidence. Planned stop preserves any maintenance marker it did not create.

The units alone cannot stop Docker from auto-starting an older container that still has `always` or `unless-stopped` while the daemon itself starts. During the separately approved cutover, disable and verify the existing Docker restart policy before enabling the supervisor. The legacy updater must also be stopped or wrapped by the same deployment interlock for that window; do not run it concurrently. Preserve its rollback copy and retire its embedded registry credential only through an approved credential rotation. A disposable Docker/systemd reboot test must prove one port owner, recovery after a runtime crash, journal reconciliation, and no deploy/supervisor race before production activation.

The checked-in wrapper and unit files are installation sources, not proof of host installation. Dedicated account creation, forced-command SSH policy, root ownership, registry credentials, recovery-automation interlock, firewall reachability, systemd installation, and the first live release require separate operational approval and verification. Publication and resume jobs explicitly select .NET SDK 10.0.400 so the NuGet environment-key path does not depend on the mutable runner image.

## Honest failure states

- **Version PR pending:** wait or resume the same PR/run identity.
- **Build or test failure:** nothing publishes.
- **Image published, NuGet failed:** image publication succeeded; package publication did not.
- **Main package published, symbols missing:** resume the original producer run/attempt; it reuses the retained artifact and attempts only the missing symbol stage.
- **Symbol upload returned an immutable conflict without a finalized exact release:** public NuGet APIs cannot prove symbol-package identity, so stop for manual reconciliation; never use skip-duplicate as proof.
- **Packages published, deployment failed:** resume only the original producer run/attempt. The workflow downloads and revalidates that retained artifact; the authenticated draft checksum proves a completed symbol stage so deployment can continue without republishing symbols. It never rebuilds or allocates another version.
- **Retained artifact missing or expired:** stop. Do not rebuild the supposedly same release.
- **SSH disconnected:** the workflow checks the cache-busted public revision; if it still cannot prove the target SHA, inspect the host journal before retrying.
- **Candidate unhealthy:** the wrapper attempts to restore the retained prior container; failed recovery is an incident.
- **Manifest, producer, or checksum mismatch:** stop before credentials are used.

## Required repository and environment configuration

Before enabling production use, reviewers must verify the protected `mudx-release-coordination` and `mudx-production-release` environments, release actor and bot-login variables, distinct coordinator and MudXBot credentials, package/registry credentials, SSH host-key pinning, the restricted deploy key, and repository branch/ruleset behavior. No secret values belong in documentation or workflow logs.

## Maintainer checks

Run the release Python tests, workflow linting, shell syntax and linting, target-framework builds, docs build, existing unit tests, image smoke test, and disposable Docker/systemd fault-injection tests. In particular, test every journal boundary, pull and health failures, duplicate/stale releases, concurrent attempts, unsafe runtime configuration, lost SSH, reboot recovery, and partial package publication.

For any deployment-state change, review one repository-local matrix before approval: each command (deploy, stop, resume, supervise, reconcile); ownership and authorized clearing of stopped, maintenance, journal, and current state; common and stop-intent lock order; prior/candidate/promoted/missing/running/exited container states; every Docker/network wait and its budget equation; and the interleavings deploy→stop, stop→deploy, stop timeout, failed rollback plus stop, borrowed maintenance plus stop, reconcile failure, crash restart, concurrent resume, daemon restart, and reboot. Add a deterministic transition or interleaving regression for every changed cell; a happy-path command test is not sufficient evidence for a cross-command invariant.
