# Architecture & Data Model

Written before implementation, per `plan.md` step 2. Covers the domain
model, the channel abstraction, the event/matching/delivery flow, and the
API surface. Implementation may adjust details as issues surface — any
material deviation gets an entry in `decision-log.md`, not a silent edit
here.

## Identity — deliberately minimal

No real authentication is being built (see `plan.md`, out-of-scope list).
Stand-in: a small set of seeded `User` rows (id, email, display name), picked
from a plain dropdown in the Angular app in place of a login screen. This
keeps "alert rules belong to a user" real and testable without spending
budget on auth. Called out explicitly so it doesn't read as a forgotten
feature.

## Core entities

```
User
  Id, Email, DisplayName

AlertRule
  Id, UserId (FK → User)
  Name                          -- human label, e.g. "Big market moves"
  Category                      -- enum: BreakingNews | Markets | NaturalDisaster | Other
  Keyword                       -- nullable, simple case-insensitive substring match against Event.Title/Body
  MinSeverity                   -- enum: Low | Medium | High | Critical
  ChannelKeys                   -- list of channel keys this rule delivers to, e.g. ["email", "slack"]
  IsActive                      -- bool
  CreatedAt

Event                           -- produced by the simulator, or admin-triggered
  Id, Category, Severity, Title, Body, Source, OccurredAt

Delivery                        -- one row per (matched rule, channel) attempt
  Id, AlertRuleId (FK), EventId (FK)
  ChannelKey
  Status                        -- enum: Pending | Sent | Failed
  Attempts, LastAttemptAt, Error (nullable)
  CreatedAt
```

`ChannelKeys` on `AlertRule` is a simple string list (stored as JSON in
SQLite via EF Core value conversion) rather than a join table — this is a
deliberate simplicity choice given the small, fixed cardinality of channels;
revisit if channels ever need their own per-rule config (e.g. a
per-rule Slack webhook override), which is out of scope here.

## The channel abstraction

```csharp
// AlertHub.Core
public interface INotificationChannel
{
    string Key { get; }          // "email", "slack" — matched against AlertRule.ChannelKeys
    string DisplayName { get; }
    Task SendAsync(NotificationMessage message, CancellationToken ct);
}
```

`AlertHub.Infrastructure` provides `EmailChannel` (SMTP) and `SlackChannel`
(incoming webhook), both registered in DI as `INotificationChannel`. The
dispatch service resolves the full set via `IEnumerable<INotificationChannel>`
and looks up by key — adding a channel is: implement the interface, register
it in `Program.cs`. Nothing in `Core`'s matching/dispatch logic changes. A
`LogChannel` (writes to a log, key `"log"`) is included specifically to prove
this without needing real Slack/SMTP credentials to demo the whole pipeline.

## Flow

1. **Event arrives** — either the background `EventSimulatorService`
   (`IHostedService`, emits a random event every 30–60s from a fixed pool of
   templates) or an admin manually triggers one via
   `POST /api/events/simulate`.
2. **Matching** — `AlertMatchingEngine` (in `Core`, no infra dependencies,
   the main unit-test target) loads active rules and returns the set of
   `(rule, channelKey)` pairs where `rule.Category == event.Category`,
   `event.Severity >= rule.MinSeverity`, and (if set) `rule.Keyword` is
   found in the event's title/body.
3. **Dispatch** — for each matched pair, `DeliveryService` creates a
   `Delivery` row, resolves the channel by key, and calls `SendAsync`,
   wrapped in a bounded retry (Polly, 3 attempts, exponential backoff),
   updating `Status`/`Attempts`/`Error` as it goes.
4. **Admin view** reads `Delivery` + joins to see what was sent, to whom,
   on which channel, and whether it succeeded.

## API surface (sketch — may gain small additions during implementation)

```
GET    /api/users                       -- seeded demo users, for the picker
GET    /api/channels                    -- registered channels (key, display name) — populates rule-creation UI

GET    /api/alert-rules?userId=         -- list (all, or scoped) — admin view lists all
POST   /api/alert-rules
PUT    /api/alert-rules/{id}
DELETE /api/alert-rules/{id}

GET    /api/events                      -- recent simulated events
POST   /api/events/simulate             -- admin: fire a specific or random event now

GET    /api/deliveries?ruleId=&status=  -- admin delivery log
```

## Why this shape

- Matching engine has zero infrastructure dependencies → the part of the
  system with actual conditional logic is the easiest to unit test and the
  cheapest to trust.
- Channel interface lives in `Core`; implementations live in
  `Infrastructure` → the "add a channel later" claim is a project-reference
  fact, not a comment.
- Delivery is modeled as its own row per (rule, channel, event) rather than
  a boolean on the rule, so retry state and history are real, queryable
  data — which is what an operator admin view is actually for.
