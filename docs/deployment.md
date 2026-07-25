# HBP — Deployment & Ops

Reference for running the API image on Coolify. Nothing here needs to run on a developer
workstation; the local stack is `docker-compose.yml` at the repository root.

## Image

`src/HBP.Api/Dockerfile` is multi-stage (`sdk:8.0` build → `aspnet:8.0` runtime), runs as the
non-root `app` user and listens on port 8080. Build context is the repository root:

```bash
docker build -f src/HBP.Api/Dockerfile -t hbp-api .
```

## Environment variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__HbpDatabase` | PostgreSQL connection string |
| `Cors__AllowedOrigins__0`, `…__1` | Allowed front-end origins (credentials are always allowed, so wildcards are rejected) |
| `Auth__CookieSecure` | `true` in production; `false` only for plain-HTTP local runs |
| `Media__StorageRoot` | Filesystem root of the media volume (`/data/media` in the image) |
| `Media__BaseUrl` | Public prefix used to build `public_url` values |
| `Smtp__Host`, `Smtp__Port`, `Smtp__Security`, `Smtp__FromAddress`, `Smtp__FromName` | SMTP transport (`Security`: `StartTls`, `SslOnConnect`, `None`) |
| `Smtp__Username`, `Smtp__Password` | SMTP credentials — secrets, set through Coolify only |
| `EmailDispatch__PollIntervalSeconds`, `__BatchSize`, `__MaxAttempts`, `__RetentionDays` | Email worker tuning; retention defaults to 90 days |
| `HBP_SEED_ADMIN_USERNAME`, `HBP_SEED_ADMIN_EMAIL`, `HBP_SEED_ADMIN_PASSWORD` | First-run admin seed; the password must be changed after the first login |
| `Database__SeedOnStartup` | Runs the idempotent seed on boot (default: development only) |
| `RUN_MIGRATIONS_ON_STARTUP` | Applies pending migrations on boot — staging convenience, off in production |
| `HBP_DESIGN_CONNECTION` | Design-time connection string used by `dotnet ef` |

Secrets (`Smtp__Password`, the seed password, the connection string) are supplied as Coolify
environment variables and never committed.

## Applying migrations

Production does **not** auto-migrate. Produce a self-contained bundle and run it as a
pre-deploy command, so the runtime image never needs the SDK:

```bash
dotnet ef migrations bundle --project src/HBP.Infrastructure --startup-project src/HBP.Api --self-contained -r linux-x64 -o artifacts/migrate
```

The bundle reads the connection string from `--connection` or `ConnectionStrings__HbpDatabase`.
Staging may instead set `RUN_MIGRATIONS_ON_STARTUP=true`.

## Health endpoints

- `/health` — liveness, does not touch the database. This is what Coolify and Uptime Kuma poll.
- `/health/ready` — readiness, includes the Npgsql check.

## Backup and restore

Two things carry state: the database and the media volume.

**Backup (Coolify scheduled task, daily, keep at least 7 generations):**

```bash
pg_dump --format=custom --no-owner --no-privileges "$DATABASE_URL" > /backups/hbp-$(date +%F).dump
tar -czf /backups/hbp-media-$(date +%F).tar.gz -C /data media
```

Copy both artefacts off the host — a volume snapshot that lives on the same machine is not a backup.

**Restore:**

1. Stop the API container so no writes race the restore.
2. Recreate an empty database, then `pg_restore --no-owner --no-privileges -d "$DATABASE_URL" hbp-<date>.dump`.
3. Unpack the media archive over the volume: `tar -xzf hbp-media-<date>.tar.gz -C /data`.
4. Start the API and check `/health/ready`, then confirm a public `GET /api/rooms` returns images.

Database and media must be restored from the same date — a media file referenced by a newer
database row would otherwise 404, and `media_files` rows with no file on disk are not repairable
from the application side.
