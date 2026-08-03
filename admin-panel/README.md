# ReelBox Admin Panel

Isolated Angular administration SPA for ReelBox / Social Media Saver.

## Run locally

```bash
cd admin-panel
npm start
```

API base URL is loaded from `public/config.json` at startup (default local: `http://localhost:5080/api`).

## Production (Cloudflare Pages)

See `docs/DEPLOYMENT.md`.

| Setting | Value |
|---------|-------|
| Root | `admin-panel` |
| Build | `npm ci && npx ng build --configuration=production` |
| Output | `dist/admin-panel/browser` |

Overwrite `config.json` at deploy with your HTTPS API base. Do not commit production URLs.

## Modules

Dashboard, Users, Admin Users, Roles, Audit Logs, Media, Jobs, Platforms, Providers, Storage, Reports, Health, Logs, Settings.

## Auth endpoints

- `POST /api/admin/auth/login|refresh|logout`
- `GET /api/admin/auth/me`

Mobile `/api/v1/auth` is untouched.
