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

## 10. Enum conversion is explicit, not a Dapper `ITypeHandler`

**Problem.** Decision 9 stores enums as checked strings. Dapper needs to translate
`TaskItemStatus.InProgress` to/from `"IN_PROGRESS"` on every query that touches a
`status`, `priority` or `role` column.

**What was tried first.** A `SqlMapper.TypeHandler<TEnum>` registered once at startup via
`SqlMapper.AddTypeHandler`. This is Dapper's documented extension point for exactly this
kind of custom conversion.

**Why it was rejected.** It does not work. Dapper hardcodes its own enum handling for both
directions - it converts an enum query parameter to its underlying `int` by default, and it
calls `Enum.Parse` directly when reading a string column into an enum-typed property - and
that built-in path runs *instead of* any registered `ITypeHandler`. This was confirmed
empirically, not assumed: the handler was verified present in Dapper's internal handler
dictionary via reflection, and the query still failed both directions (`operator does not
exist: text = integer` writing; `Requested value 'IN_REVIEW' was not found` reading, because
Dapper's fallback does not know about the underscore).

**Choice.** Enum columns are selected as `text` into a plain `string` property on the
repository's row DTO. `DbEnumMapper.Parse<TEnum>` and `DbEnumMapper.ToDbValue<TEnum>`
(`ZigZag.Infrastructure/Persistence/Mapping/`) convert explicitly at the repository
boundary - `Parse` when building the object returned to `Application`, `ToDbValue` when
binding a query parameter. Domain and Application code never sees the string form.

**Alternative not taken.** Storing enums as native PostgreSQL enum types side-steps this
specific problem (Npgsql maps them without a handler) but reopens decision 9's cost:
altering a native enum requires a migration, and it interacts awkwardly enough with Npgsql's
type mapping to introduce its own class of problems.

## 11. API response conventions

Success responses are **not** wrapped - a handler's DTO is returned directly with the
appropriate 2xx status. Only failures use the `ApiErrorResponse` envelope
(`success`/`message`/`errors`/`traceId`), built by the global exception handler. This
matches ordinary REST practice (the body's shape describes the resource, not a transport
wrapper) and keeps every handler free of `ApiResponse<T>` boilerplate on its return type.

## 12. `ValidationContext<T>` is not safe to share across validators

**Problem.** `ValidationBehavior` runs every registered `IValidator<TRequest>` for a
request and aggregates their failures.

**What went wrong first.** The initial implementation - the same pattern used by several
popular MediatR+FluentValidation reference templates - built one `ValidationContext<TRequest>`
and passed it to every validator:

```csharp
var context = new ValidationContext<TRequest>(request);
var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
```

**Why it's wrong.** `ValidationContext<T>` is a stateful accumulator, not an immutable
snapshot - FluentValidation supports child/include-rule scenarios that deliberately want
several validators to append into one shared failure list. Confirmed empirically with a
throwaway two-validator repro: passing the same context to both meant *each* validator's
returned `ValidationResult.Errors` contained *both* validators' failures - every failure
duplicated once per validator registered for that request. A single validator per request
(the common case) never triggers it, which is exactly why it is easy to ship unnoticed.

**Choice.** A fresh `ValidationContext<TRequest>` per validator call
(`ZigZag.Application/Common/Behaviors/ValidationBehavior.cs`). Any command or query that
ever ends up with more than one registered validator gets correct, non-duplicated field
errors.

## 13. Refresh token cookie is `SameSite=None`, not `Lax`/`Strict`

The refresh token is delivered as an `HttpOnly`, `Secure`, `SameSite=None` cookie scoped to
`/api/auth`, never returned in a JSON body. `SameSite=None` specifically - not the more
common `Strict`/`Lax` default - because production puts the SPA (Azure Static Web Apps) and
the API (Azure App Service) on different domains: a genuinely cross-site relationship, not
just cross-port like local dev. `Strict`/`Lax` would silently stop sending the cookie in
production while working fine locally, the worst kind of bug to catch late. The CSRF
exposure this opens is bounded: CORS already restricts which origins can read a response,
so a forged cross-site request could at most invalidate the caller's own session, not
obtain a token.

## 14. Dapper repository row DTOs use `DateTime`, not `DateTimeOffset`

**Problem.** Every `timestamptz` column needs to become a `DateTimeOffset` for
Domain/Application, which use it exclusively (never naive `DateTime`).

**What broke.** A row DTO record with `DateTimeOffset CreatedAt` failed at runtime -
confirmed by actually calling `GET /api/auth/me`, not caught by the build - with "no
parameterless constructor or matching signature ... System.DateTime createdat". Npgsql
returns `timestamptz` columns as `System.DateTime`, and Dapper's record materialization
requires an exact constructor-parameter type match; a `DateTimeOffset` property is a type
mismatch at that step even though `DateTime` converts to `DateTimeOffset` implicitly
everywhere else in C#.

**Choice.** Repository row DTOs use `DateTime` for every `timestamptz` column;
`DbDateTimeMapper.ToUtcOffset` (`ZigZag.Infrastructure/Persistence/Mapping/`) converts
explicitly when mapping to the Domain/Application type, forcing `DateTimeKind.Utc` rather
than trusting Npgsql to have set it - the implicit `DateTime`-&gt;`DateTimeOffset`
conversion silently treats `DateTimeKind.Unspecified` as local time, which would be wrong
for what is always a UTC instant. Every repository from Phase 5 onward follows this same
pattern.

## 15. `count(*)` needs an explicit `::int` cast for Dapper record binding

The same exact-constructor-match issue as decision 14, a different type: PostgreSQL's
`count(*)` returns `bigint` (`long` in C#), not `int`. A row DTO record with `int
MemberCount` failed the same way GetById's `DateTimeOffset` did - reproduced by actually
calling `POST /api/projects`. Cast in SQL (`count(*)::int`) rather than widening the DTO
property to `long`: a project's member count will never need more than 32 bits, and `int`
is the correct type for what the API actually returns. `ExecuteScalarAsync<T>` (used by
`CountOwnersAsync`) is a different Dapper code path that already coerces numeric types
leniently and was not affected - this only bites record-based `Query`/`QuerySingle`.

## 16. Enum request/response fields need `JsonStringEnumConverter`

System.Text.Json's default enum handling serializes/deserializes an enum as its numeric
value, not its name - `POST /api/projects/{id}/members` with `{"role":"Member"}` failed
with a 400 ("could not be converted to AddProjectMemberRequest"), confirmed by actually
sending the request. `AddJsonOptions` in Program.cs registers a global
`JsonStringEnumConverter`, so every enum-typed request or response field - `ProjectRole`
here, `TaskItemStatus`/`TaskPriority` from Phase 6 on - reads and writes as its readable
name instead. This also makes response bodies consistent with DTOs that already exposed an
enum as `.ToString()` manually (`UserDto.Role`), so the fix does not change any existing
JSON shape, only fixes what was previously broken.

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
| 10 | Explicit enum mapping, not a Dapper ITypeHandler | 2 |
| 11 | API response conventions (unwrapped success, enveloped failure) | 3 |
| 12 | Fresh ValidationContext per validator | 3 |
| 13 | Refresh cookie is SameSite=None (cross-domain production) | 4 |
| 14 | Dapper row DTOs use DateTime, converted explicitly to DateTimeOffset | 4 |
| 15 | count(*) needs ::int cast for Dapper record binding | 5 |
| 16 | Global JsonStringEnumConverter for enum request/response fields | 5 |
