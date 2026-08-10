# Workflow: spec issue → sub-issue → PR

How features move from plan to merged code in this repo. The same workflow runs in
production on [jme-website-drupal](https://github.com/SomethingLikeAChicken/jme-website-drupal)
(see e.g. issue #87 → PR #88 there) and ran on the Phaser-era predecessor repo
([one-more-knight#1](https://github.com/SomethingLikeAChicken/one-more-knight/issues/1)).
Written down here so future agents follow it without re-deriving it.

## The three artifacts

### 1. The spec issue (PRD) — one per project

**Issue #1** is the living plan: vision, locked decisions, milestone roadmap, test
plan, and a sub-issue checklist. Labels: `epic`, `documentation`.

- It is **never closed** while the project runs; it is edited as reality changes.
- Every sub-issue and PR says `Part of #1`.
- When a sub-issue closes, tick its box in the spec's checklist.
- Larger feature areas may get their own `spec`-labelled design issue under the PRD
  (jme example: #82), with implementation sub-issues hanging off that.

### 2. Sub-issues — one per testable feature

**Before starting work on a feature, create its sub-issue.** Not a batch upfront —
the decomposition policy is *current + next milestone only*; an issue is created when
work on it actually starts (or when it's specified well enough for triage).

A sub-issue must be independently reviewable and **testable**. Body template:

```markdown
Part of #1. <One-line context: what stage this builds on, e.g. "Builds on #12.">

<Problem: what is missing or wrong, in CONTEXT.md vocabulary.>

## Scope

- <What will be built, concrete enough to review against.>
- <Explicit non-goals if the boundary is easy to overshoot.>

## Acceptance

- [ ] <Each item a human can verify by playing / running tests / opening the build.>
- [ ] Console clean: 0 errors, 0 warnings entering and exiting play mode.
```

Housekeeping:

- **Milestone:** assign the GitHub milestone (M0–M4 exist, mapped 1:1 to the roadmap).
- **Labels:** `enhancement` (or `bug`), plus a triage label per `triage-labels.md` —
  `ready-for-agent` once fully specified.
- **Vocabulary:** titles and text use `CONTEXT.md` terms; conflicts with an ADR are
  surfaced in the issue, never silently overridden (`domain.md`).

### 3. Pull requests — exactly one per sub-issue

- **Branch:** `feature/<issue-nr>-<short-slug>` off `main` (e.g. `feature/3-webgl-build-target`).
- **Title:** the sub-issue's title (or a tightened version) suffixed with the issue
  number: `Switch build target to WebGL (#3)`.
- **Body:** starts with `Part of #1. Closes #<issue-nr>.`, then:
  - `## What changed` — what was built and *why it looks like that*; call out
    trade-offs and any deviation from the issue's scope.
  - `## Tested` — how each acceptance item was verified (play test, test run, build).
- **Merge:** after review, merge into `main` and delete the branch. `Closes #<nr>`
  auto-closes the sub-issue; then tick its box in #1.
- Stacked PRs are allowed when features build on each other — note the merge order in
  the PR body (jme example: PR #88 "Stacked on #86 — merge order #85 → #86 → this").

## Rules of thumb

- **No feature work without a sub-issue.** If you're about to write game code and no
  issue covers it, create the issue first — that is the reviewable unit.
- **One PR per issue, one issue per PR.** If a PR grows past its issue, split it.
- **A sub-issue is closeable only when every acceptance item is demonstrably true**
  (manually playable check and/or green build/tests) — that is the definition of done.
- Docs-only changes (ADRs, glossary, this file) may go straight to `main` without a
  sub-issue; game code never does.
- Issue-tracker mechanics (gh CLI conventions) live in `issue-tracker.md`; triage
  labels in `triage-labels.md`.
