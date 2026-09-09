# Database

> **Status:** Phase 2. This document is filled in when the schema lands.

PostgreSQL 16+. Schema changes are **forward-only, versioned SQL scripts** —
there is no ORM migration engine, because ZigZag uses Dapper (see
[architecture.md, decision 5](architecture.md#5-dapper-instead-of-ef-core)).

## Conventions

| Rule | Detail |
|---|---|
| Naming | `snake_case` tables and columns, plural table names (`tasks`, `task_comments`) |
| Primary keys | `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` |
| Timestamps | `timestamptz`, never naive `timestamp` |
| Enums | `text` columns constrained by `CHECK`, values in `SCREAMING_SNAKE_CASE` |
| Soft state | `is_active` / `is_archived` booleans rather than deleting rows |
| Migrations | `database/migrations/NNN_description.sql`, applied in filename order, never edited once merged |

## Planned tables

`users`, `projects`, `project_members`, `tasks`, `task_comments`,
`task_attachments`, `labels`, `task_labels`, `task_history`,
`notifications`, `refresh_tokens`.

## Planned indexes

Driven by the actual query patterns (board filtering, dashboard aggregates, comment loads):

```
tasks(project_id)
tasks(assigned_to)
tasks(status)
tasks(priority)
tasks(due_date)
task_comments(task_id)
project_members(project_id)
project_members(user_id)
```
