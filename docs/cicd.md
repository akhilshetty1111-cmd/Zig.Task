# CI/CD

> **Status:** Phase 13 (CI) and Phase 15 (CD). Workflows land in `.github/workflows/`.

## Branch strategy

```
main         production; protected; deploys to the production environment
  ^
develop      integration; deploys to the development environment
  ^
feature/*    one branch per unit of work
bugfix/*
```

Work flows `feature/* -> develop -> main`. Both `develop` and `main` are protected:
CI must pass before a merge.

## Commit convention

Conventional Commits, so history is scannable and release notes can be generated:

```
feat:  a user-visible capability
fix:   a bug fix
ci:    pipeline changes
docs:  documentation only
test:  tests only
chore: tooling, dependencies, scaffolding
refactor: behavior-preserving change
```

## Pipelines

### Backend CI — on push and pull request

```
checkout -> setup .NET 9 -> restore -> build (Release) -> unit tests -> integration tests
```

Integration tests need Docker; GitHub-hosted Ubuntu runners provide it, so Testcontainers
works without extra setup.

### Frontend CI — on push and pull request

```
checkout -> setup Node 20 -> npm ci -> lint -> typecheck -> test -> build
```

Both workflows use **path filters**, so a frontend-only change does not rebuild the API.

### CD

| Trigger | Deploys to |
|---|---|
| Merge to `develop` | Development environment |
| Merge to `main` | Production environment |

Deployment credentials come from GitHub Secrets and are never written into YAML.
