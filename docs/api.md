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

## Planned

Authentication, projects, tasks, comments and dashboard endpoints — see the
roadmap in the [README](../README.md#roadmap).
