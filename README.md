# Alert Hub

A world-events alerting system (email + Slack, extensible to more channels)
built from a deliberately vague product brief, as a take-home exercise.

**Start here:** [`docs/plan.md`](docs/plan.md) — the current implementation
plan and milestone status. See
[`docs/technical-discussion.md`](docs/technical-discussion.md) for the full
architecture and decision trail (why every decision was made, including
course-corrections), and [`docs/transcript.md`](docs/transcript.md) for the
verbatim conversation record.

## Stack

- **Backend:** ASP.NET Core Web API, EF Core, SQL Server
- **Categorization service:** a separate, standalone minimal API (its own
  process, no shared code with the main backend) that classifies articles
  into categories — a stand-in for what would become a real AI-backed
  microservice
- **Frontend:** Angular — one app serving both the end-user experience
  (configure subscriptions, read a personal feed) and the admin experience
  (manage users, view activity), gated by real login and a permission-based
  authorization model, not two separate apps

## Repo layout

```
alert-hub/
├── docs/                              ← plan, architecture/decision trail, transcript (read these first)
├── src/
│   ├── AlertHub.Core/                 ← domain model, abstractions, matching engine
│   ├── AlertHub.Infrastructure/       ← EF Core (SQL Server), migrations, seed data, stub implementations
│   ├── AlertHub.Api/                  ← ASP.NET Core Web API: controllers, auth, SignalR hub
│   └── AlertHub.Tests/                ← xUnit tests
├── services/
│   └── AlertHub.CategorizationService/  ← standalone article-categorization microservice
└── client/                            ← Angular app (user + admin)
```

## Running locally

**Backend**

Requires a local SQL Server instance (LocalDB works — `(localdb)\MSSQLLocalDB`
is the default connection string in `appsettings.json`).

```
dotnet build
dotnet ef database update --project src/AlertHub.Infrastructure --startup-project src/AlertHub.Api
dotnet run --project src/AlertHub.Api
```

**Categorization service**

```
dotnet run --project services/AlertHub.CategorizationService
```

**Frontend**

```
cd client
npm install
npm start
```

Note: a repo-local `NuGet.Config` pins package sources to nuget.org only —
this works around an unrelated authenticated feed some dev machines have
configured globally.
