# Custom backend with OAuth, over a managed BaaS

The leaderboard and player accounts run on a custom backend (an HTTP API +
Postgres), not on a Backend-as-a-Service such as Supabase or Firebase.
Authentication is **OAuth only (Google + GitHub)**. No password authentication,
hashing, or session handling is hand-rolled.

Rationale: the developer's full-stack background makes a custom backend fast and
low-risk, and gives full control over schema and behaviour. OAuth offloads the one
genuinely dangerous part — password security — to providers, and because the game
ships to WebGL (ADR-0002) the browser OAuth redirect flow stays available; a
native build would have forced a loopback or device-code flow instead. The earlier
"use Supabase" guidance was scope-insurance for someone who'd lose time to backend
setup; it does not apply here.

**Topology re-decided after ADR-0002.** The original plan was a single Next.js app
serving the Phaser game client-side alongside its API routes — one project, one
bundler, one language. With a Unity WebGL client that no longer exists as an
option: the game is a static build artifact, not a component the API can render.
The backend is therefore a **standalone API** that the Unity client calls over
HTTP via `UnityWebRequest`.

The API's language and framework are deliberately **left open** until the backend
milestone is actually reached — it is post-MVP, and nothing before it depends on
the choice. A TypeScript API (Next.js route handlers, Hono, or similar) keeps
Auth.js / Better Auth available and is the default expectation; that is a
convenience, not a constraint.

## Consequences

- Schema (Postgres):
    - `players (id uuid pk, username text unique, auth_id text, created_at)`
    - `scores  (id uuid pk, player_id uuid fk, score int, meta jsonb default '{}', created_at)`
- `meta` jsonb is the extensibility hatch (boss reached, build/relics, run
  duration, RNG seed) — extend without migrations.
- No password material is stored anywhere; `auth_id` holds the OAuth subject.
- The backend is **post-MVP**. The game must be fully playable offline first, with
  score submission behind a single swappable C# interface so the API can be wired
  in later without rework.
- **Prefer serving the WebGL build and the API from the same origin.** Same-origin
  keeps the session cookie working with no extra machinery. A separate API origin
  means CORS with credentials, or dropping cookies for a bearer token the client
  stores — both are more work than choosing one origin up front.
- The OAuth redirect happens in the **browser page hosting the canvas**, not
  inside Unity. The Unity client cannot own the redirect; it reads the resulting
  session (or token) via the JS interop boundary. Plan one small `.jslib` bridge
  rather than trying to run the flow in C#.
- `UnityWebRequest` is coroutine/async-based and WebGL has no threads — treat all
  API calls as asynchronous from the start, including error and timeout paths.
