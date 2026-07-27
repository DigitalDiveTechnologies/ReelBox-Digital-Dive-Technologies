# Backend — Social Reel Saver

ASP.NET Core (.NET 9) solution foundation aligned with the SRS technology baseline.

## Solution

`SocialReelSaver.sln`

| Project | Role |
|---------|------|
| `SocialReelSaver.Api` | ASP.NET Core Web API host |
| `SocialReelSaver.Worker` | .NET Worker Service host (downloader later) |
| `SocialReelSaver.Application` | Application use cases / DI |
| `SocialReelSaver.Domain` | Domain entities / contracts |
| `SocialReelSaver.Infrastructure` | PostgreSQL, Redis, storage wiring |
| `SocialReelSaver.Shared` | Shared options / contracts |

## Local run

```bash
# From repo root
docker compose -f docker/docker-compose.yml up postgres redis -d

cd backend
dotnet restore SocialReelSaver.sln
dotnet build SocialReelSaver.sln
dotnet run --project src/SocialReelSaver.Api
```

Health endpoints:

- `GET /health`
- `GET /health/ready` (PostgreSQL + Redis)
- `GET /health/live`

## Docker (full stack)

```bash
docker compose -f docker/docker-compose.yml up --build
```

## Environment

Copy [`backend/.env.example`](.env.example) for the variable names. ASP.NET Core reads them
from the process environment (and `appsettings*.json`); a literal `.env` file is optional.

Override via environment variables (double-underscore nesting), for example:

```text
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__PostgreSQL=Host=...;Database=social_reel_saver;...
ConnectionStrings__Redis=localhost:6379
Database__ConnectionString=...
Redis__ConnectionString=...
ObjectStorage__BucketName=...
```

Database connection is configured in `appsettings.json` / env vars and wired through
`Infrastructure` → EF Core `UseNpgsql`. Schema migrations live under
`Infrastructure/Persistence/Migrations` (apply when running the API against PostgreSQL).

## Out of scope (this sprint)

- Authentication
- Media APIs
- Download / provider business logic
