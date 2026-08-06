# ReelBox — Production Deployment Guide

## Targets

| Layer | Host |
|-------|------|
| Angular Admin | Cloudflare Pages |
| ASP.NET Core API + Worker | Windows VPS (same machine) |
| Queue | Redis on VPS |
| Downloads | yt-dlp + FFmpeg on VPS |
| Storage | Shared local folder on VPS (or S3/R2) |
| Database | Neon PostgreSQL |
| Mobile | Flutter |

Do **not** hardcode production URLs or secrets in git. Use hosting env vars and deploy-time `config.json`.

**Production download topology (required):**

```
Flutter → API → Redis → SocialReelSaver.Worker (ONLY) → yt-dlp / FFmpeg → Shared Storage → Neon → Library
```

The API must **not** register `MediaDownloadWorker` in Production (`Worker:RunInApiHost=false`).

---

## 1. Cloudflare Pages (Admin)

### Build settings

| Setting | Value |
|---------|-------|
| Root directory | `admin-panel` |
| Build command | `npm ci && npx ng build --configuration=production` |
| Output directory | `dist/admin-panel/browser` |

### Assets (already in `admin-panel/public/`)

- `_redirects` — SPA fallback `/* /index.html 200`
- `_headers` — security + cache headers
- `config.json` — runtime API base (overwrite at deploy)
- `config.example.json` — template without real hosts

### Deploy-time config (required)

After build / in Pages build command, write production config **without committing it**:

```bash
printf '%s\n' '{"apiBaseUrl":"https://YOUR-API-HOST/api","signalRUrl":""}' > dist/admin-panel/browser/config.json
```

Or use a Pages build plugin / wrangler secret step.

### Smoke

1. Open `https://YOUR-PAGES-HOST/auth/login` (direct URL)
2. Refresh `/dashboard` and `/users` (SPA fallback)
3. Login against production API (CORS must allow Pages origin)

---

## 2. Windows VPS — API + Worker checklist

Run **both** processes on the **same** VPS so Local storage and Redis are shared.

### 2.1 Prerequisites

- [ ] .NET 9 runtime (or SDK) installed
- [ ] Redis installed and listening (e.g. `localhost:6379`)
- [ ] `yt-dlp` on PATH (or set `Providers__YtDlpExecutablePath`)
- [ ] `ffmpeg` on PATH (or set `Ffmpeg__ExecutablePath`)
- [ ] Folder for shared media, e.g. `C:\ReelBox\storage` (API + Worker **same** `ObjectStorage__LocalRootPath`)
- [ ] Neon connection string ready
- [ ] Firewall: public HTTPS (or reverse proxy) to API; Redis **not** exposed publicly
- [ ] Admin PowerShell / elevated prompt for `sc.exe` service install

### 2.2 Publish

```powershell
cd backend
dotnet publish src/SocialReelSaver.Api/SocialReelSaver.Api.csproj -c Release -o C:\ReelBox\publish\api
dotnet publish src/SocialReelSaver.Worker/SocialReelSaver.Worker.csproj -c Release -o C:\ReelBox\publish\worker
```

Worker publishes `SocialReelSaver.Worker.exe` (native Windows Service host + Development console).

### 2.3 Secrets (local JSON or env)

Copy examples (gitignored):

- API: `appsettings.Production.local.json.example` → `C:\ReelBox\publish\api\appsettings.Production.local.json`
- Worker: `appsettings.Production.local.json.example` → `C:\ReelBox\publish\worker\appsettings.Production.local.json`

Both must share:

| Setting | Value |
|---------|--------|
| Neon DB | Same connection string |
| Redis | Same, e.g. `localhost:6379` |
| `Worker__UseInMemoryQueue` | `false` |
| `Worker__QueueName` | `media-download-jobs` |
| `ObjectStorage__Provider` | `Local` (same VPS) |
| `ObjectStorage__LocalRootPath` | **Identical** absolute path |
| `ObjectStorage__PlaybackSigningKey` | Same key |
| `ObjectStorage__PublicApiBaseUrl` | Public API base (no trailing `/api`) |
| `Providers__Resolver` | `YtDlp` |

API-only:

| Setting | Value |
|---------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Worker__RunInApiHost` | **`false`** |
| `Jwt__SigningKey` / `AdminJwt__SigningKey` | Set |
| SMTP | As needed for OTP |

Worker-only:

| Setting | Value |
|---------|--------|
| `DOTNET_ENVIRONMENT` | `Production` |
| Hosted consumer | Always on (do **not** run a second Worker instance) |

### 2.4 Register Worker as native Windows Service

`SocialReelSaver.Worker` uses `Microsoft.Extensions.Hosting.WindowsServices` with service name **ReelBox Download Worker**.  
Run elevated PowerShell. Path spaces require careful quoting on `binPath=`.

**Create (Automatic start on boot):**

```powershell
sc.exe create "ReelBox Download Worker" binPath= "\"C:\ReelBox\publish\worker\SocialReelSaver.Worker.exe\"" start= auto DisplayName= "ReelBox Download Worker"
```

Set working directory + Production environment for the service process (system environment or a wrapper). Prefer setting machine/user env, or create the service with an explicit environment via a small `.cmd` if needed. Minimum:

```powershell
[System.Environment]::SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production", "Machine")
```

Or set per-service with the registry / a helper. Ensure `appsettings.Production.local.json` sits next to the EXE.

**Automatic recovery (restart on crash):**

```powershell
sc.exe failure "ReelBox Download Worker" reset= 86400 actions= restart/5000/restart/10000/restart/30000
sc.exe failureflag "ReelBox Download Worker" 1
```

(`reset=` seconds before failure count resets; `actions=` restart after 5s / 10s / 30s.)

**Start / stop / status:**

```powershell
sc.exe start "ReelBox Download Worker"
sc.exe stop "ReelBox Download Worker"
sc.exe query "ReelBox Download Worker"
```

**Update after republish:**

```powershell
sc.exe stop "ReelBox Download Worker"
# overwrite C:\ReelBox\publish\worker\ ...
sc.exe start "ReelBox Download Worker"
```

**Remove (if reinstalling):**

```powershell
sc.exe stop "ReelBox Download Worker"
sc.exe delete "ReelBox Download Worker"
```

**Development (console, not Service):**

```powershell
cd backend
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project src/SocialReelSaver.Worker/SocialReelSaver.Worker.csproj
```

`AddWindowsService` only uses SCM lifetime when Windows starts the process as a service; interactive `dotnet run` stays a normal console host.

**API process:** still publish separately (IIS / Kestrel / NSSM). Keep `Worker__RunInApiHost=false` so only this Windows Service consumes Redis jobs.

### 2.5 Single-consumer verification

1. Start Redis, then **ReelBox Download Worker** service, then **API**.
2. API startup log must contain: `API host will not consume download jobs`.
3. Worker log must contain: `dedicated download consumer`.
4. Confirm **only one** Worker process (`SocialReelSaver.Worker.exe`) is running.
5. Create a media download from the app → Redis list drains via Worker → `media_items.status` → `Completed`.
6. Confirm files appear under the shared `LocalRootPath` and Library playback works.
7. Kill the Worker process once and confirm Service Recovery restarts it within ~5–30s.

Do **not** run API with `Worker__RunInApiHost=true` alongside the Worker service — that would double-consume Redis jobs.

### 2.6 Environment variables (quick reference)

See `backend/.env.example`. Minimum Production set:

```
ASPNETCORE_ENVIRONMENT=Production
DOTNET_ENVIRONMENT=Production
ConnectionStrings__PostgreSQL=...
Database__ConnectionString=...
ConnectionStrings__Redis=localhost:6379
Redis__ConnectionString=localhost:6379
Worker__UseInMemoryQueue=false
Worker__RunInApiHost=false
Worker__QueueName=media-download-jobs
ObjectStorage__Provider=Local
ObjectStorage__LocalRootPath=C:\ReelBox\storage
ObjectStorage__PublicApiBaseUrl=https://YOUR-API-HOST
ObjectStorage__PlaybackSigningKey=...
Jwt__SigningKey=...
AdminJwt__SigningKey=...
Providers__Resolver=YtDlp
Providers__YtDlpExecutablePath=yt-dlp
Ffmpeg__ExecutablePath=ffmpeg
```

---

## 3. Neon PostgreSQL

Apply migrations **before** sending traffic:

```bash
cd backend
dotnet ef database update \
  --project src/SocialReelSaver.Infrastructure/SocialReelSaver.Infrastructure.csproj \
  --startup-project src/SocialReelSaver.Api/SocialReelSaver.Api.csproj
```

Connection string must come from env (not committed JSON).

### Migration chain

1. `InitialCreate` — media
2. `AddUsers` — mobile users
3. `AlignMediaItemsSrsSchema`
4. `AddAdminUsers` — `admin_users`
5. `AddAuditLogs` — `audit_logs`
6. `AddAdminOperationalTables` — `system_settings`, `app_error_logs`

Mobile tables `users` / `media_items` are not redesigned by Admin phases.

### SuperAdmin

- If table empty and bootstrap env set → seeded at API startup
- Or create via Admin Users API after an existing SuperAdmin login

---

## 4. Security checklist

- [ ] Rotate Neon password, JWT keys, SMTP password (any previously exposed values are compromised)
- [ ] Confirm `appsettings.json` / `Production` contain **empty** secrets
- [ ] Confirm Angular never receives provider secrets (booleans only)
- [ ] Confirm CORS origins are HTTPS Pages hosts only
- [ ] Confirm Admin vs mobile JWT audiences differ
- [ ] Redis bound to localhost (or private network) only
- [ ] Only one Worker process consuming the queue (`ReelBox Download Worker` Windows Service)
- [ ] Service recovery configured (`sc.exe failure ... restart`)

---

## 5. Local development (after secret scrub)

1. Copy `appsettings.Development.local.json.example` → `appsettings.Development.local.json` (gitignored)
2. Or: `dotnet user-secrets set ...` (`UserSecretsId=social-reel-saver-api`)
3. Or: load vars from `.env.example` into your shell
4. Admin UI: `admin-panel/public/config.json` points at `http://localhost:5080/api`
5. Development defaults `Worker:RunInApiHost=true` so API can process downloads without a separate Worker process (optional Redis still recommended)

---

## 6. Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Admin login CORS error | `Cors__AdminOrigins__*` missing Pages origin |
| Admin API 401 after deploy | Wrong/missing `AdminJwt__SigningKey` |
| API fails to start | Empty JWT signing key (fail-closed) or missing DB string |
| Pages deep link 404 | `_redirects` not published |
| Admin calls localhost | Forgot deploy-time `config.json` overwrite |
| Downloads stay Queued | Worker not running, Redis down, or wrong queue name |
| Downloads fail | Missing `yt-dlp` / `ffmpeg` on Worker host |
| Playback 404 | API/Worker `LocalRootPath` mismatch |
| Duplicate / racing jobs | API `RunInApiHost=true` **and** Worker both running |
| Worker service won't start | Wrong `binPath`, missing `DOTNET_ENVIRONMENT=Production`, or local JSON missing next to EXE |
| Worker dies and stays down | Recovery not set — run `sc.exe failure "ReelBox Download Worker" ...` |
| Jobs vanish after recycle | In-memory queue; set Redis + `Worker__UseInMemoryQueue=false` |

See also `docs/ADMIN_PANEL.md` for module/API reference.
