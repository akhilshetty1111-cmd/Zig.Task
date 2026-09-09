# ZigZag

A project and task management application — Kanban boards, task workflows, comments,
activity history and a reporting dashboard.

Built as a production-shaped full-stack application: **ASP.NET Core 9 + CQRS + Dapper +
PostgreSQL** on the backend, **React + TypeScript + Vite + MUI** on the frontend, deployed
to Azure through GitHub Actions.

> **Status:** Phase 1 of 15 complete (repository and solution scaffolding).
> See [Roadmap](#roadmap) for what is built and what is next.

---

## Contents

- [Architecture](#architecture)
- [Technology stack](#technology-stack)
- [Repository layout](#repository-layout)
- [Prerequisites](#prerequisites)
- [Local setup](#local-setup)
- [Environment variables](#environment-variables)
- [Testing](#testing)
- [Roadmap](#roadmap)

---

## Architecture

Two independent solutions live side by side in this repository:

```
backend/   ->  ZigZag.sln          (ASP.NET Core Web API)
frontend/  ->  zigzag-web          (Vite + React SPA)
```

They are deployed separately (App Service and Static Web Apps) and share nothing but the
HTTP contract, so either can be rebuilt or redeployed without touching the other.

### Backend layering

The backend uses a Clean Architecture / CQRS split with strictly inward-pointing
dependencies:

```
ZigZag.API             controllers, middleware, auth config, Swagger, DI composition root
      |
      v
ZigZag.Infrastructure  Dapper repositories, PostgreSQL, JWT issuing, password hashing
      |
      v
ZigZag.Application     commands, queries, handlers, DTOs, validators, interfaces
      |
      v
ZigZag.Domain          entities, enums, domain rules   (zero dependencies)
```

**What problem this solves.** Business rules that live inside controllers cannot be tested
without spinning up HTTP, and business rules that live inside repositories cannot be tested
without a database. Pushing them into `Application` — which depends on nothing but
`Domain` — makes the interesting logic testable in milliseconds.

**Why this and not the alternatives.** A single-project API is faster to start and would be
the right call for a handful of endpoints, but ZigZag has three authorization surfaces
(system role, project membership role, resource ownership) and cross-cutting concerns
(validation, logging, transactions) that want one place to live. A full microservice split
would be the opposite mistake: independent deployability that buys nothing here, paid for
in distributed-transaction complexity.

`Application` declares interfaces (`ITaskRepository`, `IJwtTokenService`) and
`Infrastructure` implements them, so the dependency arrow points inward even though data
flows outward.

### Why Dapper and not EF Core

The Kanban board and dashboard are read-dominated with hand-shaped aggregate queries
(counts grouped by status and priority, overdue rollups, joins across four tables).
Dapper keeps that SQL explicit and reviewable instead of hidden behind LINQ translation,
and there is no change-tracker cost on the read path. The trade-off is real: no migrations
engine and no compile-time query checking, so schema changes are versioned SQL scripts
under [`database/migrations/`](database/migrations/) and repositories are covered by
integration tests against a real PostgreSQL container.

All SQL is parameterized. String concatenation with user input is never used.

---

## Technology stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 9, C#, REST |
| CQRS | MediatR 12 (commands, queries, pipeline behaviors) |
| Data access | Dapper, Npgsql, PostgreSQL 16 |
| Validation | FluentValidation (backend), Zod + React Hook Form (frontend) |
| Auth | JWT access tokens + refresh tokens, BCrypt password hashing |
| Docs | Swagger / OpenAPI with JWT authorization |
| Logging | Serilog (console + rolling file), Application Insights |
| Tests | xUnit, FluentAssertions, NSubstitute, Testcontainers, Respawn |
| Frontend | React 18, TypeScript, Vite 6, MUI 6, TanStack Query 5, Axios, React Router 7 |
| CI/CD | GitHub Actions |
| Cloud | Azure App Service, Azure Static Web Apps, Azure Database for PostgreSQL, Key Vault, Application Insights |

---

## Repository layout

```
Akhil/
├── backend/                       ASP.NET Core solution (ZigZag.sln)
│   ├── Directory.Build.props      shared MSBuild settings (net9.0, nullable, analyzers)
│   ├── Directory.Packages.props   central NuGet version management
│   ├── global.json                pins the .NET SDK major version
│   ├── src/
│   │   ├── ZigZag.API/
│   │   ├── ZigZag.Application/
│   │   ├── ZigZag.Domain/
│   │   └── ZigZag.Infrastructure/
│   └── tests/
│       ├── ZigZag.UnitTests/
│       └── ZigZag.IntegrationTests/
│
├── frontend/
│   └── zigzag-web/                Vite + React + TypeScript SPA
│
├── database/
│   ├── migrations/                versioned, forward-only SQL
│   ├── seeds/                     development seed data
│   └── scripts/                   helper scripts
│
├── docker/                        Dockerfiles + docker-compose for local dev
├── docs/                          architecture, database, api, deployment, cicd
├── .github/workflows/             CI and CD pipelines
├── .editorconfig
├── .gitignore
└── README.md
```

---

## Prerequisites

Install these before running anything locally.

### Required

| Tool | Version | Why | Get it |
|---|---|---|---|
| **.NET SDK** | **9.0** | Builds and runs the API. The solution targets `net9.0`. | <https://dotnet.microsoft.com/download/dotnet/9.0> |
| **Node.js** | 20.11+ or 22+ | Builds and runs the SPA. | <https://nodejs.org> |
| **PostgreSQL** | 16 or 17 | Application database. Not needed if you use Docker. | <https://www.postgresql.org/download/windows/> |
| **Git** | 2.4+ | Version control. | <https://git-scm.com/downloads> |

### Recommended

| Tool | Why |
|---|---|
| **Docker Desktop** | Runs PostgreSQL locally without installing it, and is **required** to run the integration tests (they start a throwaway Postgres container via Testcontainers). |
| **Azure CLI** | Provisioning and deploying Azure resources in Phase 14. |
| **psql** or **pgAdmin** | Applying the SQL migrations and inspecting data. `psql` ships with the PostgreSQL installer. |

> Installing Docker Desktop covers both PostgreSQL and the integration tests, so it is the
> single highest-value optional install.

### Verify your setup

```powershell
dotnet --list-sdks     # expect a 9.0.x entry
node --version         # expect v20.11+ or v22+
npm --version
git --version
docker --version       # optional but recommended
psql --version         # optional
```

---

## Local setup

### 1. Clone and restore

```powershell
git clone <repository-url> Akhil
cd Akhil
```

### 2. Backend

```powershell
cd backend
dotnet restore
dotnet build
```

Run the API:

```powershell
dotnet run --project src/ZigZag.API
```

| URL | What |
|---|---|
| <https://localhost:7135/swagger> | Swagger UI |
| <https://localhost:7135/health> | Health probe |

On first run, .NET may prompt you to trust the local HTTPS development certificate. If it
does not, run `dotnet dev-certs https --trust`.

### 3. Frontend

```powershell
cd frontend/zigzag-web
npm install
npm run dev
```

Opens on <http://localhost:5173>. The port is pinned (`strictPort`) because the backend
CORS policy whitelists that exact origin.

### 4. Database

Coming in Phase 2. Migrations will live in `database/migrations/` and be applied with
`psql` or the provided script.

---

## Environment variables

No secret is ever committed. `appsettings.json` ships with blank secret values that must be
supplied at runtime.

### Backend

| Setting | Environment variable | Notes |
|---|---|---|
| Database connection | `ConnectionStrings__ZigZagDb` | Local dev value is in `appsettings.Development.json`. |
| JWT signing key | `Jwt__Key` | Minimum 32 bytes. Never commit. |
| JWT issuer / audience | `Jwt__Issuer`, `Jwt__Audience` | |
| Allowed CORS origins | `Cors__AllowedOrigins__0` | Array; index per origin. An empty list blocks all cross-origin requests. |
| App Insights | `ApplicationInsights__ConnectionString` | |

Locally, prefer .NET user secrets over environment variables:

```powershell
cd backend/src/ZigZag.API
dotnet user-secrets set "Jwt:Key" "<a-long-random-value>"
```

In Azure these come from **App Service configuration** backed by **Key Vault**.
In CI they come from **GitHub Secrets**.

### Frontend

Copy `frontend/zigzag-web/.env.example` to `.env.local` and adjust. Only `VITE_`-prefixed
variables reach the browser bundle — everything there is public, so never put a secret in it.

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base URL of the ZigZag API, including `/api`. |
| `VITE_APP_NAME` | Display name. |

---

## Testing

```powershell
# Unit tests - fast, no external dependencies
dotnet test backend/tests/ZigZag.UnitTests

# Integration tests - requires Docker running
dotnet test backend/tests/ZigZag.IntegrationTests

# Frontend
cd frontend/zigzag-web
npm run lint
npm run typecheck
npm test
```

---

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 1 | Repository, solutions, project scaffolding, conventions | **Done** |
| 2 | Database schema, migrations, seeds, Dapper connection factory | Next |
| 3 | DI, MediatR/CQRS, global exception handling, Serilog, Swagger, FluentValidation | |
| 4 | Authentication: register, login, JWT, refresh tokens, current user | |
| 5 | Projects: CRUD, members, roles | |
| 6 | Tasks: CRUD, filtering, search, sort, pagination, assignment | |
| 7 | Kanban board: drag & drop, status changes, task history | |
| 8 | Comments, activity history, notifications | |
| 9 | Dashboard: statistics and charts | |
| 10 | Frontend polish: responsive, loading/error/empty states, toasts, a11y | |
| 11 | Unit and integration test coverage of key flows | |
| 12 | Docker and docker compose | |
| 13 | GitHub Actions CI | |
| 14 | Azure provisioning | |
| 15 | Continuous deployment | |

---

## Documentation

| Document | Contents |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Layering, CQRS, design decisions |
| [docs/database.md](docs/database.md) | Schema, indexes, migration strategy |
| [docs/api.md](docs/api.md) | Endpoint reference |
| [docs/deployment.md](docs/deployment.md) | Azure resources and setup |
| [docs/cicd.md](docs/cicd.md) | Pipelines and branch strategy |
