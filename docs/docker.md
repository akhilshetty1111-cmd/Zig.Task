# Docker

> **Status:** Phase 12. Local development only - production deployment (Azure) is Phase 14.

## Why

Without Docker, running ZigZag locally means installing the .NET 9 SDK, Node.js and
PostgreSQL yourself, and getting all three configured to talk to each other correctly. With
Docker, `docker compose up` packages each piece - the API, the frontend, PostgreSQL - into
its own isolated, pre-configured container and starts all three wired together. The tradeoff
is a layer of indirection worth understanding, not just running blindly - the rest of this
document is that explanation.

## The three containers

| Service | Image | What it does |
|---|---|---|
| `postgres` | `postgres:17-alpine` (official) | The database. No custom image needed. |
| `api` | Built from [`docker/backend.Dockerfile`](../docker/backend.Dockerfile) | Compiles and runs the ASP.NET Core API. |
| `web` | Built from [`docker/frontend.Dockerfile`](../docker/frontend.Dockerfile) | Compiles the React app to static files and serves them via nginx. |

`docker-compose.yml` at the repo root defines all three and how they're wired together.

## Two networks, not one - the concept that actually matters here

Docker Compose creates a private network where the three containers can reach each other by
**service name** (`postgres`, `api`, `web`) via Docker's internal DNS. But your **browser**
runs on your machine, entirely outside that network - it has no idea what `api` or `postgres`
mean as hostnames.

This is why two different URLs point at the same API:

- The **`api` container** connects to the database as `Host=postgres` - container-to-container,
  over the internal network.
- Your **browser** calls the API at `http://localhost:5000` - published out to your machine,
  because the browser is what's making that call, not a container.

Get this backwards (e.g. tell the browser to call `http://api:8080`) and every request just
fails to resolve, with a confusing DNS error in the browser console.

## Multi-stage builds

Both Dockerfiles have two stages: a **build** stage with the full SDK/Node toolchain, and a
much smaller **runtime** stage with only what's needed to execute the already-built output.
The compiler, npm, and all of `node_modules` never make it into the image that actually
ships - only the compiled `.dll`s (backend) or static `dist/` files (frontend) do. This is
standard practice: it keeps the final image small and reduces its attack surface (nothing
that isn't needed at runtime is present to be exploited).

## The build-time vs. run-time gotcha

The backend reads its configuration (connection string, CORS origins, JWT key) from
**environment variables at container start** - `docker-compose.yml`'s `environment:` block,
same idea as `appsettings.json`. Change them, restart the container, done.

The frontend is different. Vite compiles `import.meta.env.VITE_API_BASE_URL` directly into
the JavaScript bundle **when the image is built** - there is no Node.js process left at
runtime to read an environment variable from; nginx just serves static files. To change the
API URL the frontend calls, the image has to be **rebuilt** (`docker compose build web`),
not just restarted. `docker/frontend.Dockerfile` passes this in as a build `ARG`, set from
`docker-compose.yml`'s `build.args`.

## Why PostgreSQL has no published port

This machine already runs PostgreSQL natively (see [database.md](database.md)), bound to the
standard port 5432. If the `postgres` container also published 5432 to the host, they'd
collide and Compose would fail to start. Since only the `api` container ever needs to reach
this database - never your host machine directly - it simply isn't published. The commented-out
line in `docker-compose.yml` shows how to expose it on a different port (5433) if you ever want
to inspect the containerized database directly with `psql` or pgAdmin.

## Schema and seed data on first run

The official `postgres` image automatically runs every `.sql`/`.sh` file placed in
`/docker-entrypoint-initdb.d/`, in filename order, but **only the very first time** a
container starts against an empty data volume. `docker-compose.yml` mounts the exact same
`001_initial_schema.sql` and `dev_seed.sql` Phase 2 already wrote - one source of truth for
the schema, not a second copy maintained just for Docker.

Because this only runs once, editing those files later and re-running `docker compose up`
does **not** re-apply them against an existing volume. To force a clean re-init:

```powershell
docker compose down -v   # -v also deletes the named volume (zigzag-postgres-data)
docker compose up
```

## Common commands

```powershell
docker compose up              # start everything, logs in the foreground
docker compose up -d           # start everything in the background
docker compose logs -f api     # follow just the API's logs
docker compose down            # stop and remove the containers (data volume survives)
docker compose down -v         # also delete the database volume - full reset
docker compose build           # rebuild images after a Dockerfile change
docker compose up --build      # rebuild + start in one step
```

## Dev secrets in `docker-compose.yml`

The JWT signing key in `docker-compose.yml` is the same dev-only value already committed in
`appsettings.Development.json` (see [architecture.md](architecture.md)) - not a new secret,
just reused so the container needs zero manual setup to start. Neither value is ever used in
production; Phase 14 reads real secrets from Azure Key Vault instead.
