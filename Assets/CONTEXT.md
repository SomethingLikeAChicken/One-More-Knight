# Medieval Bullet-Hell (working title)

A browser-based medieval-fantasy bullet-hell shoot 'em up with roguelite elements.
The structure is Galaga-inspired (Hero at the bottom, Enemies descending); the
boss fights and bullet patterns take their difficulty from Realm of the Mad God.
Built in Unity 6 (C#, URP 2D) and shipped as a WebGL build (ADR-0002), with a
custom backend for the leaderboard (ADR-0004).

**Naming rule:** the codebase is English-only with no translation boundary
(ADR-0001). Vocabulary is **hybrid**: mechanical, conventional names form the core
(`Enemy`, `Bullet`, `Score`, `Wave`), and a themed word is canonical only where it
reads cleanly and names a genuinely distinct concept (`Boon`, `Relic`, `Relic
Altar`). When a mechanical and a themed name both fit, the mechanical one wins;
purely decorative theming lives at the presentation edge (sprite keys, display
labels), not in domain identifiers. Each term lists synonyms to _Avoid_ so the
glossary doubles as a drift guard. Attack code is actor-agnostic (ADR-0003): it
says `source`/`target`, never `hero`/`enemy`/`boss`.

## Core entities

**Run**:
A single playthrough, from start to the Hero's death (or victory). Permadeath:
death ends the Run; only meta-progression (Relics) persists across Runs.
_Avoid_: game, session, attempt, level.

**Hero**:
The single player-controlled character within a Run — moves in 2D inside the play
area, fires upward, has Health. One fixed Hero in the MVP; the vocabulary leaves
room for multiple Classes later. The Hero's in-world identity (e.g. a runeknight or
witch-hunter) is presentation flavor and may change without affecting the term.
_Avoid_: ship, avatar, player (Player is the account, see below).

**Enemy**:
A hostile Combat Actor that spawns in Waves and is destroyed by the Hero's Bullets.
_Avoid_: mob, monster (use Enemy in code; themed creature names are presentation).

**Boss**:
A strong Enemy with Phases and several Attack Patterns, spawned at a Score
threshold. Modelled as an Enemy with phases — **not** a separate code path.
_Avoid_: miniboss (until a distinct concept actually exists).

**Combat Actor**:
The shared abstraction over Hero, Enemy, and Boss — anything that holds Health and
emits Attacks. Pattern code addresses it as `source` / `target`.
_Avoid_: entity (too generic), unit, character.

## Combat & attack system

**Bullet**:
A single projectile. The Hero's Bullets damage Enemies; Enemy Bullets damage the
Hero. Pooled and reused, never created/destroyed per shot.
_Avoid_: projectile, shot (Bullet is canonical).

**Attack Pattern** (often just **Pattern**):
The data-driven definition of one emission, composed of Origin, Formation,
Direction, Motion, Timing, and Bullet (ADR-0003). The same Pattern works for an
Enemy or a Boss.
_Avoid_: bullet pattern, shot pattern (these read as actor- or weapon-specific).

**Origin**:
Where a Pattern's Bullets spawn — the source actor, a screen edge, etc.

**Formation**:
The spatial arrangement of Bullets in one emission: single, arc, circle, parallel
lines, lanes, snake, custom offsets.

**Direction**:
Where Bullets head: down, fixed angle, aimed at target, radial.

**Motion**:
How a Bullet moves after spawn: linear, homing, sine.

**Timing**:
When and how often a Pattern fires: cooldown, initial delay, bursts.

**Phase**:
A Boss state, gated by an HP band, with its own Patterns and movement.
_Avoid_: stage, state, mode.

**Telegraph**:
The brief visual/audio warning before a strong Attack lands.
_Avoid_: tell, warning, wind-up.

**Hitbox**:
The collision shape, deliberately smaller than the sprite (design rule).
_Avoid_: collider, bounds.

**Object Pool** (often **Pool**):
The reuse store for high-churn objects, chiefly Bullets — spawn/release instead of
create/destroy. **All** per-instance state resets on spawn.
_Avoid_: cache.

## Progression & scoring

**Score**:
The points earned in a Run; the figure ranked on the Leaderboard. May be surfaced
in the UI under a themed label (e.g. "Fame"), but the domain term is Score.
_Avoid_: points, fame (in code).

**Health**:
A Combat Actor's hit points. The Hero's Health reaching zero ends the Run.
_Avoid_: lives (unless a distinct lives concept is added); HP is fine in code,
not in prose.

**Wave**:
A defined group or sequence of Enemies that spawns together or in order.
_Avoid_: round, level.

**Powerup**:
A temporary in-Run buff that drops from Enemies and is collected by the Hero (e.g.
faster fire, a shield). Lasts within the Run. MVP concept.
_Avoid_: pickup, item, buff (in code).

**Pact**:
A player-chosen difficulty bargain (#117, #129): exactly one may be active at a
time, offered at Run start and after each Boss kill as three tier candidates
(easy/medium/hard). It makes the Run harder via explicit effects and multiplies
the LeaderboardScore earned while it holds — the multiplier is bound to the tier,
never the individual Pact. Distinct from a Powerup (an Enemy drop), a Boon (a
per-Run upgrade), a Relic (permanent), and a Wave modifier (involuntary,
per-Wave).
_Avoid_: curse (taken by death-Curses), cursed relic, contract, wager, handicap.

**Boon** _(stretch)_:
A per-Run upgrade chosen on level-up that lasts the rest of that Run — the
roguelite build layer. Distinct from a Powerup (an Enemy drop) and a Relic
(permanent).
_Avoid_: perk, upgrade (ambiguous), card.

**Relic** _(stretch)_:
A permanent meta-progression unlock that persists across Runs, acquired or invested
between Runs.
_Avoid_: meta upgrade (in titles), unlock, talent.

**Relic Altar** _(stretch)_:
The between-Run surface where Relics are unlocked and invested — the
meta-progression / "skill tree" screen, themed.
_Avoid_: skill tree (in code and titles — use Relic Altar), shop, store.

## Backend & identity

**Player**:
The persistent human identity behind Runs — an account authenticated via OAuth
(Google/GitHub, ADR-0004), the owner of submitted Scores. Backend concept; maps to
the `players` table. Do **not** use for the in-Run avatar — that is the Hero.
_Avoid_: user, account (Player is canonical for the identity).

**Leaderboard**:
The ranked list of Scores across Players, read from the backend.
_Avoid_: highscore table, ranking, scoreboard.

**Run Summary** _(stretch)_:
The per-Run data submitted alongside a Score (seed, Boss reached, build, duration),
stored in `scores.meta`. The basis for any future score validation (ADR-0005).
_Avoid_: run log, telemetry, replay.

## Scope

- **MVP (M0–M4):** Hero, Enemy, Boss, Bullet, Attack Pattern (Origin / Formation /
  Direction / Motion / Timing), Phase, Wave, Score, Health, Powerup, Hitbox, Object
  Pool, Telegraph.
- **Post-MVP:** Player, Leaderboard, Run Summary (backend, OAuth); Boon, Relic,
  Relic Altar (progression).
- **Not modelled:** multiplayer, trading, multiple Classes (one fixed Hero in the
  MVP), and game modes other than the core Run.