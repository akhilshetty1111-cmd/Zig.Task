# Database

PostgreSQL 16+. Schema changes are **forward-only, versioned SQL scripts** — there is no
ORM migration engine, because ZigZag uses Dapper (see
[architecture.md, decision 5](architecture.md#5-dapper-instead-of-ef-core)).

## Applying the schema locally

```powershell
# 1. Create the role and database once (see README.md > Local setup for the
#    full first-time setup, including the Postgres install).
psql -U postgres -h 127.0.0.1 -c "CREATE ROLE zigzag LOGIN PASSWORD 'zigzag';"
psql -U postgres -h 127.0.0.1 -c "CREATE DATABASE zigzag OWNER zigzag;"

# 2. Apply migrations, then load development seed data.
cd database/scripts
./apply-migrations.ps1
./seed-dev-data.ps1
```

Both scripts accept `-PgHost`, `-Port`, `-DatabaseName`, `-Username`, `-Password` and
`-PgBinPath` to target a different server. `seed-dev-data.ps1` is idempotent — it wipes its
own rows before reloading, so re-running it is always safe. `apply-migrations.ps1` is not
idempotent by design: migrations are forward-only, so re-running an already-applied one is
expected to fail rather than silently no-op.

All seeded users share the password `Passw0rd!` (BCrypt-hashed, work factor 11).

## Conventions

| Rule | Detail |
|---|---|
| Naming | `snake_case` tables and columns, plural table names (`tasks`, `task_comments`) |
| Primary keys | `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` |
| Timestamps | `timestamptz`, never naive `timestamp` |
| Enums | `text` columns constrained by `CHECK`, values in `SCREAMING_SNAKE_CASE` |
| Soft state | `is_active` / `is_archived` booleans rather than deleting rows |
| Migrations | `database/migrations/NNN_description.sql`, applied in filename order, never edited once merged |
| `updated_at` | Maintained by a `BEFORE UPDATE` trigger (`set_updated_at()`), not application code |

## Enum handling with Dapper

Enum columns are `text`, not native PostgreSQL enum types or integers (rationale:
[architecture.md, decision 9](architecture.md#9-enum-storage)), and conversion to/from the
C# enum happens through `DbEnumMapper.Parse<TEnum>` / `DbEnumMapper.ToDbValue<TEnum>`
(`ZigZag.Infrastructure/Persistence/Mapping/`) rather than a registered Dapper
`ITypeHandler`. **This is a hard requirement, not a style preference** — a
`SqlMapper.TypeHandler<TEnum>` was tried first and does not work: Dapper hardcodes its own
enum conversion for both query parameters and column deserialization, and that path runs
instead of any registered handler regardless of registration. Confirmed empirically; see
[architecture.md, decision 10](architecture.md#10-enum-conversion-is-explicit-not-a-dapper-itypehandler)
for how. Every repository must select enum columns into a `string` property and call
`DbEnumMapper.Parse` explicitly.

## Schema

```
users ──┬──< projects (owner_id)
        ├──< project_members >── projects
        ├──< tasks (created_by)
        ├──< tasks (assigned_to, nullable)
        ├──< task_comments
        ├──< task_attachments
        ├──< task_history (changed_by)
        ├──< notifications
        └──< refresh_tokens

projects ──┬──< labels
           └──< tasks

tasks ──┬──< task_comments
        ├──< task_attachments
        ├──< task_history
        └──< task_labels >── labels
```

| Table | Purpose | Notable constraints |
|---|---|---|
| `users` | Accounts | `email` is `citext` (case-insensitive unique); `role` checked against `ADMIN`/`PROJECT_MANAGER`/`MEMBER` |
| `projects` | Top-level containers | `owner_id` is `ON DELETE RESTRICT` - a project is never silently orphaned |
| `project_members` | Per-project role (`OWNER`/`MANAGER`/`MEMBER`/`VIEWER`) | Composite PK `(project_id, user_id)`; distinct from `users.role` - see architecture.md |
| `labels` | Per-project tags | Unique `(project_id, name)`; `color` checked as `#rrggbb` |
| `tasks` | The core entity | `status`/`priority` checked strings; `completed_at IS NOT NULL` iff `status = 'DONE'`, enforced by CHECK; `assigned_to` is `ON DELETE SET NULL` |
| `task_labels` | Task ⟷ label, many-to-many | Composite PK |
| `task_comments` | Discussion thread per task | |
| `task_attachments` | Uploaded files per task | `file_size_bytes > 0` |
| `task_history` | Append-only audit trail | Written by command handlers in the same transaction as the mutation; never updated/deleted |
| `notifications` | Per-user notifications | Partial index on unread only |
| `refresh_tokens` | JWT refresh token store | Stores a SHA-256 hash, never the raw token; `replaced_by_token_hash` supports rotation-with-reuse-detection (Phase 4) |

## Indexes

Beyond the automatic primary/unique-key indexes:

```
tasks(project_id)
tasks(assigned_to)
tasks(status)
tasks(priority)
tasks(due_date)                    -- partial: WHERE due_date IS NOT NULL
tasks(project_id, status)          -- composite: the Kanban board's hottest query
task_labels(label_id)
task_comments(task_id)
task_attachments(task_id)
task_history(task_id)
project_members(project_id)
project_members(user_id)
notifications(user_id, created_at DESC)  -- partial: WHERE is_read = false
refresh_tokens(user_id)
refresh_tokens(expires_at)         -- partial: WHERE revoked_at IS NULL, for cleanup sweeps
```

The `tasks(project_id, status)` composite index was verified with `EXPLAIN` to actually be
selected by the planner for the board's per-project, per-status query, rather than
combining two single-column indexes.

## Dapper connection factory

`IDbConnectionFactory` (`ZigZag.Application/Common/Interfaces/`) is implemented by
`NpgsqlConnectionFactory` (`ZigZag.Infrastructure/Persistence/`), reading the
`ConnectionStrings:ZigZagDb` configuration value. Registered scoped via
`AddInfrastructure()`, so a request that touches multiple repositories can share one
connection within a unit of work.
