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

## D7 — Caught: unrelated employer NuGet feed breaking restore
**Date:** 2026-09-18

`dotnet new classlib` failed on the very first scaffold command with a 401
against `graphisoft-web.pkgs.visualstudio.com`. This is not a project issue —
it's a leftover authenticated private feed in this machine's global
`%APPDATA%\NuGet\NuGet.Config` from an unrelated employer/project. Checked
`dotnet nuget list source` from inside the new project directory to confirm
it was actually being consulted (it was: 4 sources merged in, including that
feed, "Enabled"), rather than assuming and guessing at a fix.

**Fix:** added a repo-local `NuGet.Config` with `<clear/>` + nuget.org only,
so restore for this repo is fully reproducible regardless of what's
configured on any given machine — this also protects anyone else who clones
the repo from hitting the same unrelated-feed problem.

## D8 — Caught: `dotnet add package` silently grabbed an incompatible EF Core version
**Date:** 2026-09-18

`dotnet add package Microsoft.EntityFrameworkCore.Sqlite` with no version
pin resolved to 10.0.12 — the latest on nuget.org — which targets net10.0
only. The projects are net8.0 (the installed SDK is 8.0.101; net10 isn't
available here). Restore failed immediately with an explicit NU1202
incompatibility error, so this was caught by the build, not silently
shipped — but worth logging because it's exactly the kind of "looks like a
reasonable command" output that would be wrong to run blind in CI or a
fresh clone.

**Fix:** pinned `Microsoft.EntityFrameworkCore.Sqlite` and
`Microsoft.EntityFrameworkCore.Design` to `8.0.11` explicitly (the current
patch on the 8.x line, matching net8.0). Verified with a full
`dotnet build` afterward that the whole solution compiles clean, not just
the one project touched.

## D9 — Rejected: template scaffolding cruft left in the initial commit
**Date:** 2026-09-18

`dotnet new webapi` and `dotnet new classlib` leave behind boilerplate that
has no place in the repo's history: a `WeatherForecastController`/
`WeatherForecast.cs` pair in `AlertHub.Api`, and an empty `Class1.cs` stub in
each class library. Likewise `ng new` generates a full marketing-style
placeholder landing page in `app.html` (20KB) with a matching spec asserting
on `<h1>Hello, client</h1>` text that has nothing to do with this app.

**Fix:** deleted the WeatherForecast files and both `Class1.cs` stubs;
replaced `app.html` with a bare `<router-outlet />` and trimmed `app.ts`
(dropped the now-unused `title` signal) and `app.spec.ts` (dropped the
title-text assertion, kept the "should create" smoke test). Rebuilt the
.NET solution and reran `ng build` + `ng test` after each deletion to
confirm nothing depended on the removed files before committing — not just
assumed the template files were inert.

---

<!-- Append further entries below as the build proceeds. Each entry for a
     rejected/corrected AI output should state: what was generated, what was
     wrong with it or why it was rejected, and what was done instead. -->
