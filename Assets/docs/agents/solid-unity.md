# SOLID & Unity best practices for this codebase

How we apply SOLID in Unity — and, just as importantly, when we deliberately don't.
Based on [SOLID: When to use it and why](https://giannisakritidis.com/blog/SOLID-When-To-Use-It-And-Why/)
(Giannis Akritidis) and Unity's official
[best practice guides](https://docs.unity3d.com/6000.5/Documentation/Manual/best-practice-guides.html).
This document operationalizes the "build concrete first, abstract from working code"
philosophy in `AGENTS.md` and ADR-0003.

## The stance: SOLID is an investment, not a rulebook

Akritidis' core point: architecture should be about *investing* time, not losing it.
Apply a principle where you can name the concrete future change it protects against;
skip it where you can't. Signals to abstract: a class changes on every feature, a
dependency is an implementation detail (rendering, input device, backend), history
shows volatility. Signals to stay concrete: the code hasn't changed in several
iterations, or you'd be abstracting on a guess. That is exactly why M3 hardcodes the
Boss and M4 abstracts it — the patterns must exist and feel right before the engine
generalizes them.

## The five principles, mapped to this project

- **Single Responsibility** — one reason to change per component. `Health` knows hit
  points; `HeroController` knows input + movement; `BulletSpawner` knows spawning;
  `RunManager` knows Run state. Keep it that way: a new concern (e.g. Powerup pickup)
  is a new small component on the same GameObject, not a new region in an existing one.
- **Open/Closed** — extend via data and composition, not modification. The entire
  attack system is designed for this: a new attack/Enemy/Boss is a new ScriptableObject
  asset (+ at most one new formation function), never an edit to engine code (PRD §5,
  ADR-0003). If adding content forces editing a switch statement, that switch is in
  the wrong place.
- **Liskov Substitution** — anything holding a `Health` and emitting attacks is a
  Combat Actor; pattern code must work unchanged whether `source` is the Hero, an
  Enemy, or a Boss. A Boss is an Enemy with Phases, **not** a subclass that breaks the
  contract with special cases. Prefer composition over inheritance; deep MonoBehaviour
  hierarchies are how LSP dies in Unity.
- **Interface Segregation** — keep interfaces narrow when they appear (e.g. a future
  `IDamageable` exposes `TakeDamage`, not the whole `Health`). Don't pre-create
  interfaces for single-implementation classes — that's ceremony, not architecture.
- **Dependency Inversion** — game logic stays independent of volatile implementation
  details. Concretely here: pattern *math* (Formation offsets, Direction resolution)
  is plain C# testable in EditMode without scene setup; actors reach the engine
  through the `BulletSpawner` seam; the future Leaderboard client is one class behind
  which `UnityWebRequest` details hide. Don't inject abstractions between stable
  neighbours (e.g. `Enemy` ↔ its own `EnemyStats`) just to have interfaces.

## Unity-specific practices we hold ourselves to

From Unity's best practice guides + hard-won project rules (see also `AGENTS.md`):

- **ScriptableObjects are shared data, never runtime state.** Assets describe
  (`EnemyStats`, future Pattern/BossDefinition assets); per-instance state lives on
  the component. Scaling is passed as a multiplier — never write to a shared asset.
- **Composition over inheritance** for MonoBehaviours; small components, one
  responsibility each.
- **Pooling:** `UnityEngine.Pool.ObjectPool<T>` before hand-rolling; ALL per-life
  state resets on spawn (not deactivate); never run pattern timing in a coroutine on
  a poolable object — use a persistent scheduler.
- **One owner per transform:** a Bullet is transform-driven with manual overlap checks
  (no `Rigidbody2D`), Hero/Enemy are kinematic bodies — never mix both drivers on one
  object (ADR-0003 invariant).
- **Assembly definitions** separate runtime / editor / tests; editor code under
  `Editor/`. This makes module boundaries real — the graded artifact.
- **No magic numbers in MonoBehaviours** — tuning lives in ScriptableObjects,
  inspectable and versioned.
- **WebGL is the platform:** no `System.Threading`, no file I/O; async goes through
  coroutines / `async` over `UnityWebRequest`; download size is a feature.
- **Performance is measured, not assumed:** the bullet-physics ownership question is
  decided by profiling a worst-case bullet count in a WebGL build (PRD §8.3), not on
  paper. Avoid per-frame allocations in the bullet path (cache, pool, no LINQ in
  `Update`).
- **Determinism:** gameplay RNG is an owned, seeded `System.Random` — never the global
  `UnityEngine.Random` (ADR-0005).

## Review heuristic

When reviewing a PR (or your own diff), ask in order:

1. Does each new class have one reason to change?
2. Could the next content addition (new Enemy, new Pattern) land without editing this
   code? If not, is that acceptable *because we're pre-M4 concrete-first*?
3. Is any ScriptableObject mutated at runtime? (Instant fail.)
4. Is there an abstraction with a single implementation and no named future second
   one? (Remove it.)
5. Does anything in the bullet path allocate per frame or per shot? (Pool it.)
