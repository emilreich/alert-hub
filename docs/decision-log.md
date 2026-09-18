# Decision Log

Running log of decisions made during the build, why, and — critically — the
points where AI-generated output was rejected, corrected, or second-guessed.
Entries are appended in chronological order as the work happens. Each entry
that involves accepting or rejecting AI-produced work says explicitly what
was checked and why it passed or failed.

---

## D1 — Separate repo, not a folder in the Zenty codebase
**Date:** 2026-09-18

The working environment's existing repo (`zenty`) is an unrelated Hungarian
SaaS product with its own CLAUDE.md conventions (Next.js, Supabase, specific
voice/design rules). This exercise is a standalone brief with no relation to
that domain. Building it inside `zenty` would mix unrelated commit history
and force irrelevant conventions onto an unrelated project.

**Decision:** new, standalone local repo at `C:\dev\alert-hub`, to be pushed
to a new GitHub repo by the candidate once created locally.

## D2 — Per-user filtered subscriptions over global alert types
**Date:** 2026-09-18

See `plan.md` §4 for the full reasoning. Chosen deliberately as the more
expensive option because it better matches the brief's "set up alerts"
phrasing and gives the admin view and matching engine real logic to exercise,
at the cost of more build time. Flagged here as a scope decision that could
be revisited if time runs out (fallback documented in `plan.md`'s "what could
get cut" section).

## D3 — Simulated event source instead of real event detection
**Date:** 2026-09-18

Real breaking-news/market/disaster detection is out of reach for a 24h
exercise and isn't what this brief is actually testing (see `plan.md` §1).
Decision: build a seedable, manually-triggerable fake event generator behind
the same kind of interface a real feed adapter would implement, so swapping
in a real source later is a contained change. Explicitly documented as a
scope cut rather than left unstated, so it doesn't read as an oversight.

## D4 — Admin view = operator view, not end-user self-service
**Date:** 2026-09-18

See `plan.md` §3. "Admin view" in the brief is taken literally: an
operator-facing surface (all users' rules, delivery log, manual trigger),
not a polished per-user settings page. A separate end-user portal is a
documented cut, since it would mostly duplicate the admin CRUD screens.

## D5 — Stack: ASP.NET Core + Angular + SQLite/EF Core
**Date:** 2026-09-18

Chosen because it's the candidate's strongest stack — the exercise's stated
intent is to spend the 24h budget on thinking/judgment, not on fighting an
unfamiliar framework. SQLite specifically (over Postgres/SQL Server) so a
reviewer can clone and run the repo with no external DB dependency, while
still getting real EF Core migrations as an artifact.

## D6 — Four-project solution layout (Core / Infrastructure / Api / Tests)
**Date:** 2026-09-18

Considered a single-project layout (faster, less ceremony) against a fuller
clean-architecture split. Settled on four projects specifically to make the
channel abstraction's boundary a physical one: `INotificationChannel` lives
in `Core` with zero dependency on `Infrastructure`, and Email/Slack
implementations live in `Infrastructure` alongside EF Core and the event
simulator. This makes "flexible enough to add channels later" verifiable by
looking at project references, not just by reading a comment. Considered
this the right amount of structure for a 24h exercise — not collapsing
everything into one project, but not going further into a 6+ project clean
architecture either, since that would add ceremony without adding anything
this brief asks to see demonstrated.

---

<!-- Append further entries below as the build proceeds. Each entry for a
     rejected/corrected AI output should state: what was generated, what was
     wrong with it or why it was rejected, and what was done instead. -->
