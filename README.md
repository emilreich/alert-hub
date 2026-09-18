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

**Backend**

```
dotnet build
dotnet run --project src/AlertHub.Api
```

**Frontend**

```
cd client
npm install
npm start
```

Note: a repo-local `NuGet.Config` pins package sources to nuget.org only —
this works around an unrelated authenticated feed some dev machines have
configured globally (see `docs/decision-log.md`, D7).
