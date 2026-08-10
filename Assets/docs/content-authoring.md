# Content authoring — enemies, patterns, groups, waves, bosses

Everything gameplay is a **ScriptableObject asset**. You never touch code, prefabs,
or scenes to add content — you create assets via **right-click in the Project window
→ Create → One More Knight → …** and reference them from other assets. This doc walks
each asset type from smallest to largest.

Folder convention:

```
Assets/Settings/
├── Patterns/   ← Attack Pattern assets (what a shot looks like)
├── Enemies/    ← EnemyStats + BossStats (who fires it)
├── Groups/     ← GroupDefinition (choreographed squads with a difficulty cost)
└── Waves/      ← MainProgression (the difficulty curve + group pool)
```

---

## 1. Attack Pattern (`Create → One More Knight → Attack Pattern`)

One Pattern = one attack, composed from six axes (ADR-0003). The same asset works
for any Enemy or Boss — pattern code never knows who fires it.

| Section | Field | What it does |
|---|---|---|
| Origin | `muzzleOffset` | How far from the actor's centre Bullets spawn, along their own direction |
| Formation | `formation` | `Single` · `Arc` (`bulletCount` over `spreadAngle`°) · `Circle` (`bulletCount` around 360°) · `Wall` (`bulletCount` side by side, `wallSpacing` units apart, flying parallel) |
| Direction | `direction` | `Down` · `Fixed` (`fixedAngle`°, 0 = down) · `AimedAtTarget` (falls back to Down without a target) · `Radial` (spread anchor for Circle) |
| Motion | `motion` | `Linear` · `Sine` (`sineAmplitude` units, `sineFrequency` rad/s — snaking) · `Homing` (`homingTurnSpeed`°/s toward the target for `homingDuration` s, then straight — the cap is the fairness rule) |
| Timing | `cooldown`, `initialDelay` | Seconds between bursts / before the first one |
| | `burstCount`, `burstSpacing` | Emissions per burst and their spacing |
| | `angleStepPerEmission` | Rotates the aim by N° per emission → **spirals & rotating fans** |
| Bullet | `bulletSpeed`, `bulletDamage`, `acceleration` | Acceleration may be negative (decelerating) |
| | `bulletTint` | **Readability rule:** enemy shots red/violet/green — never gold/blue (those are the Hero's) |

**Recipes** (all exist as assets you can copy):

- *Aimed shot*: Single + AimedAtTarget (`BossAimedShot`)
- *Fan*: Arc 5–9 @ 60–110° + Down (`BossFan`, `TyrantFanWide`)
- *Ring*: Circle 12–20 + Radial (`BossRadial`, `MonarchRingDense`)
- *Offset/rotated ring*: Circle + **Fixed** — the fixed angle rotates the whole ring; two of these interleave (`TyrantRingOffset`)
- *Spiral*: burstCount 8–14, burstSpacing ~0.1, `angleStepPerEmission` 20–30 (`SpiralRotor`, `SpinnerSpiral`)
- *Sine curtain*: Arc + Sine motion (`SineCurtain`)
- *Homing bolt*: Single + AimedAtTarget + Homing motion (`HomingBolt`)
- *Sweeping wall*: Wall 7 @ ~1.1 spacing + Down (`SweepingWall`)
- *Lunging shot*: low `bulletSpeed` + positive `acceleration` (`TyrantFanWide`)

---

## 2. Enemy type (`Create → One More Knight → Enemy Stats`)

One pooled prefab plays every Enemy — identity is data:

- **Stats:** `maxHealth`, `moveSpeed`, `scoreValue`, `contactDamage`
- **Identity:** `sprite` + `tint` (drop a PNG into `Assets/Art/`, import type *Sprite*,
  **Pixels Per Unit 32** to match the rest; the sprite swap happens on spawn)
- **Movement:** `Descend` (straight down) or `Weave` (+ `weaveAmplitude`/`weaveFrequency`)
- **Attack:** an Attack Pattern asset — or **None = never shoots** (Darter, Bulwark)

That's the whole recipe. The nine existing types are the reference palette:
Descender (baseline), Weaver (weaving arc-shooter), Sentinel (tanky aimed),
Darter (fast body-threat), Warlock (ring caster), Bulwark (wall),
Spinner (spiral), Hexer (sine curtains), Hunter (weaving homing).

**Rule that keeps it fair:** never edit stats at runtime and never put per-instance
state in the asset — scaling reaches enemies as multipliers (ADR-0003).

---

## 3. Enemy Group (`Create → One More Knight → Enemy Group`)

A reusable choreographed squad — the unit the wave composer buys:

- **`difficulty`** — its price against a wave's budget. Calibrate against the pool:
  4 Descenders = 3 · Sentinel + screen = 7 · 2 Hexers + Weaver wedge = 8 ·
  Bulwarks + Sentinel + Weavers = 12. If a new group feels meaner than an 8, cost it 9+.
- **`minWave`** — earliest wave it may appear (content gating; keeps wave 1 gentle
  even if you cost something cheap).
- **`slots`** — the choreography, in order. Each slot: its own Enemy `type`, `count`,
  `formation` (`Line` spread across the top · `Vee` wedge · `Column` one lane),
  `anchor` (−1 left … 0 centre … 1 right; Line ignores it), `spacing` (Vee),
  `spawnInterval` (within the slot), `delayBeforeSlot` (after the previous slot —
  this is what makes "Bulwark first, Hunters behind it" read on screen).

Mixing types across slots is the point — a group is a *scene*, not a stat block.

---

## 4. The difficulty curve (`Assets/Settings/Waves/MainProgression.asset`)

One asset owns the whole run pacing:

- **`baseBudget` / `budgetPerWave` / `maxBudget`** (currently 8 / +3 / 30): wave N's
  budget. The composer buys random eligible groups (cost ≤ remaining budget,
  wave ≥ minWave) until nothing fits — fixed difficulty, variable composition.
- **`pool`** — drag your new GroupDefinition in here. **That's the only registration
  step for new content.**
- **`delayBetweenGroups`** — the layering guarantee (groups never stack at once).
- **Post-cap escalation** — once the budget is capped, waves get harder only through
  `hpMultiplierStep`/`speedMultiplierStep`, hard-capped at `maxHpMultiplier`/
  `maxSpeedMultiplier` (×4 / ×1.5). Density can never exceed what the pool's costs
  allow — that is the anti-bullet-wall rule; don't defeat it by making one group huge.

Randomness draws from a per-Run seeded `System.Random` (ADR-0005 seam).

---

## 5. Boss (`Create → One More Knight → Boss Stats`)

One Boss prefab plays every Boss:

- **Stats:** `maxHealth`, `contactDamage`, `scoreReward`
- **Identity:** `sprite` + `scale` (bosses are 96–120 px sprites at PPU 32)
- **Movement:** `entrySpeed` (descent to the hover line), `hoverSpeed`, `hoverAmplitude`
- **`phases`** — ordered list; each Phase: `name`, `entersAtHpFraction` (first = 1;
  a 0.5 Phase begins at ≤50 % HP), its **Pattern asset list**, `hoverSpeedMultiplier`,
  and visuals (`tint` held for the Phase, `entryPulseScale`/`entryPulseDuration` —
  make Phase changes unmissable). Phase switches cancel the previous Phase's bursts
  automatically.

**Register it:** open `Assets/Scenes/Game.unity` → select the **BossDirector**
object → add an element to **Encounters** with your BossStats asset and its Score
threshold (current ladder: 1000 / 3000 / 6000 / 10000). Rewards feed the next
threshold, so leave headroom between them.

---

## 6. Checklist for any new content

1. Create the asset(s), reference existing ones where possible.
2. Enter play mode from the Game scene and watch it (a Boss: temporarily lower its
   threshold on the BossDirector; revert before committing).
3. Console must stay clean — 0 errors, 0 warnings (AGENTS.md quality gate).
4. Colors follow the readability rule; hitboxes stay smaller than sprites.
5. Commit the `.asset` **with its `.meta`**, via the usual sub-issue → PR workflow
   for gameplay-affecting content (`docs/agents/workflow.md`).
