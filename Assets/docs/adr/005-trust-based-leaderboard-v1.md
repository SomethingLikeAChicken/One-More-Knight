# Trust-based leaderboard for v1, with a hardenable endpoint

Scores are computed client-side and submitted to the API. For v1 the server
accepts submitted scores on trust. Server-authoritative validation — re-simulating
the run on the server to verify the score — is explicitly **out of scope** for the
180-hour budget.

Rationale: a single-player web game computes its score in the browser, so a
determined user can POST a fabricated score. True validation would require running
the game's simulation on the server, which is a project in itself. The pragmatic
choice is to ship a trust-based leaderboard now while designing the submit endpoint
so it *could* be hardened later.

Shipping as WebGL/wasm (ADR-0002) rather than readable JavaScript raises the effort
of tampering slightly, but wasm is **not** a security boundary and must not be
treated as one. The endpoint is spoofable either way; nothing below changes.

## Consequences

- The leaderboard is spoofable in v1. This is a deliberate, documented limitation,
  not an oversight.
- The pattern/run RNG should be **seeded**, so a future validation path (replay
  from seed) remains possible. Use an owned `System.Random` instance (or a saved
  `Random.State`) for run-affecting randomness — `UnityEngine.Random` is global
  static state that any script, and Unity itself, can advance, so a run seeded
  through it is not reproducible. Keep cosmetic randomness off the seeded stream.
- The submit endpoint is designed to be hardenable: accept a seed + run summary,
  sanity-bound the score, and rate-limit submissions.
- This tradeoff and its remediation path are written up explicitly in the project
  documentation.