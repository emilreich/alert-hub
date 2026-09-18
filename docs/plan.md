# Plan: Alert Hub Implementation

This is the living implementation plan. It reflects the architecture
resolved through the full design discussion recorded in
`docs/technical-discussion.md` (narrative + decision timeline) and
`docs/transcript.md` (verbatim conversation) — those two files are the
authoritative record of *why*; this file is the current *what's being
built, in what order, and what's done so far*.

An earlier version of this document (and a companion `architecture.md`,
`decision-log.md`, `prompt-log.md`) described a different, simpler design
from an earlier session — SQLite, no auth, category/keyword alert rules,
no microservice, no permission system. That design was superseded after
further discussion; those files have been removed rather than kept around
as stale duplicates. Everything they captured that's still relevant lives
in `technical-discussion.md`'s Decision Timeline.

## Context

The brief, as originally given:

> We want users to be able to set up alerts so they get notified when
> something important happens in the world — like breaking news, market
> movements, natural disasters, that kind of thing. Should work for both
> email and Slack. Make it flexible enough that we can add more channels
> later. We need an admin view too.

Through collaborative discussion, this became a concrete architecture:
real auth with a permission/role model, a genuinely separate
categorization microservice with async HTTP+webhook dispatch, an
`(User, Outlet, Category)` subscription/matching engine, SignalR real-time
push, and an Angular app with User/Admin route trees over MSSQL/EF Core.
See `technical-discussion.md` for the full reasoning behind every one of
these decisions.

**Commit discipline (explicit requirement):** each milestone below lands
as its own commit (or small commit series for larger ones), not one large
commit at the end.

## Two ambiguities resolved before implementation started

1. **Categorization microservice is fully independent** — no project
   reference to `Core`, its own DTOs. Categories cross the service
   boundary as plain strings, resolved case-insensitively against the main
   API's `Category` table in the callback handler. The fixed
   category-name list is kept in sync by hand between the microservice's
   keyword-lookup table and the main API's seed data (no compiler check)
   — an accepted, documented tradeoff.
2. **SignalR group membership keys off permission claims, not a role
   claim** — for internal consistency with "JWT permissions are the
   authorization source of truth" everywhere else in the design.

## Solution layout

```
alert-hub/
  src/
    AlertHub.Core/            # entities, enums, abstractions — zero infra deps
    AlertHub.Infrastructure/  # EF Core (MSSQL), migrations, seed data, stub impls
    AlertHub.Api/             # ASP.NET Core host: controllers, hub, auth/DI composition root
    AlertHub.Tests/           # xunit: unit (Core) + lightweight integration (fake dispatcher)
  services/
    AlertHub.CategorizationService/   # standalone minimal API, own DTOs, no shared code
  client/                     # Angular 22, standalone components, signals
```
References: `Infrastructure → Core`; `Api → Core, Infrastructure`;
`Tests → Core, Infrastructure, AlertHub.CategorizationService`;
`CategorizationService → (none)`.

## Build order (each row = its own commit)

| # | Milestone | Status |
|---|---|---|
| M0 | Solution restructuring: MSSQL swap, new microservice project + sln entry | **Done** |
| M0.5 | Docs reconciliation — consolidate to plan/technical-discussion/transcript | **Done** |
| M1 | Data model + EF Core + migrations + deterministic seed data | **Done** |
| M2 | Auth: hashing, JWT issuance/validation, permission policies, fresh per-request `IsDisabled` check | Not started |
| M3 | Self-service CRUD: catalog, subscriptions, channel config | Not started |
| M4 | Admin CRUD: list/detail/edit-on-behalf/disable, audit writes | Not started |
| M5 | Categorization microservice + async dispatch + matching engine | Not started |
| M6 | Fan-out: `FeedEntry`, `NotificationLog`, stub channels | Not started |
| M7 | SignalR — load-bearing patterns only (new-feed-entry push, admin-edit push) | Not started |
| M8 | Feed read paths (ownership-scoped + permission-branching) | Not started |
| M9a–d | Angular: shell/auth, user screens, admin screens, SignalR wiring | Not started |
| M10 | Droppable real-time additions — only if time remains | Not started |
| M11 | Optional: as-built architecture summary | Not started |

Concrete file lists per milestone are in `technical-discussion.md`'s
implementation-planning entries; this table tracks status, not detail.

## Priority / cut list

**(a) Load-bearing — build even under time pressure:** data model +
EF/MSSQL; real auth (PasswordHasher + JWT); permission model baked into
JWT; subscription CRUD + matching engine; the physically separate
microservice + `ICategorizationDispatcher` (HTTP+webhook); `FeedEntry`
materialization + IDOR-safe ownership-scoped content endpoint; the two
original SignalR push patterns (new feed entry → user, admin edit →
affected user); admin CRUD; `AuditLog`/`NotificationLog` tables + writes;
a working (unstyled) Angular path through all of the above.

**(b) Valuable but droppable, with fallback:**
- Audit-trail UI → keep the table/writes, expose via API only.
- Keyword-based categorizer → fall back to a fixed lookup per seed template.
- "Admins"/`articles.viewAll` SignalR group broadcasts → admin screens refetch instead of live-pushing.
- Forced-logout push on disable → rely solely on the per-request `IsDisabled` check.
- Content-provider caching → skip; regenerate deterministic content per request.
- Angular polish → bare functional forms/tables.

**(c) Explicitly out of scope:** real OAuth; real outlet APIs;
Dockerfiles/compose; a queue-based dispatcher implementation; refresh
tokens; multi-role-per-user; feed read/unread state; retroactive feed
backfill.

## Testing approach

- **Unit** (`AlertHub.Tests/Unit/`): `MatchingEngineTests.cs` (pure logic, no infra), `KeywordCategorizerTests.cs` (via a test-only reference to the categorization service).
- **Integration** (`AlertHub.Tests/Integration/`): `CategorizationFlowTests.cs` — ingest → categorize-callback → match → `FeedEntry`/`NotificationLog`, using a `FakeCategorizationDispatcher` and EF Core's InMemory provider.
- **Not tested given the time budget:** SignalR hub behavior (manual verification), Angular component tests beyond the default scaffold smoke test, MSSQL-specific migration behavior (verified once manually).

## Issues caught during implementation

Terse, running log of concrete bugs/mistakes caught and fixed while
executing this plan — implementation-time process evidence, distinct from
`technical-discussion.md`'s architecture decisions. Full detail for each
also lives in the relevant commit message; this is the scannable index.

1. **Unrelated authenticated NuGet feed breaking restore.** A machine-wide
   `%APPDATA%\NuGet\NuGet.Config` (leftover from an unrelated
   employer/project) was being consulted on `dotnet new`, causing 401s.
   Fixed with a repo-local `NuGet.Config` (`<clear/>` + nuget.org only) so
   restore is reproducible regardless of any given machine's global config.
2. **`dotnet add package` silently resolved an incompatible EF Core
   version.** Adding the SQLite EF Core package with no version pin
   grabbed 10.0.12 (net10.0-only) against net8.0 projects — caught by an
   explicit `NU1202` restore error, not a silent failure. Fixed by pinning
   EF Core packages to `8.0.11` throughout.
3. **Template scaffolding cruft in the initial commit.** `dotnet new
   webapi`/`classlib` and `ng new` leave behind boilerplate
   (`WeatherForecastController`, empty `Class1.cs` stubs, a full marketing
   landing page in `app.html`) with no place in the repo's history.
   Removed, then rebuilt/retested to confirm nothing depended on them.
4. **A subagent deleted `docs/plan.md` during plan-mode exploration.**
   Explore/Plan subagents have Bash access even without Edit/Write; at some
   point during repo survey/plan-drafting, `docs/plan.md` was deleted —
   not requested by any instruction given. Caught via routine `git status`
   review before committing M0, restored with `git checkout -- docs/plan.md`
   before anything was staged. A reminder that subagent side effects need
   the same scrutiny as their reported output.
5. **`InvariantGlobalization=true` broke the MSSQL connection.** Left over
   in `AlertHub.Api.csproj` from the original SQLite-based scaffold
   (harmless there). `Microsoft.Data.SqlClient` needs culture info
   internally to open a connection, so it fails outright under invariant
   globalization mode — surfaced immediately as a hard `CultureNotFoundException`
   on the first `dotnet ef database update`, not a silent issue. Removed
   the flag from `AlertHub.Api.csproj` (left in place on the categorization
   microservice, which has no DB dependency and never hits this path).

## Verification checkpoints

- After M1: `dotnet ef database update` against local MSSQL; confirm schema + seed data.
- After M2: manual login via Swagger; confirm JWT permission claims; confirm a disabled seeded user is rejected on the next call.
- After M5/M6: `dotnet test`; manually trigger `POST /api/dev/ingest`; confirm `Pending → Categorized → FeedEntry/NotificationLog`.
- After M7/M8: manually verify SignalR push for both load-bearing patterns.
- After M9d: run the Angular app end to end for both seeded User and Admin accounts.
