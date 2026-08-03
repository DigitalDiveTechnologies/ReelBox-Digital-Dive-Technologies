# ReelBox Admin Panel — Architecture & Operations

## Architecture overview

```
Flutter mobile  →  /api/v1/*     ─┐
                                  ├→  ASP.NET Core (SmarterASP)  →  Neon PostgreSQL
Angular Admin   →  /api/admin/*  ─┘         │
                                            ├ MediaDownloadWorker (in-process)
                                            └ RapidAPI / Object storage
```

Clean Architecture layers:

| Layer | Responsibility |
|-------|----------------|
| Domain | Entities/enums (`User`, `MediaItem`, `AdminUser`, `AuditLog`, `SystemSetting`, `AppErrorLog`) |
| Application | Use cases, DTOs, abstractions |
| Infrastructure | EF Core, JWT, queue, providers, storage |
| API | Controllers, auth policies, middleware |
| Angular | Admin UI only; runtime config via `/config.json` |

Flutter and `/api/v1/*` contracts are independent of Admin modules.

## Admin modules

Dashboard · Users · Admin Users · Roles · Audit Logs · Media · Download Jobs · Platforms · Providers · Storage · Reports · System Health · Logs · Settings

## Roles

| Role | Notes |
|------|-------|
| SuperAdmin | Full access including settings / provider writes |
| Operations | Ops manage (media/jobs/platforms/storage) |
| Support | Users + media/jobs |
| Technical | Health/logs/providers read |
| Analyst | Reports / read dashboards |

Server policies: `AdminOnly`, `AdminUsers.Manage`, `AdminMedia.Manage`, `AdminPlatforms.Manage`, `AdminSettings.Manage`, plus role policies.

## API endpoints

Prefix: `/api/admin` (all require `AdminBearer` except noted).

Auth: `POST auth/login|refresh|logout`, `GET auth/me`  
Dashboard: `GET dashboard/summary|trends|activity`  
Users: `GET users`, `GET/{id}`, `PATCH/{id}/status`, `POST/{id}/revoke-sessions`  
Admins: `GET|POST admins`, `GET|PATCH/{id}`  
Roles: `GET roles`, `PATCH roles/admins/{id}`  
Audit: `GET audit-logs`, `GET/{id}`  
Media: `GET media`, `GET/{id}`, `DELETE/{id}`, `POST/{id}/retry`, `GET/{id}/playback`  
Jobs: `GET jobs`, `POST/{id}/retry|cancel|requeue`  
Platforms: `GET platforms`, `PATCH/{platform}`  
Providers: `GET providers`, `PATCH/{name}`, `POST/{name}/health-check`  
Storage: `GET storage/summary`, `POST orphan-scan|cleanup`  
Reports: `GET reports/*`, `GET reports/export.csv`  
Health: `GET health/overview`  
Logs: `GET logs`, `GET/{id}`  
Settings: `GET|PUT settings`

Anonymous: `GET /health`, `/health/ready`, `/health/live`

## Environment variables

See `backend/.env.example` and `docs/DEPLOYMENT.md`. Production secrets **must** come from hosting env vars — committed `appsettings.json` / `Production` keep secrets empty.

## Deployment

Follow `docs/DEPLOYMENT.md` for Cloudflare Pages, SmarterASP.NET, and Neon.

## Production checklist

- [ ] Secrets rotated (Neon, JWT ×2, RapidAPI)
- [ ] Env vars set on SmarterASP
- [ ] EF migrations applied
- [ ] SuperAdmin available
- [ ] CORS includes Pages HTTPS origin
- [ ] Pages `config.json` points at HTTPS API `/api`
- [ ] `/health` and Admin login smoke pass
- [ ] Mobile login + download smoke pass (regression)

## Troubleshooting

See `docs/DEPLOYMENT.md` § Troubleshooting.
