# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`Assets/CONTEXT.md`**, or
- **`Assets/CONTEXT-MAP.md`** if it exists — it points at one `CONTEXT.md` per context. Read each one relevant to the topic.
- **`Assets/docs/adr/`** — read ADRs that touch the area you're about to work in. In multi-context repos, also check per-context `docs/adr/` folders for context-scoped decisions.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The producer skill (`/grill-with-docs`) creates them lazily when terms or decisions actually get resolved.

## File structure

This repo is **single-context**: one `CONTEXT.md` + one `docs/adr/`.

It is also a **Unity project** (ADR-0002). The repo root is the Unity project root,
but everything worked on by hand lives under `Assets/` — so the docs live there too,
next to the code they describe.

```
/
├── Assets/                     ← the working tree; docs and game code both live here
│   ├── AGENTS.md               ← contributor contract
│   ├── CLAUDE.md               ← pointer to the above
│   ├── CONTEXT.md              ← domain glossary
│   ├── docs/
│   │   ├── adr/
│   │   │   ├── 001-english-only.md
│   │   │   ├── 002-unity-over-phaser-web.md
│   │   │   ├── 003-data-driven-attack-pattern-engine.md
│   │   │   ├── 004-custom-backend-oauth.md
│   │   │   └── 005-trust-based-leaderboard-v1.md
│   │   └── agents/
│   │       ├── workflow.md
│   │       ├── issue-tracker.md
│   │       ├── triage-labels.md
│   │       ├── domain.md
│   │       └── solid-unity.md
│   ├── Scenes/
│   ├── Scripts/                ← game code goes here
│   └── Settings/
├── Packages/                   ← Unity package manifest + lock
└── ProjectSettings/
```

Unity generates a `.meta` file beside every file under `Assets/`, documents
included. That is expected — commit the `.meta` files together with the docs, and
never commit a `.md` without its `.meta` or Unity will regenerate it with a fresh
GUID on the next machine.

ADR files are numbered with **three digits** (`001-`), while prose refers to them
as `ADR-0001`. Both forms are in use; match the filenames on disk when linking.

`Assets/Scripts/` exists since M1/M2 (Hero, Enemy, Bullet, Wave, Run code plus
`Assets/Prefabs/`, `Assets/Art/`, `Assets/Settings/Enemies/`). Core `CONTEXT.md`
terms (Hero, Enemy, Bullet, Wave, Score, Health, Object Pool) are implemented;
Boss, Attack Pattern, Phase, and everything post-MVP are still intent only.

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/grill-with-docs`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0003 (data-driven attack pattern engine) — but worth reopening because…_