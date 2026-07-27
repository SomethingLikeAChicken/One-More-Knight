# Unity (WebGL) over Phaser 4 (web)

The game is built in Unity 6 with C#, using the URP 2D renderer, and shipped as a
**WebGL build** — rather than as a Phaser 4 + TypeScript + Vite browser game.

**This reverses an earlier decision.** The original ADR-0002 chose Phaser 4 over
Unity, framing the alternative as a *native PC* Unity build and rejecting it
largely on delivery grounds. That framing was the flaw: the real choice is not
"web vs. native", it is "which engine produces the web build". Unity WebGL keeps
every delivery benefit the original decision was protecting.

Rationale: the developer already knows Unity, and on a fixed ~180-hour budget
engine familiarity is the single largest lever on how much game actually gets
built. The original ADR traded that away for a stack the developer would have had
to learn alongside the game itself. Shipping to WebGL preserves what the Phaser
decision was really buying — examiners open a URL and play at every checkpoint and
at hand-in, with no download and no install. Unity's editor tooling (Inspector,
prefabs, the 2D toolchain, play-mode iteration) is a direct advantage for
hand-tuning bullet patterns, which ADR-0003 makes the core of the project. And
ScriptableObjects are a first-class fit for ADR-0003's data-driven pattern engine:
pattern definitions become authored, versioned assets rather than a hand-rolled
config format plus a loader.

Tradeoff accepted: WebGL builds are large and slow to first load compared to a
Vite bundle, WebGL is single-threaded and IL2CPP-only, and the browser/DOM and npm
ecosystems are no longer directly available to the client. C# replaces TypeScript,
so the game no longer shares a language with the backend.

## Consequences

- **Delivery target is WebGL.** A native desktop build may exist for performance
  headroom, but WebGL is the delivery target and the one the backend design
  assumes. Set the Unity build target to WebGL early, not at the end.
- Modular systems are authored as **ScriptableObject assets** plus C# code
  (ADR-0003), rather than hand-built entirely in code.
- Deploy the WebGL build as a static site (Vercel/Netlify both work). Compressed
  builds need correct `Content-Encoding` headers — verify this in M0 rather than
  discovering it at hand-in.
- The backend becomes a **separate service** the client calls over HTTP; it no
  longer shares a bundler or a language with the game (ADR-0004).
- Budget real time in M0 for the first WebGL build. It is slower than a web
  bundler build and has its own failure modes; proving the deploy pipeline early
  is the whole point of M0.
- If web delivery ever stopped mattering, a native build would be the better
  target — but that would be a new decision, not this one.
