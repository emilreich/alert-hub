# Plan of Attack — Alert Hub

## The brief, as given

> We want users to be able to set up alerts so they get notified when something
> important happens in the world — like breaking news, market movements,
> natural disasters, that kind of thing. Should work for both email and Slack.
> Make it flexible enough that we can add more channels later. We need an
> admin view too.

No further specification was provided. This document records how the
ambiguity was resolved, in what order the work was tackled, and why — written
before implementation started, not reconstructed afterward.

## What's actually being evaluated

The instructions accompanying the brief are explicit that the deliverable is
the *process*, not a polished product: how the ambiguity was scoped, which
assumptions were challenged, and what was rejected or course-corrected along
the way. This plan, `decision-log.md`, and `prompt-log.md` are therefore
treated as first-class deliverables, written and updated as work happens —
not a post-hoc summary.

## Ambiguities and how they were resolved

### 1. What does "something important" mean, and where do events come from?

Real event detection — classifying breaking news, market anomalies, and
natural disasters from raw feeds — is a research problem, not something
buildable (or fairly evaluable) in a 24h exercise. Building a shallow fake of
it would burn the time budget on the part of the system that matters least to
this brief's actual ask (alerting infrastructure), while producing a
detector too thin to demonstrate anything real.

**Decision:** treat "event" as an external input the system reacts to, and
build a simulated event source (a seedable generator producing events with a
category, severity, and free-text body) behind the same kind of interface a
real feed integration would sit behind. The system's job starts at "an event
arrived" — sourcing/classifying real-world events is explicitly out of scope,
called out as a scope cut rather than silently ignored. A thin real feed
(e.g. an RSS pull) is a documented stretch goal, not a commitment.

### 2. "Flexible enough to add more channels later"

This is the one architectural requirement the brief states directly rather
than implies. It reads as a request for a channel abstraction, not "build
Email and Slack as two separate hardcoded paths."

**Decision:** define an `INotificationChannel` (or equivalent) contract that
Email and Slack both implement, registered via DI, addressed by a channel key
stored on the subscription/rule. Prove extensibility by keeping a third,
trivial channel (e.g. a console/log or webhook-to-generic-URL channel) as a
one-file addition — evidence that "add a channel" doesn't touch dispatch
logic, not just a claim.

### 3. What is the admin view for?

Not specified. Two readings are plausible: (a) an end-user-facing view of
one's own alerts, or (b) an operator/ops view over the whole system. Brief
says "admin view," not "user settings page" or "user dashboard" — language
suggests (b).

**Decision:** build an operator admin view: browse all users' alert rules,
inspect delivery history (success/failure per channel, with retry state),
and manually fire a test event to verify a rule end-to-end. Explicitly not
building a separate polished end-user self-service portal — noted as a scope
cut, since a functionally-equivalent CRUD UI for "my own rules" would mostly
duplicate the admin CRUD screens without adding new design decisions.

### 4. What does a "subscription" look like?

Considered two shapes:

- **Global rules:** a small admin-curated list of alert types; users
  subscribe/unsubscribe and choose channels. Simple, but the admin view has
  little real data to show and it understates the system's flexibility.
- **Per-user filtered subscriptions:** users define rules with filters
  (category, keyword, minimum severity) and per-rule channel selection.

**Decision:** per-user filtered subscriptions. It's a heavier build but gives
the matching engine actual logic to test, gives the admin view meaningful
data (rules vary per user), and is a more honest interpretation of "set up
alerts" from the brief's wording. Documented as the more time-expensive
choice, taken deliberately rather than by default.

## Scope

**In scope**
- Alert rule CRUD (category / keyword / min-severity filters, channel
  selection) per user
- Simulated event source, seedable and manually triggerable
- Rule-matching engine (event → matching rules → dispatch)
- Notification dispatch behind a channel abstraction: Email (SMTP) + Slack
  (incoming webhook), plus one throwaway channel proving extensibility
- Delivery log with status (sent / failed / retrying) and retry/backoff
- Admin view (Angular): rules across all users, delivery log, manual event
  trigger
- Persistence via EF Core (SQLite) with real migrations
- Unit tests for the matching engine and dispatch/retry logic

**Explicitly out of scope (documented cuts, not oversights)**
- Real-world event detection / classification from live feeds
- End-user self-service portal (admin view only)
- Additional channels beyond Email/Slack/throwaway-demo (architecture
  supports them; not building them is the point)
- Full auth/IAM (a minimal seeded-user model stands in for real auth)
- Horizontal scaling / message-queue-backed dispatch (noted as the obvious
  next step for a real system, not built here)

## Architecture at a glance

- **Backend:** ASP.NET Core Web API, EF Core + SQLite
  - `AlertHub.Core` — domain model, `INotificationChannel` contract, matching
    engine (framework-agnostic, unit-testable in isolation)
  - `AlertHub.Infrastructure` — EF Core persistence, Email/Slack channel
    implementations, the simulated event source
  - `AlertHub.Api` — controllers, DI wiring, hosted services
  - `AlertHub.Tests` — xUnit, focused on matching + dispatch logic
- **Frontend:** Angular admin SPA — rules table, delivery log, manual trigger
  control
- **Why this stack:** it's the stack I'm strongest in, which matters given
  the exercise's 24h budget is meant to go toward thinking and judgment, not
  fighting an unfamiliar framework.

## Order of work

1. **Docs first** (`plan.md`, `decision-log.md`, `prompt-log.md`) — this step
2. Data model + API contract sketch (small, before code)
3. Solution scaffold: `.sln` + 4 projects, Angular app skeleton, empty but
   wired (DI, EF migrations, routing) — first commit
4. Domain model + channel abstraction + Email/Slack implementations
5. Event simulator + rule-matching engine
6. Delivery pipeline: dispatch, logging, retry/backoff
7. Admin-facing API endpoints (rules across users, delivery log, manual
   trigger)
8. Angular admin UI over those endpoints
9. Unit tests for matching/dispatch
10. README pass, final docs/prompt-log update, commit hygiene check

Each numbered step is intended to land as its own commit (or small commit
series) with a message describing the milestone, so the git history itself
is legible evidence of the order of work.

## What could get cut if time runs short

In priority order, the first things to drop if the 24h budget is tight:
1. Third "throwaway" channel (Email + Slack alone already answer the brief;
   the third is purely to prove the extension story)
2. Retry/backoff sophistication (fall back to single-attempt + logged failure)
3. Angular polish (a functional, unstyled table beats a half-built styled one)
4. Real feed stretch goal (was never committed to)

Unit tests on the matching engine and the channel abstraction itself are the
last things to cut — they're what makes "flexible enough to add channels
later" a demonstrated property instead of a claim.
