-- ============================================================================
-- ZigZag - Initial schema
-- ============================================================================
-- Forward-only. Once merged, this file is never edited - a later change is a
-- new numbered migration. See docs/database.md for the numbering convention.
--
-- Enums are stored as `text` constrained by CHECK, not native PostgreSQL enum
-- types or integers. Rationale: docs/architecture.md, decision 9.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto; -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS citext;   -- case-insensitive email uniqueness

-- ----------------------------------------------------------------------------
-- users
-- ----------------------------------------------------------------------------
CREATE TABLE users (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name          text NOT NULL,
    email         citext NOT NULL,
    password_hash text NOT NULL,
    role          text NOT NULL DEFAULT 'MEMBER'
                      CHECK (role IN ('ADMIN', 'PROJECT_MANAGER', 'MEMBER')),
    is_active     boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_users_email UNIQUE (email)
);

COMMENT ON TABLE users IS 'Application accounts. citext email is case-insensitive at the database level, so "a@b.com" and "A@B.com" collide on the unique constraint instead of relying on application code to lowercase consistently.';

-- ----------------------------------------------------------------------------
-- projects
-- ----------------------------------------------------------------------------
CREATE TABLE projects (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name        text NOT NULL,
    description text,
    owner_id    uuid NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    is_archived boolean NOT NULL DEFAULT false,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_projects_name_not_blank CHECK (btrim(name) <> '')
);

COMMENT ON CONSTRAINT projects_owner_id_fkey ON projects IS 'ON DELETE RESTRICT: a user who owns a project cannot be deleted until ownership is transferred, so projects are never silently orphaned.';

-- ----------------------------------------------------------------------------
-- project_members
-- ----------------------------------------------------------------------------
CREATE TABLE project_members (
    project_id uuid NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    user_id    uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    role       text NOT NULL DEFAULT 'MEMBER'
                   CHECK (role IN ('OWNER', 'MANAGER', 'MEMBER', 'VIEWER')),
    joined_at  timestamptz NOT NULL DEFAULT now(),

    PRIMARY KEY (project_id, user_id)
);

COMMENT ON TABLE project_members IS 'A user''s role within one specific project. Distinct from users.role, which is the system-wide role - see docs/architecture.md.';

-- ----------------------------------------------------------------------------
-- labels
-- ----------------------------------------------------------------------------
CREATE TABLE labels (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id uuid NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    name       text NOT NULL,
    color      text NOT NULL DEFAULT '#6366f1',
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT uq_labels_project_name UNIQUE (project_id, name),
    CONSTRAINT ck_labels_color_hex CHECK (color ~ '^#[0-9a-fA-F]{6}$')
);

-- ----------------------------------------------------------------------------
-- tasks
-- ----------------------------------------------------------------------------
-- CLR type is TaskItem / TaskItemStatus, not Task / TaskStatus - see
-- docs/architecture.md, decision 7. The table and column names below stay
-- unabbreviated ("tasks", "status") since SQL has no such collision.
CREATE TABLE tasks (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id   uuid NOT NULL REFERENCES projects (id) ON DELETE CASCADE,
    title        text NOT NULL,
    description  text,
    status       text NOT NULL DEFAULT 'TODO'
                     CHECK (status IN ('TODO', 'IN_PROGRESS', 'IN_REVIEW', 'DONE', 'BLOCKED')),
    priority     text NOT NULL DEFAULT 'MEDIUM'
                     CHECK (priority IN ('LOW', 'MEDIUM', 'HIGH', 'URGENT')),
    assigned_to  uuid REFERENCES users (id) ON DELETE SET NULL,
    created_by   uuid NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    due_date     date,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,

    CONSTRAINT ck_tasks_title_not_blank CHECK (btrim(title) <> ''),
    -- Keeps status and completed_at from drifting apart at the database level
    -- rather than trusting every code path that touches status to also set it.
    CONSTRAINT ck_tasks_completed_at_matches_status
        CHECK ((status = 'DONE') = (completed_at IS NOT NULL))
);

COMMENT ON CONSTRAINT tasks_assigned_to_fkey ON tasks IS 'ON DELETE SET NULL: a task survives its assignee being removed - it becomes unassigned rather than being deleted.';

-- ----------------------------------------------------------------------------
-- task_labels (many-to-many)
-- ----------------------------------------------------------------------------
CREATE TABLE task_labels (
    task_id  uuid NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
    label_id uuid NOT NULL REFERENCES labels (id) ON DELETE CASCADE,

    PRIMARY KEY (task_id, label_id)
);

-- ----------------------------------------------------------------------------
-- task_comments
-- ----------------------------------------------------------------------------
CREATE TABLE task_comments (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id    uuid NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
    user_id    uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    comment    text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_task_comments_not_blank CHECK (btrim(comment) <> '')
);

-- ----------------------------------------------------------------------------
-- task_attachments
-- ----------------------------------------------------------------------------
CREATE TABLE task_attachments (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id       uuid NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
    uploaded_by   uuid NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    file_name     text NOT NULL,
    file_url      text NOT NULL,
    file_size_bytes bigint NOT NULL CHECK (file_size_bytes > 0),
    content_type  text NOT NULL,
    created_at    timestamptz NOT NULL DEFAULT now()
);

-- ----------------------------------------------------------------------------
-- task_history
-- ----------------------------------------------------------------------------
CREATE TABLE task_history (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id    uuid NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
    changed_by uuid NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    field_name text NOT NULL,
    old_value  text,
    new_value  text,
    changed_at timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE task_history IS 'Append-only audit trail. Rows are written by command handlers alongside the mutation, in the same transaction - see docs/architecture.md CQRS section. Never updated or deleted.';

-- ----------------------------------------------------------------------------
-- notifications
-- ----------------------------------------------------------------------------
CREATE TABLE notifications (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    type       text NOT NULL,
    title      text NOT NULL,
    message    text,
    task_id    uuid REFERENCES tasks (id) ON DELETE CASCADE,
    project_id uuid REFERENCES projects (id) ON DELETE CASCADE,
    is_read    boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

-- ----------------------------------------------------------------------------
-- refresh_tokens
-- ----------------------------------------------------------------------------
CREATE TABLE refresh_tokens (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    -- SHA-256 hash of the token, never the raw value - a stolen database
    -- backup must not hand out usable refresh tokens.
    token_hash   text NOT NULL,
    expires_at   timestamptz NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    revoked_at   timestamptz,
    replaced_by_token_hash text,

    CONSTRAINT uq_refresh_tokens_token_hash UNIQUE (token_hash)
);

COMMENT ON TABLE refresh_tokens IS 'replaced_by_token_hash implements rotation-with-reuse-detection: if a hash that was already replaced is presented again, every token in that family is revoked - see Phase 4 auth design.';

-- ============================================================================
-- Indexes
-- ============================================================================
-- Primary/unique-key indexes above are automatic. These cover the query
-- patterns from docs/api.md (board filtering, dashboard aggregates, comment
-- and history loads) - see docs/database.md.

CREATE INDEX ix_projects_owner_id        ON projects (owner_id) WHERE is_archived = false;

CREATE INDEX ix_project_members_project_id ON project_members (project_id);
CREATE INDEX ix_project_members_user_id    ON project_members (user_id);

CREATE INDEX ix_tasks_project_id  ON tasks (project_id);
CREATE INDEX ix_tasks_assigned_to ON tasks (assigned_to);
CREATE INDEX ix_tasks_status      ON tasks (status);
CREATE INDEX ix_tasks_priority    ON tasks (priority);
CREATE INDEX ix_tasks_due_date    ON tasks (due_date) WHERE due_date IS NOT NULL;
-- Serves the Kanban board's single most common query: one project's tasks
-- grouped by column. A composite index here beats the API planner combining
-- two single-column indexes for this specific, very hot access path.
CREATE INDEX ix_tasks_project_status ON tasks (project_id, status);

CREATE INDEX ix_task_labels_label_id  ON task_labels (label_id);

CREATE INDEX ix_task_comments_task_id ON task_comments (task_id);

CREATE INDEX ix_task_attachments_task_id ON task_attachments (task_id);

CREATE INDEX ix_task_history_task_id ON task_history (task_id);

CREATE INDEX ix_notifications_user_unread ON notifications (user_id, created_at DESC)
    WHERE is_read = false;

CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
-- Sweeps expired/revoked rows on a schedule; partial index keeps it cheap by
-- excluding the (larger, irrelevant) set of already-cleaned-up tokens.
CREATE INDEX ix_refresh_tokens_expires_at ON refresh_tokens (expires_at)
    WHERE revoked_at IS NULL;

-- ============================================================================
-- updated_at maintenance
-- ============================================================================
-- One trigger function, attached per table, instead of every command handler
-- remembering to set updated_at itself.

CREATE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_projects_updated_at
    BEFORE UPDATE ON projects
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_tasks_updated_at
    BEFORE UPDATE ON tasks
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_task_comments_updated_at
    BEFORE UPDATE ON task_comments
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
