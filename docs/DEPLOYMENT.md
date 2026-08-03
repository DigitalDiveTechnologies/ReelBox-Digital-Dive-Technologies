# ReelBox — Production Deployment Guide

## Targets

| Layer | Host |
|-------|------|
| Angular Admin | Cloudflare Pages |
| ASP.NET Core API | SmarterASP.NET |
| Database | Neon PostgreSQL |
| Mobile | Flutter (already production) |

Do **not** hardcode production URLs or secrets in git. Use hosting env vars and deploy-time `config.json`.

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

## 2. SmarterASP.NET (API)

1. Publish:
   ```bash
   dotnet publish src/SocialReelSaver.Api/SocialReelSaver.Api.csproj -c Release -o ./publish
   ```
2. Upload `publish` contents to the site.
3. Set **Application Settings / Environment Variables** from `backend/.env.example` (real values).
4. Ensure `ASPNETCORE_ENVIRONMENT=Production`.
5. Forwarded headers are enabled for `X-Forwarded-Proto` behind the host proxy.
6. Hit `GET /health` then `GET /health/ready`.

### Minimum production secrets

- `Database__ConnectionString` / `ConnectionStrings__PostgreSQL`
- `Jwt__SigningKey`
- `AdminJwt__SigningKey` (different from mobile)
- `RapidApi__ApiKey`
- `Cors__AdminOrigins__0` = Cloudflare Pages HTTPS origin
- `ObjectStorage__PublicApiBaseUrl` = public HTTPS API base

Optional first admin (only if `admin_users` empty):

- `AdminBootstrap__Email`
- `AdminBootstrap__Password`

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

- [ ] Rotate Neon password, RapidAPI key, mobile + Admin JWT keys (any previously committed values are compromised)
- [ ] Confirm `appsettings.json` / `Production` contain **empty** secrets
- [ ] Confirm Angular never receives provider secrets (booleans only)
- [ ] Confirm CORS origins are HTTPS Pages hosts only
- [ ] Confirm Admin vs mobile JWT audiences differ

---

## 5. Local development (after secret scrub)

1. Copy `appsettings.Development.local.json.example` → `appsettings.Development.local.json` (gitignored)
2. Or: `dotnet user-secrets set ...` (`UserSecretsId=social-reel-saver-api`)
3. Or: load vars from `.env.example` into your shell
4. Admin UI: `admin-panel/public/config.json` points at `http://localhost:5080/api`

---

## 6. Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Admin login CORS error | `Cors__AdminOrigins__*` missing Pages origin |
| Admin API 401 after deploy | Wrong/missing `AdminJwt__SigningKey` |
| API fails to start | Empty JWT signing key (fail-closed) or missing DB string |
| Pages deep link 404 | `_redirects` not published |
| Admin calls localhost | Forgot deploy-time `config.json` overwrite |
| Downloads fail | Missing `RapidApi__ApiKey` or platform disabled |
| Jobs vanish after recycle | In-memory queue; set Redis + `Worker__UseInMemoryQueue=false` when ready |

See also `docs/ADMIN_PANEL.md` for module/API reference.
