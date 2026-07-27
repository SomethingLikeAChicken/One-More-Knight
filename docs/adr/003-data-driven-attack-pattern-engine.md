# Data-driven, actor-agnostic attack pattern engine

Enemy and boss attacks are defined by data (configuration), not by bespoke
per-attack or per-boss code. A single attack pattern engine is shared by every
combat actor. A pattern is composed from six parameter groups: **Origin**,
**Formation**, **Direction**, **Motion**, **Timing**, and **Bullet**. A boss is
modelled as an enemy with phases — not a separate code path.

Rationale: the goal is to scale content (new enemies, new bosses, new attacks)
through configuration rather than new code, which is both the project's
architectural centerpiece and its primary assessment artifact. Treating bosses as
"more complex enemies" keeps one combat model instead of two.

Patterns are authored as **ScriptableObject assets** (ADR-0002), so a pattern is a
versioned project asset tuned in the Inspector — not a code literal and not a
hand-rolled config format needing its own loader.

The engine is built **concrete-first**: early patterns are hardcoded and hand-tuned
in M3, then the working patterns are abstracted into the data-driven engine in M4.
Do not build the engine before real, fun patterns exist. Target roughly 80–90% of
attacks expressible from config, with a small `custom` escape hatch for genuinely
unique attacks rather than torturing the schema to reach 100%.

Pattern code is **actor-agnostic**: it references `source` / `target`, never
`boss` / `player`, so the same pattern works for a wave enemy or a boss.

## Consequences

- A new enemy or boss is mostly a new ScriptableObject asset, plus occasionally one
  new formation or motion module in code.
- The engine is the architectural heart of the project and the main thing the
  documentation should explain.
- **Over-abstraction is the main risk**: the engine must not be built before M3's
  patterns exist and feel good.
- ScriptableObjects are shared assets, not per-instance state. A pattern asset
  describes an attack; it must never store the live state of an actor currently
  firing it. Runtime state belongs in the emitter component.
- Implementation invariants that must hold:
    - Pooled bullets reset **all** motion/homing/wave state on every spawn.
      Deactivation is not a reset.
    - Sine/wave motion must not fight the physics step: don't write
      `transform.position` on a body that `Rigidbody2D` velocity is also driving.
      Pick one owner per bullet — physics-driven (`MovePosition`, in
      `FixedUpdate`) or fully transform-driven with manual overlap checks — and
      keep it consistent.
    - Scheduled bursts/rows must survive their scheduling mechanism. Coroutines stop
      when their GameObject is deactivated, and pooling deactivates objects — so a
      pooled source recycled mid-burst silently truncates the burst. Run multi-step
      timing on a persistent scheduler, or guard and explicitly cancel on
      death/phase change.