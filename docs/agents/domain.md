# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, or
- **`CONTEXT-MAP.md`** at the repo root if it exists — it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`docs/adr/`** — read ADRs that touch the area you're about to work in. In multi-context repos, also check per-context `docs/adr/` folders for context-scoped decisions.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The producer skill (`/grill-with-docs`) creates them lazily when terms or decisions actually get resolved.

## File structure

This repo is **single-context**: one `CONTEXT.md` + `docs/adr/` at the repo root.

It is also a **Unity project** (ADR-0002), so the repo root is the Unity project
root. Documentation lives at the repo root and deliberately **not** under
`Assets/` — Unity imports everything under `Assets/` as an asset and generates a
`.meta` file per document. Keep docs outside it.

```
/
├── AGENTS.md                  ← contributor contract
├── CLAUDE.md                  ← pointer to the above
├── CONTEXT.md                 ← domain glossary
├── docs/
│   ├── adr/
│   │   ├── 001-english-only.md
│   │   ├── 002-unity-over-phaser-web.md
│   │   ├── 003-data-driven-attack-pattern-engine.md
│   │   ├── 004-custom-backend-oauth.md
│   │   └── 005-trust-based-leaderboard-v1.md
│   └── agents/
│       ├── issue-tracker.md
│       ├── triage-labels.md
│       └── domain.md
├── Assets/                    ← Unity assets; game code goes in Assets/Scripts/
│   ├── Scenes/
│   └── Settings/
├── Packages/                  ← Unity package manifest + lock
└── ProjectSettings/
```

ADR files are numbered with **three digits** (`001-`), while prose refers to them
as `ADR-0001`. Both forms are in use; match the filenames on disk when linking.

`Assets/Scripts/` does not exist yet — the project is still an empty URP 2D
template (one scene, a camera, and a global light). Nothing in `CONTEXT.md` is
implemented yet; the glossary describes the intended domain, not existing code.

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/grill-with-docs`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0003 (data-driven attack pattern engine) — but worth reopening because…_