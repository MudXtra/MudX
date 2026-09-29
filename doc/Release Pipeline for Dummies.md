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

Deployment remains SSH-based. During an approved activation, the dedicated key must be restricted to a non-interactive sudo invocation of `mudx-deploy-ssh`; callers provide only a monotonic sequence, canonical version, source SHA, and image digest. That forced-command wrapper validates the exact grammar and forwards it without a shell. The root `mudx-deploy` program hard-codes the image repository and runtime allowlist; neither layer accepts arbitrary Docker flags, paths, mounts, commands, or rollback targets.

The host transaction does the following:

1. takes a host-wide lock and rejects stale or conflicting releases;
2. pulls the candidate digest while the current container is still available, or records an explicit no-prior state for a first installation;
3. journals named stages durably, but at this audited head does not persist create intent before Docker's candidate-creation side effect;
4. disables the previous container's restart policy before stopping it;
5. starts the candidate with restart policy `no`, checks `/healthz`, and records the committed state;
6. restores the retained previous container if candidate verification fails.

The supplied systemd units are designed for systemd—not Docker restart policy—to be the single restart owner. They manage the existing production container name, `MudX`, so preparing the source does not require a live rename. An explicit systemd start transition clears a prior stopped marker once before the continuous supervisor launches. The supervisor never clears that marker, so a newer planned stop remains dominant even when it completes before supervisor initialization. The supervisor waits for the container to exit, applies the two-second restart delay after every observed exit, and only then restarts it. Every deploy, journal reconciliation, explicit start, and planned service stop coordinates through the same host lock and durable maintenance intent. Reconciliation repairs an interrupted journal before the supervisor starts the recovered container.

A planned stop owns a single 120-second handoff budget across the stop-intent and common locks, then has a separate 40-second Docker-operation budget; the unit’s 165-second stop timeout covers both budgets plus a five-second manager margin. Deployment Docker calls are individually bounded at 70 seconds and recovery shares a 40-second budget, so an in-flight call plus stopped rollback and margin fits inside the handoff budget. Deploy checks the durable stop intent after every blocking boundary and serializes the final promotion/commit against stop intent. If stop arrives during deployment, the deployment fails, restores naming without restarting the prior container, and leaves Docker restart disabled. If either stop budget expires, the stopped marker remains durable and a later explicit systemd start is required.

Deployment rejects while journal recovery is pending, including an otherwise exact duplicate release. Deployment maintenance intent is cleared only after a successful deployment or successful rollback; a failed rollback leaves it for the supervisor’s next successful reconciliation. A terminal successful deployment, rollback, or reconciliation consumes its journal so a later boot cannot replay completed recovery; failed recovery preserves both journal and maintenance evidence. The journal records prior and candidate container IDs before later rename boundaries, allowing rollback to distinguish an uncommitted promoted candidate from an already-restored prior container and resume after a partial rollback. It currently records the candidate ID only after `docker create` returns, so the create-success-before-journal window is not replay-safe; the intended recovery model is conditional on the Miss 12 write-ahead correction below. Planned stop preserves any maintenance marker it did not create.

The units alone cannot stop Docker from auto-starting an older container that still has `always` or `unless-stopped` while the daemon itself starts. During the separately approved cutover, disable and verify the existing Docker restart policy before enabling the supervisor. The legacy updater must also be stopped or wrapped by the same deployment interlock for that window; do not run it concurrently. Preserve its rollback copy and retire its embedded registry credential only through an approved credential rotation. A disposable Docker/systemd reboot test must prove one port owner, recovery after a runtime crash, journal reconciliation, and no deploy/supervisor race before production activation.

The checked-in wrapper and unit files are installation sources, not proof of host installation. Dedicated account creation, forced-command SSH policy, root ownership, registry credentials, recovery-automation interlock, firewall reachability, systemd installation, and the first live release require separate operational approval and verification. Publication and resume jobs explicitly select .NET SDK 10.0.400 so the NuGet environment-key path does not depend on the mutable runner image.

## Artifact provenance and actual use

This inventory was audited against PR #83 base `de62548cf335af9f4e065c78fde7b82b41c424e9` and head `bd0bd2415e2bbc30de08fbddb4a7f945f0312227`. PR #81, **Add secure resumable MudX release pipeline**, introduced the original pipeline in commit `14794c6ce07c017ac2f7f557707947ae3eb76a95`; its commit `7e08c759fbf13ec98944daac41a243b24453e255` then hardened the workflow, deploy program, guide, deploy-state tests, and release-guard tests. PR #83 introduced the three paths marked new below in `f80f528c47e1eea189c0294cd96f95d4a5915844`.

Repository presence is not runtime evidence. At the audited head, GitHub registered `Release_MudX.yml` as active but reported **zero workflow runs**. No checked-in workflow invokes `tests/release_pipeline`; those Python tests have been run manually, not enforced by CI. The checked-in host files are installation sources, not installed-state evidence. PR #83's recorded evidence says production remained unchanged, and neither the repository nor the PR establishes production installation or runtime use. PR #83 therefore remains pre-activation work.

| # | Artifact | Origin and base behavior before PR #83 | Actual-use evidence | PR #83 behavior now | Intended use and residual gap |
|---:|---|---|---|---|---|
| 1 | `.github/workflows/Release_MudX.yml` | PR #81, `14794c6`; further hardened by `7e08c75`. The base workflow already coordinated version PR creation, producer-bound build artifacts, protected publication/deployment, and retained-artifact resume. | **Repository:** read by `test_release_guard.py` and self-binds workflow metadata.<br>**Test/CI:** its structure is inspected by manual Python tests; the release-pipeline tests are not called by checked-in CI.<br>**Release execution:** GitHub reports zero runs.<br>**Production:** no deployment run evidence. | Adds pinned `actions/setup-dotnet` with SDK `10.0.400` to both publish and resume publication jobs. | **After merge:** remains the manual release coordinator on protected `dev`.<br>**After approved activation:** may call the SSH protocol and verify the public revision.<br>**Gap:** neither release nor deployment behavior has been exercised by this workflow. |
| 2 | `deploy/mudx-deploy` | PR #81, `14794c6`; hardened by `7e08c75`. The base program managed `mudxdocwebsite` with `deploy`/`reconcile`, a lock, journal, candidate health check, promotion, and basic rollback. | **Repository:** called by the forced-command wrapper and both systemd units; directly loaded/executed by `test_deploy_state.py`.<br>**Test/CI:** extensive manual fake-Docker and real-Docker evidence exists, but no checked-in CI caller.<br>**Release execution:** zero release-workflow runs.<br>**Production:** installation/runtime not established. | Manages `MudX`; adds stopped/maintenance intent, bounded Docker calls, `resume`, `supervise`, and `stop`; expands replay handling and binds rollback mutations to recorded IDs. | **After merge:** reviewed host-program source only; current recovery is known broken at two identity/write-ahead boundaries.<br>**After approved activation:** root-owned `/usr/local/libexec/mudx-deploy`, reached only through the wrapper or systemd, but only after both blockers below are fixed and verified.<br>**Blockers:** committed-candidate reconcile trusts a mutable name (Miss 11), and candidate creation lacks a durable write-ahead intent before `docker create` (Miss 12). |
| 3 | `deploy/mudx-deploy-ssh` | New in PR #83 at `f80f528`; no base path. | **Repository:** allowed by the sudoers source and executed by `test_forced_command.py`.<br>**Test/CI:** grammar, preflight, shell-free forwarding, and fixed `/usr/bin/python3` are tested manually; not CI-enforced.<br>**Release execution:** zero workflow runs.<br>**Production:** installation/authorized-key use not established. | Accepts only `preflight` or exact `deploy SEQUENCE VERSION SHA DIGEST` grammar, then `execv`s the fixed root deploy path without a shell. | **After merge:** installation source.<br>**After approved activation:** root-owned `/usr/local/libexec/mudx-deploy-ssh`, invoked as the dedicated account's forced sudo command.<br>**Gap:** the repository has no installer or `authorized_keys` manager. |
| 4 | `deploy/mudx-deploy.sudoers` | New in PR #83 at `f80f528`; no base path. | **Repository:** authorizes only the installed wrapper and preserves only `SSH_ORIGINAL_COMMAND`; no checked-in installer references the filename.<br>**Test/CI:** no direct sudo-policy execution in checked-in CI.<br>**Release execution:** zero workflow runs.<br>**Production:** installation not established. | Defines the narrow `mudxdeploy` → root wrapper boundary. | **After merge:** sudoers installation source.<br>**After approved activation:** validate with `visudo`, install root-owned under `/etc/sudoers.d`, and pair with the forced SSH command.<br>**Gap:** host account/key provisioning and installed-policy validation are operational work. |
| 5 | `deploy/systemd/mudx-container.service` | PR #81, `14794c6`. The base oneshot directly started/stopped Docker container `mudxdocwebsite` and remained active after exit. | **Repository:** ordered after `mudx-reconcile.service` and inspected by `test_deploy_state.py`.<br>**Test/CI:** timeout and command wiring are tested manually; no checked-in CI caller.<br>**Release execution:** zero workflow runs.<br>**Production:** unit installation/enablement not established. | Becomes a continuously restarted supervisor that calls deploy `resume`, `supervise`, and `stop`, with a 165-second stop timeout. | **After merge:** unit-file source.<br>**After approved activation:** root-owned systemd unit installed with `mudx-reconcile.service`, daemon-reloaded, then enabled only after cutover proof.<br>**Gap:** enabling it before disabling legacy restart ownership can create competing starters. |
| 6 | `doc/Release Pipeline for Dummies.md` | PR #81, `14794c6`; hardened by `7e08c75`. The base guide described the release identity, resumable publication, basic deploy/reconcile model, and the separate activation requirement. | **Repository:** human-maintainer documentation; no executable caller.<br>**Test/CI:** no documentation assertion covers this guide.<br>**Release execution:** not applicable.<br>**Production:** not evidence of installation. | PR #83 expanded stop/supervisor/recovery timing and ownership guidance; this audit adds exact provenance, usage status, wiring, tests, CI gap, activation boundary, and blocker. | **After merge:** maintainer source of truth for the proposed pipeline.<br>**After approved activation:** operational checklist, not an automated installer.<br>**Gap:** must remain synchronized with state-machine fixes and actual host evidence. |
| 7 | `tests/release_pipeline/test_deploy_state.py` | PR #81, `14794c6`; hardened by `7e08c75`. The base six tests covered grammar/stale/duplicate behavior, concurrency, runtime validation, stage interruptions, first install, and rollback. | **Repository:** directly exercises `deploy/mudx-deploy`, fake Docker, and systemd source.<br>**Test/CI:** run manually; no checked-in workflow invokes it.<br>**Release execution:** tests are not part of the release workflow.<br>**Production:** test evidence only. | Expands to state, lock, timeout, stop/supervisor, replay, marker-ownership, and adversarial rollback-ID cases. | **After merge:** regression suite for host state-machine changes.<br>**After approved activation:** remains pre-deployment assurance, supplemented by disposable real Docker/systemd tests.<br>**Gaps:** it does not cover the committed-candidate reconciliation identity mismatch or a kill/timeout after successful `docker create` but before `candidate_id` is journaled. |
| 8 | `tests/release_pipeline/test_forced_command.py` | New in PR #83 at `f80f528`; no base path. | **Repository:** executes `deploy/mudx-deploy-ssh` with a temporary target.<br>**Test/CI:** four manual unit tests; no checked-in workflow invokes them.<br>**Release execution:** not part of the release workflow.<br>**Production:** test evidence only. | Verifies preflight, absolute interpreter provenance, exact forwarding without a shell, and rejection of ambiguous/arbitrary commands. | **After merge:** regression suite for the SSH trust boundary.<br>**After approved activation:** supplements, but does not replace, host ownership/sudoers/authorized-key checks.<br>**Gap:** no live SSH/sudo installation is exercised. |
| 9 | `tests/release_pipeline/test_release_guard.py` | PR #81, `14794c6`; hardened by `7e08c75`. The base tests covered versions/actors, version-only diffs, manifests, package equivalence, resume planning/artifact selection, head binding, and workflow/action pins. | **Repository:** executes `tools/release_pipeline/release_guard.py` and inspects the workflow and publisher.<br>**Test/CI:** run manually; no checked-in workflow invokes it.<br>**Release execution:** not part of the release workflow.<br>**Production:** test evidence only. | Adds checks that publish and resume jobs pin setup-dotnet and SDK `10.0.400`. | **After merge:** regression suite for coordinator and publisher guards.<br>**After approved activation:** remains repository assurance only.<br>**Gap:** source inspection does not prove a GitHub Actions run or host deployment. |

## Manual release-pipeline tests and CI boundary

From the repository root, run the complete Python release-pipeline suite with:

```bash
python3 -m unittest discover -s tests/release_pipeline -p 'test_*.py'
```

This command discovers all checked-in release-pipeline Python tests, including the three PR #83 files above. It is a required manual check today. No file under `.github/workflows/` invokes this command, `tests/release_pipeline`, or these test modules, so a green repository PR check must not be described as proof that this suite ran. Adding CI enforcement is a separate workflow change and is outside this documentation-only audit.

## Host wiring and activation boundary

Merging these files does not install them. A separately approved activation must, at minimum:

1. install `deploy/mudx-deploy` and `deploy/mudx-deploy-ssh` as root-owned executables at `/usr/local/libexec/mudx-deploy` and `/usr/local/libexec/mudx-deploy-ssh`;
2. validate `deploy/mudx-deploy.sudoers` with `visudo`, then install it root-owned under `/etc/sudoers.d`;
3. configure the dedicated SSH account and key so its forced command invokes the installed wrapper through non-interactive sudo, while disabling shell, forwarding, and unrelated SSH capabilities;
4. install both `deploy/systemd/mudx-reconcile.service` and `deploy/systemd/mudx-container.service` as root-owned systemd units, then run `systemctl daemon-reload`;
5. provision the approved registry access and verify host-key pinning, firewall reachability, the public health/revision endpoints, file hashes, permissions, and command paths;
6. stop or interlock the legacy updater, disable and verify the old container's Docker restart policy, and complete disposable Docker/systemd reboot and fault-injection proof; and
7. only then, under explicit cutover approval, enable/start the units and permit the first live release.

Do not run those steps merely because the PR merged. The repository contains no host installer, and this guide intentionally does not contain account names, keys, credentials, or host-specific commands.

## Known recovery blockers at the audited head

PR #83 is **not merge-ready or activation-ready** at the audited head. The intended post-fix use described above is conditional on closing both known recovery defects; the current implementation is known broken in these paths.

- **Miss 11 — committed-current identity:** the committed-candidate branch of `reconcile()` selects `current['container']` by mutable name for restart-policy update and optional start, then clears the journal without checking that the live container matches a durable candidate ID. If that name has been reassigned, recovery can mutate the replacement and erase its recovery evidence. The fix must persist and validate the committed candidate's immutable ID, perform no update/start on missing or mismatched identity, and retain the journal on failure.
- **Miss 12 — candidate-create write-ahead gap:** after the prior container has been renamed and the journal records `prior-renamed`, `docker create` can succeed before `candidate_id` and `candidate-created` are durably journaled. A process kill or client timeout in that window leaves a named candidate with no recorded identity; current reconciliation fails with `candidate container identity unavailable` and cannot automatically restore service. The fix must durably record create intent before the call, then on recovery validate the discovered container's expected image and labels before adopting its immutable ID or removing it. This was reported for exact head `bd0bd241` in [discussion 4138934778](https://github.com/MudXtra/MudX/pull/83#discussion_r4138934778).

Both fixes require deterministic wrong-ID, missing-ID, matching-ID, and create-succeeded-before-journaling regressions. Recovery must retain its durable evidence whenever identity or provenance cannot be proven.

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
