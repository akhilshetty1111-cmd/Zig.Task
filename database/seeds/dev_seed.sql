-- ============================================================================
-- ZigZag - development seed data
-- ============================================================================
-- NOT part of the migration chain - run manually against a local dev database
-- after 001_initial_schema.sql. Idempotent: safe to re-run, it wipes and
-- reloads its own rows first.
--
-- All seeded users share the password "Passw0rd!" - hashed here with BCrypt
-- work factor 11, matching the default used by ZigZag.Infrastructure's
-- IPasswordHasher (Phase 4). This is local-only, throwaway data; never seed
-- a real password hash into anything but a dev database.
-- ============================================================================

BEGIN;

-- Wipe in dependency order (children first) so this script re-runs cleanly.
DELETE FROM notifications;
DELETE FROM refresh_tokens;
DELETE FROM task_history;
DELETE FROM task_attachments;
DELETE FROM task_comments;
DELETE FROM task_labels;
DELETE FROM labels;
DELETE FROM tasks;
DELETE FROM project_members;
DELETE FROM projects;
DELETE FROM users;

-- ----------------------------------------------------------------------------
-- Users
-- ----------------------------------------------------------------------------
INSERT INTO users (id, name, email, password_hash, role) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Amara Okafor',   'amara.admin@zigzag.dev',   '$2a$11$dYG5QWj3dA82NDT0zZiSkevSYGfv39M5Z3w.vU5vKwEDZv1omvo76', 'ADMIN'),
    ('22222222-2222-2222-2222-222222222222', 'Diego Fernandez','diego.pm@zigzag.dev',      '$2a$11$dYG5QWj3dA82NDT0zZiSkevSYGfv39M5Z3w.vU5vKwEDZv1omvo76', 'PROJECT_MANAGER'),
    ('33333333-3333-3333-3333-333333333333', 'Priya Sharma',   'priya@zigzag.dev',         '$2a$11$dYG5QWj3dA82NDT0zZiSkevSYGfv39M5Z3w.vU5vKwEDZv1omvo76', 'MEMBER'),
    ('44444444-4444-4444-4444-444444444444', 'Liam Chen',      'liam@zigzag.dev',          '$2a$11$dYG5QWj3dA82NDT0zZiSkevSYGfv39M5Z3w.vU5vKwEDZv1omvo76', 'MEMBER'),
    ('55555555-5555-5555-5555-555555555555', 'Sofia Rossi',    'sofia@zigzag.dev',         '$2a$11$dYG5QWj3dA82NDT0zZiSkevSYGfv39M5Z3w.vU5vKwEDZv1omvo76', 'MEMBER');

-- ----------------------------------------------------------------------------
-- Projects
-- ----------------------------------------------------------------------------
INSERT INTO projects (id, name, description, owner_id) VALUES
    ('aaaaaaaa-0000-0000-0000-000000000001', 'Website Relaunch',   'Rebuild the marketing site on the new design system.', '22222222-2222-2222-2222-222222222222'),
    ('aaaaaaaa-0000-0000-0000-000000000002', 'Mobile App v2',      'Native rewrite of the customer mobile app.',           '22222222-2222-2222-2222-222222222222');

INSERT INTO project_members (project_id, user_id, role) VALUES
    ('aaaaaaaa-0000-0000-0000-000000000001', '22222222-2222-2222-2222-222222222222', 'OWNER'),
    ('aaaaaaaa-0000-0000-0000-000000000001', '33333333-3333-3333-3333-333333333333', 'MEMBER'),
    ('aaaaaaaa-0000-0000-0000-000000000001', '44444444-4444-4444-4444-444444444444', 'MEMBER'),
    ('aaaaaaaa-0000-0000-0000-000000000002', '22222222-2222-2222-2222-222222222222', 'OWNER'),
    ('aaaaaaaa-0000-0000-0000-000000000002', '55555555-5555-5555-5555-555555555555', 'MANAGER'),
    ('aaaaaaaa-0000-0000-0000-000000000002', '33333333-3333-3333-3333-333333333333', 'MEMBER');

-- ----------------------------------------------------------------------------
-- Labels
-- ----------------------------------------------------------------------------
INSERT INTO labels (id, project_id, name, color) VALUES
    ('bbbbbbbb-0000-0000-0000-000000000001', 'aaaaaaaa-0000-0000-0000-000000000001', 'bug',        '#dc2626'),
    ('bbbbbbbb-0000-0000-0000-000000000002', 'aaaaaaaa-0000-0000-0000-000000000001', 'design',     '#7c3aed'),
    ('bbbbbbbb-0000-0000-0000-000000000003', 'aaaaaaaa-0000-0000-0000-000000000002', 'backend',    '#0891b2'),
    ('bbbbbbbb-0000-0000-0000-000000000004', 'aaaaaaaa-0000-0000-0000-000000000002', 'urgent',     '#ea580c');

-- ----------------------------------------------------------------------------
-- Tasks - spread across every status and priority so the board and dashboard
-- both have something to render.
-- ----------------------------------------------------------------------------
INSERT INTO tasks (id, project_id, title, description, status, priority, assigned_to, created_by, due_date, completed_at) VALUES
    ('cccccccc-0000-0000-0000-000000000001', 'aaaaaaaa-0000-0000-0000-000000000001', 'Set up design tokens',        'Define color, spacing and type scale.', 'DONE',        'MEDIUM', '44444444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222', CURRENT_DATE - 10, now() - interval '3 days'),
    ('cccccccc-0000-0000-0000-000000000002', 'aaaaaaaa-0000-0000-0000-000000000001', 'Hero section responsive fix', 'Layout breaks under 768px.',            'IN_PROGRESS', 'HIGH',   '44444444-4444-4444-4444-444444444444', '33333333-3333-3333-3333-333333333333', CURRENT_DATE + 2,  NULL),
    ('cccccccc-0000-0000-0000-000000000003', 'aaaaaaaa-0000-0000-0000-000000000001', 'Contact form validation',     'Client + server validation needed.',    'IN_REVIEW',   'MEDIUM', '33333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', CURRENT_DATE + 5,  NULL),
    ('cccccccc-0000-0000-0000-000000000004', 'aaaaaaaa-0000-0000-0000-000000000001', 'SEO audit',                   NULL,                                     'TODO',        'LOW',    NULL,                                     '22222222-2222-2222-2222-222222222222', NULL,               NULL),
    ('cccccccc-0000-0000-0000-000000000005', 'aaaaaaaa-0000-0000-0000-000000000001', 'Broken checkout redirect',    'Reported by QA; blocks staging deploy.','BLOCKED',     'URGENT', '44444444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222', CURRENT_DATE - 1,  NULL),
    ('cccccccc-0000-0000-0000-000000000006', 'aaaaaaaa-0000-0000-0000-000000000002', 'Design auth flow',            NULL,                                     'DONE',        'HIGH',   '55555555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222', CURRENT_DATE - 20, now() - interval '10 days'),
    ('cccccccc-0000-0000-0000-000000000007', 'aaaaaaaa-0000-0000-0000-000000000002', 'Implement token refresh',     'Silent refresh on app foreground.',     'IN_PROGRESS', 'HIGH',   '33333333-3333-3333-3333-333333333333', '55555555-5555-5555-5555-555555555555', CURRENT_DATE + 7,  NULL),
    ('cccccccc-0000-0000-0000-000000000008', 'aaaaaaaa-0000-0000-0000-000000000002', 'Offline task cache',          NULL,                                     'TODO',        'MEDIUM', NULL,                                     '55555555-5555-5555-5555-555555555555', CURRENT_DATE + 14, NULL);

INSERT INTO task_labels (task_id, label_id) VALUES
    ('cccccccc-0000-0000-0000-000000000002', 'bbbbbbbb-0000-0000-0000-000000000001'),
    ('cccccccc-0000-0000-0000-000000000001', 'bbbbbbbb-0000-0000-0000-000000000002'),
    ('cccccccc-0000-0000-0000-000000000005', 'bbbbbbbb-0000-0000-0000-000000000004'),
    ('cccccccc-0000-0000-0000-000000000007', 'bbbbbbbb-0000-0000-0000-000000000003');

-- ----------------------------------------------------------------------------
-- Comments
-- ----------------------------------------------------------------------------
INSERT INTO task_comments (task_id, user_id, comment) VALUES
    ('cccccccc-0000-0000-0000-000000000002', '33333333-3333-3333-3333-333333333333', 'Reproduced on iPhone SE viewport - flex-wrap is missing on the hero container.'),
    ('cccccccc-0000-0000-0000-000000000002', '44444444-4444-4444-4444-444444444444', 'Pushed a fix, can someone re-test?'),
    ('cccccccc-0000-0000-0000-000000000005', '22222222-2222-2222-2222-222222222222', 'Blocked on payment gateway sandbox access - following up with vendor.');

-- ----------------------------------------------------------------------------
-- History - a couple of representative status transitions
-- ----------------------------------------------------------------------------
INSERT INTO task_history (task_id, changed_by, field_name, old_value, new_value) VALUES
    ('cccccccc-0000-0000-0000-000000000001', '44444444-4444-4444-4444-444444444444', 'status', 'IN_PROGRESS', 'DONE'),
    ('cccccccc-0000-0000-0000-000000000002', '44444444-4444-4444-4444-444444444444', 'status', 'TODO', 'IN_PROGRESS'),
    ('cccccccc-0000-0000-0000-000000000005', '22222222-2222-2222-2222-222222222222', 'status', 'IN_PROGRESS', 'BLOCKED');

COMMIT;
