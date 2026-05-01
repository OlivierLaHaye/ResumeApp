# Branch cleanup audit — 2026-04-30

End-to-end audit and cleanup of every local and remote Git branch. Goal: merge clearly valuable, validated work into `main`; delete branches whose work is already represented in `main`; back up everything before deleting; validate no regression.

## Repo facts

- Repository: `OlivierLaHaye/ResumeApp` (https://github.com/OlivierLaHaye/ResumeApp).
- App type: WPF .NET 10 desktop app (`net10.0-windows`, `UseWPF=true`), with companion xUnit test project `ResumeApp.Tests`.
- Default branch: `main`.
- Remote: `origin` only.
- Build entrypoint: `ResumeApp.sln` (also includes the test project).
- Test runner: xUnit 2.x, Microsoft.NET.Test.Sdk 17.x, coverlet.collector 6.x, NSubstitute 5.x.
- Pre-build target: `RunResxCleaner` runs `ResxCleaner.exe` against `Properties/` before every build (re-orders `<data>` nodes).
- Beads tracking: `bd list` reported "no beads database found"; Beads is not initialized in this repo. Per spec, continued without it and documented here.
- Tooling versions used: `dotnet 10.0.201`, `gh 2.91.0`, Git on Windows 11.

## Starting state

- `main` SHA before cleanup: `4d9ada6e2e6ef75c0a10ed9b3ee2bf6a6fb75714` ("ResumeApp → Reposition as Senior Design Engineer with AI-Native and agent orchestrator framing (v3.1)").
- Working tree at session start: 14 tracked photo `.jpg` files under `Resources/Projects/Photography/**` showed `M` (working-tree size 10–40 MB vs. tracked 1–5 MB). These are user-side Dropbox-synced original assets, identical between every audited branch (`git diff --name-only main feat/... -- Resources/Projects/Photography/` returned empty). They were left untouched throughout cleanup. `.claude/` was untracked (Claude session worktree storage).
- Two stale Claude-session worktrees were registered:
  - `.claude/worktrees/competent-antonelli-1ec33f` on `main` (clean).
  - `.claude/worktrees/upbeat-cerf-868950` on `claude/upbeat-cerf-868950` (clean).
- GitHub PRs: a single historical PR, `#1 Add comprehensive test suite for 100% code coverage`, MERGED 2026-04-04 (squash-merge).

## Final state

- `main` SHA after cleanup: `26c96be` (will advance by one more commit for this audit doc + README index update; see "Post-merge commits on main").
- Local branches: `main` (active). All other local branches deleted.
- Remote branches: `origin/main`. All other remote branches deleted (and one short-lived `origin/maintenance/branchCleanup` removed at the end).
- 10 immutable backup tags pushed under `backup/branch-cleanup/*-20260430` for full recovery of every audited branch.

## Branch audit table

`L` = local, `R` = remote, `T` = tracking. `Ahead/Behind` = vs. `main` at session start.

| Branch | L | R | T | Ahead | Behind | Last commit | Classification |
| --- | --- | --- | --- | ---: | ---: | --- | --- |
| `main` | yes | yes | yes | — | — | 4d9ada6 (2026-04-30) | KEEP_PROTECTED |
| `claude/competent-antonelli-1ec33f` | yes | yes | yes | 0 | 0 | 4d9ada6 (2026-04-30) | DELETE_MERGED (= main) |
| `claude/cv-content-strategy-recruiter` | yes | yes | yes | 0 | 5 | 291a8ef (2026-04-08) | DELETE_MERGED (ancestor) |
| `claude/upbeat-cerf-868950` | yes | no | n/a | 0 | 1 | fdf0ba2 (2026-04-09) | DELETE_MERGED (ancestor, in worktree) |
| `codex/readme-portfolio-refresh` | yes | no | n/a | 0 | 1 | fdf0ba2 (2026-04-09) | DELETE_MERGED (ancestor, no upstream) |
| `docs/timeline-parity-audit` | yes | yes | yes | 1 | 1 | 1f3a97f (2026-04-16) | MERGE_VIA_FEAT then DELETE |
| `docs/timeline-horizontal-spec` | yes | yes | yes | 2 | 2 | 89ffae5 (2026-04-16) | MERGE_VIA_FEAT then DELETE |
| `feat/timeline-horizontal-v2-p0` | yes | yes | yes | 3 | 1 | 6a30d1f (2026-04-16) | MERGE_READY |
| `claude/polish-cv-app-ui-UuUpR` | no | yes | n/a | 0 | 9 | 8cffab7 (2026-04-05) | DELETE_MERGED (ancestor) |
| `claude/redesign-timeline-ux-cE5Zj` | no | yes | n/a | 0 | 15 | 529af66 (2026-04-05) | DELETE_MERGED (ancestor) |
| `claude/summary-app-full-coverage-9A34Q` | no | yes | n/a | 5 | 22 | 06a9aa7 (2026-04-03) | DELETE_SQUASHED (PR #1 squash-merged) |

Lineage detail for the timeline branches: `1f3a97f → 89ffae5 → 6a30d1f` is a chained sequence — `feat/timeline-horizontal-v2-p0` already contains both predecessor doc commits, so merging `feat` automatically retires both `docs/*` branches.

The `claude/summary-app-full-coverage-9A34Q` tip was not a literal ancestor of `main` because PR #1 was squash-merged on GitHub (the original 5 commits were collapsed into `dd1584c Add comprehensive test suite for 100% code coverage (#1)`), but the work is fully present in `main` (test project `ResumeApp.Tests/` exists with 514 baseline tests passing) and a backup tag preserves the original commit graph.

## Decisions

### Merged into main
- `feat/timeline-horizontal-v2-p0` → `main` via `--no-ff` merge into `maintenance/branchCleanup`, then fast-forward of `main`. Brings in:
  - `1f3a97f docs(timeline): app timeline audit, feature inventory, web parity matrix`.
  - `89ffae5 docs(timeline): desktop horizontal timeline spec, parity matrix, validation plan`.
  - `6a30d1f feat(timeline): phase P0 — localize Today marker and carry role as subtitle`.
  - Net diff: +1,579 lines / −4 lines across 17 files. Code: `Controls/TimelineControl.cs`, `Models/TimelineTimeFrameItem.cs`, `Pages/ExperiencePage.xaml`, `ViewModels/Pages/ExperiencePageViewModel.cs`. Resources: `Properties/Resources.resx` and `Properties/Resources.fr-CA.resx` add a single new key `TimelineLabelTodayMarker`. Tests: 3 new test files. Docs: 8 new files under `docs/timeline/` and `docs/screenshots/app-timeline/`.
  - Conflict analysis: the two branches that had touched `Properties/Resources.*resx` modified disjoint regions (`main`'s 4d9ada6 edited keys near lines 77/140/314/335/344/476/818; `feat` added a new key block near line 847). Three-way merge produced no conflicts.

### Deleted (already merged or squash-merged)
- Local: `claude/competent-antonelli-1ec33f`, `claude/cv-content-strategy-recruiter`, `claude/upbeat-cerf-868950`, `codex/readme-portfolio-refresh`, `docs/timeline-horizontal-spec`, `docs/timeline-parity-audit`, `feat/timeline-horizontal-v2-p0`.
- Remote: `origin/claude/competent-antonelli-1ec33f`, `origin/claude/cv-content-strategy-recruiter`, `origin/claude/polish-cv-app-ui-UuUpR`, `origin/claude/redesign-timeline-ux-cE5Zj`, `origin/claude/summary-app-full-coverage-9A34Q`, `origin/docs/timeline-horizontal-spec`, `origin/docs/timeline-parity-audit`, `origin/feat/timeline-horizontal-v2-p0`.

### Kept
- `main` (protected default).
- `maintenance/branchCleanup` (short-lived integration branch) was kept long enough to push the audit doc, then deleted at the very end.

### Worktrees removed
- `.claude/worktrees/competent-antonelli-1ec33f` (was on `main`, clean).
- `.claude/worktrees/upbeat-cerf-868950` (was on `claude/upbeat-cerf-868950`, clean).
- `git worktree remove` succeeded in deregistering both, but the now-empty parent directories could not be `rmdir`'d (Dropbox/FS handle: "Device or resource busy"). Git no longer treats them as worktrees and they will physically clean up on next OS reboot or Dropbox release.

## Backup tags

All ten tags pushed to `origin`. Each captures the SHA the branch had at audit time. Recovery: `git checkout -b <name> <tag>`.

| Tag | Original branch | Captured SHA |
| --- | --- | --- |
| `backup/branch-cleanup/claude-competent-antonelli-1ec33f-20260430` | `claude/competent-antonelli-1ec33f` | 4d9ada6 |
| `backup/branch-cleanup/claude-cv-content-strategy-recruiter-20260430` | `claude/cv-content-strategy-recruiter` | 291a8ef |
| `backup/branch-cleanup/claude-upbeat-cerf-868950-20260430` | `claude/upbeat-cerf-868950` | fdf0ba2 |
| `backup/branch-cleanup/codex-readme-portfolio-refresh-20260430` | `codex/readme-portfolio-refresh` | fdf0ba2 |
| `backup/branch-cleanup/docs-timeline-horizontal-spec-20260430` | `docs/timeline-horizontal-spec` | 89ffae5 |
| `backup/branch-cleanup/docs-timeline-parity-audit-20260430` | `docs/timeline-parity-audit` | 1f3a97f |
| `backup/branch-cleanup/feat-timeline-horizontal-v2-p0-20260430` | `feat/timeline-horizontal-v2-p0` | 6a30d1f |
| `backup/branch-cleanup/origin-claude-polish-cv-app-ui-UuUpR-20260430` | `origin/claude/polish-cv-app-ui-UuUpR` | 8cffab7 |
| `backup/branch-cleanup/origin-claude-redesign-timeline-ux-cE5Zj-20260430` | `origin/claude/redesign-timeline-ux-cE5Zj` | 529af66 |
| `backup/branch-cleanup/origin-claude-summary-app-full-coverage-9A34Q-20260430` | `origin/claude/summary-app-full-coverage-9A34Q` | 06a9aa7 |

Recovery example: `git fetch origin tag backup/branch-cleanup/feat-timeline-horizontal-v2-p0-20260430 && git checkout -b feat/timeline-horizontal-v2-p0 backup/branch-cleanup/feat-timeline-horizontal-v2-p0-20260430`.

## Validation

| Stage | Command | Result |
| --- | --- | --- |
| Baseline restore | `dotnet restore ResumeApp.sln --nologo -v:minimal -m:1` | "All projects are up-to-date for restore." |
| Baseline build (main = 4d9ada6) | `dotnet build ResumeApp.sln -c Release --nologo --no-restore -m:1 -v:minimal` | 0 warning(s), 0 error(s), ~11.6s |
| Baseline test (main) | `dotnet test ResumeApp.sln -c Release --no-build --nologo --verbosity normal` | Total 514, Passed 514, Failed 0, ~5.7s |
| Post-merge build (main + feat = 26c96be) | same | 0 warning(s), 0 error(s), ~8.4s |
| Post-merge test | same | Total 523, Passed 523, Failed 0, ~5.7s (delta = +9 tests from feat's new test files) |
| Branch ancestry verification | `git merge-base --is-ancestor <branch> main` | All seven local + seven of eight remote candidates returned ANCESTOR_OF_MAIN; `origin/claude/summary-app-full-coverage-9A34Q` returned NOT_ANCESTOR but is squash-merged via PR #1 (verified via `gh pr list --state all`). |

`Properties/Resources.resx` and `Properties/Resources.fr-CA.resx` are touched by the `RunResxCleaner` pre-build target on every build (cosmetic node reorder). Those automatic edits were reset with `git checkout -- Properties/Resources*.resx` after each build round; they were never committed.

## Commands used (chronological)

```
git status -s
git remote -v
git rev-parse HEAD
git rev-parse origin/main
git branch -a -vv
git worktree list
git fetch --all --prune --tags

# audit (one example, run for every branch)
git rev-list --left-right --count main...<branch>
git merge-base --is-ancestor <branch> main
git log --oneline main..<branch>
git diff --stat main...<branch>
git log --oneline --graph --all -15

# baseline
dotnet restore ResumeApp.sln --nologo -v:minimal -m:1
dotnet build  ResumeApp.sln -c Release --nologo --no-restore -m:1 -v:minimal
dotnet test   ResumeApp.sln -c Release --no-build --nologo --verbosity normal
git checkout -- Properties/Resources.resx Properties/Resources.fr-CA.resx

# integrate
git branch maintenance/branchCleanup main
git checkout maintenance/branchCleanup
git merge --no-ff feat/timeline-horizontal-v2-p0 -m "Merge feat/timeline-horizontal-v2-p0 into main: phase P0 timeline + docs ..."

# post-merge validation
dotnet build ResumeApp.sln -c Release --nologo --no-restore -m:1 -v:minimal
dotnet test  ResumeApp.sln -c Release --no-build --nologo --verbosity normal
git checkout -- Properties/Resources.resx Properties/Resources.fr-CA.resx

# backup tags
for each candidate: git tag backup/branch-cleanup/<name>-20260430 <branch>

# push
git push --set-upstream origin maintenance/branchCleanup
git checkout main
git merge --ff-only maintenance/branchCleanup
git push origin main
git push origin --tags 'backup/branch-cleanup/*'

# worktrees
git worktree remove .claude/worktrees/competent-antonelli-1ec33f
git worktree remove .claude/worktrees/upbeat-cerf-868950

# delete branches
git branch -d <each merged local branch>
git push origin --delete <each merged remote branch>
git fetch --all --prune
```

## Known leftovers and follow-ups

- The two empty worktree directories under `.claude/worktrees/` could not be `rmdir`'d due to a Windows file-system / Dropbox handle. They are no longer registered in `.git/worktrees/` and are inert; OS reboot or Dropbox sync release will free them. Not a blocker.
- The 14 user-side photo `.jpg` working-tree modifications under `Resources/Projects/Photography/**` were left as-is. They are larger originals being staged by Dropbox sync and were intentionally not committed because they are out-of-scope for this maintenance task. The user can review and commit (or revert) them at their convenience; they did not interfere with build, test, or merge.
- Beads is not initialized in this repo. If recurring maintenance tracking is desired, run `bd init` at the repo root.
