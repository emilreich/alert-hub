# Alert Hub

A world-events alerting system (email + Slack, extensible to more channels)
built from a deliberately vague product brief, as a take-home exercise.

**Start here:** [`docs/plan.md`](docs/plan.md) — the plan of attack, scoping
decisions, and architecture, written before any code. See also
[`docs/decision-log.md`](docs/decision-log.md) for decisions and
course-corrections made during the build, and
[`docs/prompt-log.md`](docs/prompt-log.md) for the verbatim prompt history.

## Stack

- **Backend:** ASP.NET Core Web API, EF Core, SQLite
- **Frontend:** Angular (admin view)

## Repo layout

```
alert-hub/
├── docs/                    ← plan, decision log, prompt log (read these first)
├── src/
│   ├── AlertHub.Core/               ← domain model, channel abstraction, matching engine
│   ├── AlertHub.Infrastructure/     ← EF Core, Email/Slack channels, event simulator
│   ├── AlertHub.Api/                ← ASP.NET Core Web API
│   └── AlertHub.Tests/              ← xUnit tests
└── client/                  ← Angular admin app
```

## Running locally

_Filled in once the scaffold lands — see commit history for progress._
