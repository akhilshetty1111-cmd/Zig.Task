# Architecture

This document records the significant architectural decisions in ZigZag and the reasoning
behind each one. Every decision is stated as: the problem, the choice, the alternatives,
and why the choice fits ZigZag specifically.

---

## 1. Two separate solutions in one repository

**Problem.** The API and the SPA have different toolchains, different build times and
different deployment targets, but they evolve together — a new endpoint and the screen that
calls it belong in the same pull request.

**Choice.** A single Git repository containing two independent solutions:
`backend/ZigZag.sln` and `frontend/zigzag-web`.

**Alternatives.**
- *Two repositories.* Clean separation, but a contract change becomes two PRs that can merge
  out of order, and there is no single commit that represents a working system.
- *One solution containing both.* MSBuild driving npm is fragile and makes the .NET build
  depend on Node being installed.

**Why this fits ZigZag.** One team, one release train, but two genuinely separate build
pipelines and Azure resources. A monorepo with independent solutions gives atomic contract
changes without coupling the builds. CI runs the backend and frontend workflows on path
filters, so a frontend-only change does not rebuild the API.

---

## 2. Clean Architecture layering

**Problem.** Business rules mixed into controllers require HTTP to test; business rules
mixed into repositories require a database to test. Both make the interesting logic the
hardest logic to verify.

**Choice.** Four projects with dependencies pointing inward:

| Project | Depends on | Contains |
|---|---|---|
| `ZigZag.Domain` | *nothing* | Entities, enums, domain rules |
| `ZigZag.Application` | Domain | Commands, queries, handlers, DTOs, validators, interfaces |
| `ZigZag.Infrastructure` | Application, Domain | Dapper repositories, PostgreSQL, JWT, hashing |
| `ZigZag.API` | Application, Infrastructure | Controllers, middleware, Swagger, DI |

`Application` declares the interfaces; `Infrastructure` implements them. The dependency
arrow therefore points inward even though data flows outward at runtime. This is the
Dependency Inversion Principle applied at the project level, and the compiler enforces it —
`Domain` physically cannot reference `Npgsql`.

**Alternatives.**
- *Single project.* Correct for a small API. Rejected because ZigZag has three distinct
  authorization surfaces and several cross-cutting concerns that need a home.
- *Vertical slice architecture.* Genuinely attractive; each feature owns its full stack.
  Partially adopted — see decision 4 — but the shared `Domain`/`Infrastructure` split is
  kept because repositories and the connection factory are shared by every slice.
- *Microservices.* Buys independent deployability that ZigZag does not need, and turns a
  "create task and write history row" transaction into a distributed one.

---

## 3. CQRS with MediatR

**Problem.** Reads and writes have different shapes. Creating a task validates input,
enforces permissions, writes two tables and records history. Loading the Kanban board joins
four tables and projects a flat DTO. Forcing both through one "service" layer produces
classes that grow without bound.

**Choice.** Every operation is a `Command` (changes state) or a `Query` (returns data), each
with exactly one handler, dispatched through MediatR.

This buys three concrete things:

1. **One class, one operation.** `CreateTaskCommandHandler` has one reason to change.
2. **Cross-cutting behavior without inheritance.** MediatR pipeline behaviors run validation,
   logging and transaction management around *every* handler. Adding a behavior is one
   registration, not an edit to fifty handlers.
3. **Trivially testable handlers.** A handler takes its dependencies through the constructor
   and returns a value. No HTTP, no DI container, no database.

**Alternatives.**
- *Service classes.* `ITaskService` with fifteen methods. Fewer files, but cross-cutting
  concerns end up copy-pasted and the class becomes a merge-conflict magnet.
- *Handlers without MediatR* (inject the handler directly into the controller). Removes a
  dependency, but also removes the pipeline, which is the main reason to use MediatR here.
- *Full CQRS with separate read/write databases and eventual consistency.* Enormous
  complexity for a system with one database and no scale pressure. Explicitly rejected —
  ZigZag uses CQRS as a *code organization* pattern, not an infrastructure one.

**Boundary rule.** Commands do not return query results beyond the identifier of what they
created. A caller that needs the full object issues a query.

---

## 4. Feature folders inside Application

Handlers are grouped by feature, not by technical role:

```
Application/Features/Tasks/Commands/CreateTask/
    CreateTaskCommand.cs
    CreateTaskCommandHandler.cs
    CreateTaskCommandValidator.cs
```

Everything that changes together sits together. The alternative — `Commands/`, `Handlers/`,
`Validators/` as top-level folders — means one logical change touches three distant
directories.

---

## 5. Dapper instead of EF Core

**Problem.** The dashboard and board are read-dominated with aggregate queries: counts
grouped by status and priority, overdue rollups, joins across tasks, users, projects and
comments.

**Choice.** Dapper with hand-written, parameterized SQL. Schema changes are forward-only
SQL scripts in `database/migrations/`.

**Alternatives.**
- *EF Core.* Migrations, change tracking, LINQ, compile-time-checked queries. Real
  advantages. The cost is that the queries that matter most here are the ones LINQ
  translates least predictably, and diagnosing a bad plan means reading generated SQL.
- *Raw ADO.NET.* Dapper *is* ADO.NET plus object mapping, with no meaningful overhead.
  Choosing raw ADO.NET means hand-writing `IDataReader` loops for no benefit.

**Why this fits ZigZag.** The queries are the product. Keeping them visible and reviewable
in the repository — where a slow one shows up in a diff — is worth losing the migrations
engine. The costs are mitigated deliberately: versioned SQL scripts replace migrations, and
integration tests run every repository against a real PostgreSQL container so a typo in SQL
fails the build rather than production.

**Non-negotiable.** All SQL is parameterized. User input is never concatenated into a query.

---

## 6. Central Package Management

All NuGet versions live in `backend/Directory.Packages.props`; `.csproj` files reference
packages without a version. This makes version drift between projects impossible and makes
a dependency bump a one-file diff.

---

## 7. Naming: `TaskItem`, not `Task`

`ImplicitUsings` imports `System.Threading.Tasks` into every file, so a domain type named
`Task` collides with the BCL `Task` and a `TaskStatus` enum collides with
`System.Threading.Tasks.TaskStatus`. Rather than sprinkle `using` aliases through the
codebase, the CLR types are named `TaskItem`, `TaskItemStatus` and `TaskPriority`.

The database tables stay `tasks` and `task_comments`, and the REST routes stay `/api/tasks`.
Only the C# identifiers differ.

---

## 8. CORS fails closed

The allowed-origin list comes from configuration. If it is empty, the policy is registered
with no origins — every cross-origin request is rejected and a warning is logged.
`AllowAnyOrigin()` is never called: the API sends credentials, and browsers reject a
wildcard origin combined with credentials anyway.

Development origins are in `appsettings.Development.json`. Production origins come from
Azure App Service configuration.

---

## 9. Enum storage

Enums are stored in PostgreSQL as SCREAMING_SNAKE_CASE strings (`IN_PROGRESS`) guarded by
`CHECK` constraints, not as integers and not as native PostgreSQL enum types.

- *Integers* make raw SQL and database dumps unreadable, and renumbering silently corrupts data.
- *Native PG enums* are readable but altering one requires a migration and they interact
  awkwardly with Npgsql type mapping.
- *Strings + CHECK* are readable in every tool, cheap to extend, and still validated by the
  database rather than trusting the application.

The translation lives in a Dapper type handler in `Infrastructure`, so `Domain` stays free
of persistence concerns.

---

## Decision log

| # | Decision | Phase |
|---|---|---|
| 1 | Monorepo, two solutions | 1 |
| 2 | Clean Architecture layering | 1 |
| 3 | CQRS via MediatR | 1 |
| 4 | Feature folders | 1 |
| 5 | Dapper over EF Core | 1 |
| 6 | Central Package Management | 1 |
| 7 | `TaskItem` naming | 1 |
| 8 | CORS fails closed | 1 |
| 9 | Enums as checked strings | 1 |
