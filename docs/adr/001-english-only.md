# English-only codebase and presentation, no translation boundary

The entire project is written in 100% English — code (identifiers, comments, logs,
docs) *and* all user-facing text (menus, HUD, leaderboard, copy). Unlike a
Swiss-German agency context where German is a presentation concern with a
translation boundary, here there is no German anywhere in the artifact. German is
only the language the developer happens to use when chatting with AI assistants;
it never enters the repo or the shipped game.

Rationale: this is a solo, greenfield project with no client constraint forcing
German, an international/portfolio-facing game audience, and English UI as the
natural default. Keeping a single language across code and product avoids the cost
of a translation boundary entirely and yields a clean, conventional codebase that
humans and coding agents work in without translation drift.

## Consequences

- `CONTEXT.md` is a plain English glossary — it does **not** carry a `de:`
  translation map (contrast with agency projects that do).
- All UI strings, menu labels, and leaderboard text are authored in English.
- Conversations with AI assistants may happen in German, but nothing German is
  ever committed. A German speaker reading the repo will find no German; this is
  deliberate, not an oversight.