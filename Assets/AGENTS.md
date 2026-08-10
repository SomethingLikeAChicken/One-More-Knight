# AGENTS.md

## Project Overview

A browser-based **medieval-fantasy bullet-hell shoot 'em up** with roguelite elements,
built as a university project (~180h budget).

- **Base structure:** classic arcade shooter, Galaga-inspired. Player at the bottom,
  enemies/bosses descend from the top.
- **Difficulty identity:** boss fights and bullet patterns are inspired by *Realm of the
  Mad God* — readable, pattern-based, genuinely hard. NOT "harder = more bullets".
- **Theme:** medieval fantasy / occult siege aesthetic (runeknight, witch hunter, demons,
  necromancers, dragons). Projectiles are spells/runes/bolts, not lasers. NOT a space game.
- **Not an MMO.** Single-player web game with an online leaderboard. Permadeath is
  per-run only (lose the run, not a persistent character).

**Status (2026-08-10):** M1 + M2 of the milestone plan are done and play-tested —
Hero movement/shooting, Waves, pooled Bullets, Health, Score, Game Over. M0 (WebGL
build + deploy) is **not** done and overdue. No Boss, no Enemy fire, no backend yet.
The living plan is the PRD: **GitHub issue #1** in this repo.

## Project Documentation & Decision Record

This repo is docs-driven. Read these before working, and honour them in all output:

- **`Assets/CONTEXT.md`** — the domain glossary / ubiquitous language. Use its
  terms in every identifier, issue title, test name, and PRD; never drift to the
  synonyms it marks _Avoid_. Hero = in-run avatar, Player = the OAuth account.
- **`Assets/docs/adr/`** — architectural decisions and their rationale: 0001
  English-only, 0002 Unity (WebGL) over Phaser 4 / web, 0003 data-driven attack
  pattern engine, 0004 custom backend + OAuth, 0005 trust-based leaderboard. If your
  work would contradict an ADR, surface it explicitly rather than silently overriding.
- **`Assets/docs/agents/`** — working standards: `workflow.md` (**the spec issue →
  sub-issue → PR workflow — read before starting any feature**), `issue-tracker.md`
  (GitHub + `gh` CLI), `triage-labels.md` (triage label vocabulary), `domain.md`
  (how to consume the domain docs), `solid-unity.md` (SOLID applied pragmatically +
  Unity best practices).

Documentation lives **under `Assets/`**, alongside the code it describes — that is
the tree actually being edited. The cost is that Unity imports each document as an
asset and generates a `.meta` file next to it; commit those `.meta` files with the
docs. They are unreferenced assets, so they add nothing to a build.

If a doc is absent, proceed silently — don't flag it or pre-create it.

**Workflow:** the PRD lives as **GitHub issue #1** and is broken into small,
independently reviewable and testable sub-issues; each sub-issue is created before
work on its feature starts and is completed by exactly one linked PR. The full
conventions are in `docs/agents/workflow.md`. Issues name concepts using
`CONTEXT.md` vocabulary and reference the relevant ADR where a decision applies.

## Tech Stack

- **Unity 6 (6000.5.5f1) + C#**, with the **Universal Render Pipeline, 2D Renderer**
  (ADR-0002). The engine choice is developer familiarity on a fixed budget — see the
  ADR, and note it reverses an earlier decision to use Phaser 4.
- **Build target: WebGL.** This is the delivery target, not an afterthought — examiners
  open a URL. Switch the project to WebGL in M0 and keep it building throughout; a
  WebGL build that is first attempted late is a late-project risk.
- **Input System package** (`com.unity.inputsystem`, new input system is already the
  active handler). Author bindings in `Assets/Settings/InputSystem_Actions.inputactions`
  rather than polling raw keys.
- **2D toolchain already installed:** 2D Animation, 2D IK, SpriteShape, Tilemap Extras,
  Aseprite importer, PSD importer. Prefer these over hand-rolled asset pipelines.
- **Physics:** Unity 2D (`Rigidbody2D` / `Collider2D`) for the Hero and Enemies. Bullets
  are the open question — a bullet-hell spawns far more projectiles than Unity 2D
  physics wants to own. Decide per ADR-0003's invariants and measure before committing.
- **Custom backend** for auth + leaderboard (post-MVP) — see Backend section and ADR-0004.
- **Deploy:** static hosting for the WebGL build (Vercel or Netlify). Deploy a trivial
  build in M0 so "runs in a browser" is proven from day one, including the compression
  headers a Unity WebGL build needs.
- **Dependency policy:** keep Unity packages reasonably current via Package Manager, but
  do **not** chase Unity major/minor upgrades mid-project — an engine upgrade in a 180h
  budget is pure risk. Pin the editor version and move deliberately.

## Core Design Decisions (locked)

- **Movement:** full 2D movement for the player, constrained to a play area (NOT
  Galaga-style horizontal-only). RotMG-style patterns require dodging on both axes.
- **Hitboxes:** projectile and player hitboxes must be SMALLER than their sprites
  (e.g. visual 16x16, hitbox 6–8px). Critical for fair bullet-hell feel.
- **Readability:** strict projectile color coding —
  player shots gold/blue, enemy shots red/violet/green, powerups bright,
  danger zones outlined. Telegraph strong attacks (0.5–1.0s warning).
- **Fairness:** patterns are deterministic; variation comes from start angle / speed /
  count. No randomly unavoidable death.

## Architecture Philosophy

**Build concrete first, abstract from working code.** Do not pre-build the perfect engine.
Hardcode early patterns, get them feeling good, THEN generalize (see Milestone M4).

The end-state target is a **data-driven, actor-agnostic attack pattern system** shared by
both normal enemies AND bosses. A boss is just a more complex enemy with phases — not a
special case.

A pattern is defined by config, not bespoke code:
`Origin / Formation / Direction / Motion / Timing / Bullet`

- Formations: single, arc, circle, parallelLines, lanes, snake, customOffsets
- Directions: down, fixed, aimedAtTarget, radial
- Motions: linear, homing, sine
- Always use `source` / `target` in pattern code, never `boss` / `player`.
- Engine name: `AttackPatternEngine`, NOT `BossPatternEngine`.
- Target: ~80–90% of attacks from config; allow a small `custom` hook for the rest. Do not
  torture the schema to hit 100%.

Patterns are authored as **ScriptableObject assets** — tuned in the Inspector, versioned
in git, no bespoke config format or loader. A ScriptableObject is a shared asset: it
describes an attack and must never hold the runtime state of an actor firing it.

### Seams to plant early (cheap now, painless refactor in M4)

- Route EVERY bullet through one central spawn function, even when calls are hardcoded.
- Keep enemy stats (hp, speed, score) in ScriptableObjects from M1 — no magic numbers
  inside MonoBehaviours.
- One `EnemyBullet` type with swappable sprite + behavior fields, not many bullet classes.
- Put game code behind assembly definitions (`.asmdef`) from the start. It keeps compile
  times sane and makes the module boundaries real rather than aspirational — which is
  exactly what the architecture is being assessed on.

## Milestone Plan

- **M0 — Setup & deploy (8–12h):** switch build target to WebGL, scene skeleton
  (Boot/Menu/Game/GameOver as Unity scenes), placeholder sprite, running game loop,
  WebGL build deployed online with working compression headers.
- **M1 — Movement + shooting + dumb enemies (15–20h):** Hero moves (2D, constrained) via
  the Input System and shoots up; enemies spawn top, move straight down; Hero bullets
  destroy them. No health or score yet. Focus on game feel. Use a minimal object pool for
  Hero bullets.
- **M2 — Health + score + lose condition = MVP (15–20h):** enemy HP, Hero Health,
  contact-damage lose condition, score per kill, HUD, Game Over + restart.
- **M3 — Boss, HARDCODED (20–25h):** score threshold spawns a boss with HP bar, hover
  movement, 2–3 hardcoded patterns (aimed, fan, simple radial/circle), one phase change at
  50% HP, reward on death. First basic enemy shot (single straight-down) lands here too.
  **Do not build the engine yet** — hand-tune real patterns first.
- **M4 — Abstraction / modular pattern engine (20–30h):** refactor M3's hardcoded patterns
  + enemy shots into the ScriptableObject-driven engine above. Success test: add a 2nd boss
  or new enemy mostly via new assets + at most one new formation function.

## Post-MVP / Stretch (only after the core is solid)

Leaderboard + OAuth login (custom backend, ~20–30h), bosses 2 & 3 (mostly new pattern
assets), powerups/relics, roguelite meta-progression (per-run level-up boni first; skill
tree / "relic altar" last).

## Backend (custom)

Simple custom backend: players + their highscores, designed to extend later. It is a
**standalone HTTP API** the Unity client calls via `UnityWebRequest` — the game is a
static WebGL artifact, not something the API renders (ADR-0004). Framework choice is
deliberately deferred to the backend milestone.

Schema (Postgres):
- `players (id uuid pk, username text unique, auth_id text, created_at)`
- `scores  (id uuid pk, player_id uuid fk, score int, meta jsonb default '{}', created_at)`
- `meta` jsonb is the extensibility hatch — store boss reached, build/relics, run
  duration, RNG seed, etc. without migrations.
- Leaderboard read = top N `scores` joined to `players`.

Auth: OAuth (Google + GitHub). `players.auth_id` holds the OAuth subject. No password
handling anywhere. The redirect happens in the **page hosting the WebGL canvas**, not
inside Unity — plan a small `.jslib` bridge to hand the session back to C#. Prefer serving
the build and the API from the same origin so the session cookie just works; a separate
origin means CORS-with-credentials or bearer tokens.

**Score validation — known limitation (document this):** the score is computed
client-side, so the submit endpoint is spoofable. Compiling to wasm raises the effort
slightly but is not a security boundary. Server-authoritative validation (re-sim the run)
is out of scope for 180h. Design the endpoint so it *could* be hardened later (accept seed
+ run summary, sanity-bound the score, rate-limit). Ship trust-based for v1 and write it
up as an explicit tradeoff with the path to fix.

## Scope Reality

180h is tight. M0–M4 + a simple leaderboard + polish = a complete, gradeable project.
Everything else (bosses 2–3, skill tree, multiple classes) is bonus, to be framed in the
docs as a deliberate scope decision. Reserve real time for balancing, juice, assets/sound,
and documentation — these are routinely underestimated.

## Coding Conventions & Gotchas

- **Quality gate:** there is **no CI pipeline yet** — setting one up is an open task. Until
  then the standing bar is local: the project compiles with **zero errors and zero
  warnings**, and the Unity console is clean on entering and exiting play mode. Do not
  open a PR that leaves console noise behind.
- Standard C# / .NET naming. Rider is the configured editor (`com.unity.ide.rider`) — use
  its formatter rather than inventing a house style.
- **Assembly definitions:** put runtime code, editor code, and tests in separate `.asmdef`s.
  Editor-only code must live under an `Editor/` folder or it breaks the build.
- **Object pooling:** reset ALL motion/homing/wave state on every spawn, or recycled bullets
  inherit their previous life's behavior. `OnEnable` is the reset point, not the
  constructor. `UnityEngine.Pool.ObjectPool<T>` exists — use it before writing your own.
- **Sine/wave motion vs physics:** do NOT write `transform.position` on a body whose
  `Rigidbody2D` velocity is also driving it — the two fight and collision breaks. Pick one
  owner per bullet: physics-driven (`MovePosition`, in `FixedUpdate`) or fully
  transform-driven with manual overlap checks. Be consistent per bullet type.
- **Coroutines and pooling interact badly.** A coroutine stops when its GameObject is
  deactivated, and pooling deactivates objects — so a pooled boss recycled mid-burst
  silently truncates the burst instead of erroring. Run multi-step pattern timing on a
  persistent scheduler, or cancel explicitly on death/phase change.
- **WebGL constraints:** no threads (`System.Threading` is unavailable), no direct file
  I/O, `PlayerPrefs` is backed by IndexedDB, and initial download size is a real UX cost.
  Everything asynchronous goes through coroutines or `async` over `UnityWebRequest`.
- **Leaderboard integrity:** seed the pattern RNG with an owned `System.Random` instance,
  not `UnityEngine.Random` (global static, advanced by anything). One-line decision now,
  avoids pain later — see ADR-0005.

## Working Notes

- Chat-side planning/design is done in German; code, comments, identifiers, and this file
  are in English (ADR-0001). Nothing German is ever committed.
