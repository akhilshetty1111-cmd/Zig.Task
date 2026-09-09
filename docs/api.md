# API reference

> **Status:** Phase 3 onward. Endpoints are documented here as they are implemented.
> The live, always-current contract is Swagger at `/swagger` when running locally.

## Conventions

### Base path

All application endpoints are under `/api`. Health probes are the exception:
`/health` (infrastructure) and `/api/health` (reachable via the SPA's API base URL).

### Response envelope

Failures return a consistent shape produced by the global exception middleware:

```json
{
  "success": false,
  "message": "Task not found",
  "errors": [],
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

`traceId` correlates the response with the Serilog entry and the Application Insights trace.

### Status codes

| Code | Used for |
|---|---|
| 200 | Successful read or update returning a body |
| 201 | Resource created; `Location` header points at it |
| 204 | Successful delete, or update with no body |
| 400 | Validation failure — `errors` lists the field messages |
| 401 | Missing, expired or invalid access token |
| 403 | Authenticated but not permitted for this project or resource |
| 404 | Resource does not exist, or the caller may not know that it does |
| 409 | Conflict — duplicate email, stale update |
| 500 | Unhandled failure. No internal detail is exposed in production |

## Implemented

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/health` | none | Liveness probe |
| GET | `/api/health` | none | Liveness probe via the API base path |
| POST | `/api/auth/register` | none | Create an account; auto-issues tokens (no separate login call needed) |
| POST | `/api/auth/login` | none | Same failure message for unknown email, wrong password, or a deactivated account — no user enumeration |
| POST | `/api/auth/refresh` | refresh cookie | Rotates the refresh token; reusing an already-rotated token revokes every active token for that user |
| POST | `/api/auth/logout` | refresh cookie | Idempotent; works even with an expired/missing access token |
| GET | `/api/auth/me` | Bearer token | Current user |

The refresh token is never in a JSON body — it is set as an `HttpOnly`, `Secure`,
`SameSite=None` cookie scoped to `/api/auth` (see
[architecture.md, decision 13](architecture.md#13-refresh-token-cookie-is-samesitenone-not-laxstrict)).
The access token is returned in the response body and sent as `Authorization: Bearer <token>`.

## Planned

Projects, tasks, comments and dashboard endpoints — see the roadmap in the
[README](../README.md#roadmap).
