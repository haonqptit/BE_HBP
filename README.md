# BE_HBP

Backend API for the Hotel Booking Portal, built with ASP.NET Core 8 and PostgreSQL 16.

## Local development

```powershell
dotnet tool restore
dotnet restore src/HBP.Api/HBP.Api.csproj
dotnet run --project src/HBP.Api/HBP.Api.csproj
```

The development PostgreSQL container uses database/user `hbp` on port `5432`.

## Projects

| Project | Contains |
|---|---|
| `HBP.Domain` | Entities and enums mapped to `docs/schema.sql` |
| `HBP.Application` | DTOs, validators, service interfaces and abstractions (no ASP.NET dependency) |
| `HBP.Infrastructure` | EF Core persistence, migrations, and the implementations of those abstractions |
| `HBP.Api` | Controllers, middleware and the embedded email dispatch worker |

## API surface

Public (no auth, bilingual through `Accept-Language` or `?lang=vi|ja`):
`GET /api/rooms`, `/api/rooms/{slug}`, `/api/services`, `/api/services/{slug}`, `/api/gallery`,
`/api/amenities`, plus `POST /api/booking-requests` and `POST /api/contact-requests`.

Admin (cookie session, CSRF header `X-HBP-CSRF` on every mutation) under `/api/admin`:
`auth/*`, `media`, `rooms` (with `PUT rooms/{id}/amenities` and `PUT rooms/{id}/media`),
`amenities`, `services`, `gallery/categories`, `gallery/items`, `booking-requests`,
`contact-requests`, `settings`, `dashboard`. List endpoints accept
`?page=&pageSize=&search=&sort=` and return a `PagedResult`.

Health: `/health` (liveness) and `/health/ready` (includes PostgreSQL).

## Further reading

- `docs/migration-verification.md` — how the migrations are checked against `docs/schema.sql`
- `docs/deployment.md` — image, environment variables, migrations, backup and restore
