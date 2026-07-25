# Mobile Project Structure

Social Media Reel Saver — Flutter architecture scaffold.

This document explains every folder under `mobile/`, the dependency rules, configured packages, environment handling, and route map.

---

## Overview

The app uses **feature-first Clean Architecture**:

```
presentation → domain ← data
```

- `presentation` depends on `domain`
- `data` depends on `domain` and implements domain contracts
- `domain` has no Flutter/UI or networking dependencies
- Cross-cutting code lives in `core/` and `shared/`

No business logic is implemented in this scaffold. Folders and stubs exist so features can be built without restructuring.

---

## Top-Level Layout

```text
mobile/
├── assets/                 # Static assets registered in pubspec.yaml
├── lib/                    # Application source
├── test/                   # Widget / unit tests
├── PROJECT_STRUCTURE.md    # This file
└── pubspec.yaml            # Dependencies and asset registration
```

Platform folders (`android/`, `ios/`, etc.) are standard Flutter runners and are unchanged by this architecture phase.

---

## `assets/`

| Folder | Purpose |
|--------|---------|
| `assets/images/` | Product images, illustrations |
| `assets/icons/` | Custom icon assets |
| `assets/fonts/` | Custom font files (when design adds them) |

Each folder contains a `.gitkeep` so empty directories are tracked in git.

---

## `lib/`

### `lib/main.dart`

App entry point. Ensures Flutter bindings, runs `bootstrap()`, then mounts `ProviderScope` + `SocialReelSaverApp`.

### `lib/app/`

| File | Purpose |
|------|---------|
| `app.dart` | Root `ConsumerWidget`: `MaterialApp.router`, themes, `go_router` via Riverpod |
| `bootstrap.dart` | Async startup hooks (empty for now; reserved for secure storage, logging, etc.) |

---

### `lib/core/`

Cross-cutting infrastructure used by all features.

#### `core/config/`

| File | Purpose |
|------|---------|
| `env.dart` | `Env` enum: `dev`, `staging`, `prod` |
| `app_config.dart` | Compile-time config from `--dart-define` (`ENV`, `API_BASE_URL`) |

#### `core/constants/`

| File | Purpose |
|------|---------|
| `api_endpoints.dart` | REST path constants matching SRS §9 |
| `app_constants.dart` | App display name |

#### `core/errors/`

| File | Purpose |
|------|---------|
| `app_exception.dart` | Base exception for infrastructure errors |
| `failure.dart` | Sealed `Failure` types for domain/presentation error handling |

#### `core/network/`

| Path | Purpose |
|------|---------|
| `api_client.dart` | Abstract HTTP client contract (no Dio/http package yet) |
| `interceptors/` | Reserved for auth / logging interceptors |

#### `core/router/`

| File | Purpose |
|------|---------|
| `app_router.dart` | `GoRouter` factory and route table |
| `route_names.dart` | Named route identifiers |
| `route_paths.dart` | Path templates |

#### `core/theme/`

| File | Purpose |
|------|---------|
| `app_colors.dart` | Color token placeholders |
| `app_text_styles.dart` | Typography placeholders |
| `app_theme.dart` | Material 3 light / dark `ThemeData` |

#### `core/utils/`

Reserved for shared pure helpers (date formatting, validators, etc.). Currently empty (`.gitkeep` only).

---

### `lib/features/`

Each feature is a vertical slice with the same three layers:

```text
features/<feature>/
├── data/
│   ├── datasources/     # Remote / local data sources
│   ├── models/          # DTOs / JSON models
│   └── repositories/    # Repository implementations
├── domain/
│   ├── entities/        # Pure domain entities
│   ├── repositories/    # Repository interfaces (contracts)
│   └── usecases/        # Single-purpose use cases
└── presentation/
    ├── providers/       # Riverpod providers / notifiers
    ├── controllers/     # Presentation controllers (auth feature)
    ├── pages/           # Route-level screens
    └── widgets/         # Feature-local widgets
```

#### Features (SRS-aligned)

| Feature | Role |
|---------|------|
| `auth/` | Splash/Login shells, repository contracts, empty providers (Sprint 1 foundation) |
| `home/` | Share-entry handling, manual URL paste, recent/pending cards |
| `library/` | Paginated media library, filters, search/sort |
| `media_detail/` | Playback, metadata, delete |
| `download/` | Progress / status UI for active jobs |
| `settings/` | Account, cache, legal links, preferences |
| `share/` | OS share-sheet / share-intent intake |

Empty layer folders keep a `.gitkeep` until real Dart files are added.

Auth route shells:

- `auth/presentation/pages/splash_page.dart` (app entry)
- `auth/presentation/pages/login_page.dart`

Other route shells (title-only `PlaceholderPage` wrappers — **not** product UI):

- `home/presentation/pages/home_page.dart`
- `library/presentation/pages/library_page.dart`
- `media_detail/presentation/pages/media_detail_page.dart`
- `settings/presentation/pages/settings_page.dart`
- `share/presentation/pages/share_intake_page.dart`

`download/` has no route yet; progress UI is expected as feature widgets/cards under Home/Library during feature work.

---

### `lib/shared/`

Code shared across features without belonging to a single feature.

| Folder / File | Purpose |
|---------------|---------|
| `models/media_status.dart` | SRS §13 media status vocabulary (plain enum) |
| `widgets/placeholder_page.dart` | Minimal scaffold used by route shells |
| `extensions/` | Shared Dart/Flutter extensions (reserved) |

---

### `lib/l10n/`

Reserved for localization (ARB / generated localizations). Empty until i18n is introduced.

---

## Packages (intentionally minimal)

| Package | Why |
|---------|-----|
| `flutter_riverpod` | State management + DI for async polling, auth session, library state |
| `go_router` | Declarative routing, path params, share/deep-link entry |
| `cupertino_icons` | Default Flutter icon set |

**Not installed yet** (deferred until business logic): Dio/http, secure storage, video player, share plugins, SignalR.

---

## Environment Handling

Configuration is compile-time via `--dart-define` (no `.env` package).

| Define | Default | Meaning |
|--------|---------|---------|
| `ENV` | `dev` | `dev` / `staging` / `prod` |
| `API_BASE_URL` | `http://localhost:5000` | Backend base URL |

### Run examples

```bash
# Local development (defaults)
flutter run

# Explicit defines
flutter run \
  --dart-define=ENV=dev \
  --dart-define=API_BASE_URL=http://10.0.2.2:5000

# Staging
flutter run \
  --dart-define=ENV=staging \
  --dart-define=API_BASE_URL=https://staging-api.example.com

# Production build
flutter build apk \
  --dart-define=ENV=prod \
  --dart-define=API_BASE_URL=https://api.example.com
```

Access values through `AppConfig.env` and `AppConfig.apiBaseUrl`.

---

## Route Map

| Path | Name | Page shell |
|------|------|------------|
| `/` | `splash` | `SplashPage` (auth entry; navigation TBD) |
| `/login` | `login` | `LoginPage` |
| `/home` | `home` | `HomePage` |
| `/library` | `library` | `LibraryPage` |
| `/media/:id` | `mediaDetail` | `MediaDetailPage` |
| `/settings` | `settings` | `SettingsPage` |
| `/share` | `share` | `SharePage` (optional `?url=` query) |

Auth redirects are intentionally absent until backend session APIs are wired.

---

## API Endpoint Constants

Defined in `core/constants/api_endpoints.dart` (SRS §9):

| Method (future) | Path helper |
|-----------------|-------------|
| `POST` | `ApiEndpoints.media` |
| `GET` | `ApiEndpoints.media` |
| `GET` | `ApiEndpoints.mediaById(id)` |
| `POST` | `ApiEndpoints.mediaRetry(id)` |
| `DELETE` | `ApiEndpoints.mediaById(id)` |
| `GET` | `ApiEndpoints.mediaPlayback(id)` |

---

## Dependency Rule Summary

1. Features may import `core/` and `shared/`.
2. Features should not import another feature’s `data/` or `presentation/` internals; prefer `shared/` or domain contracts.
3. `domain/` must stay free of Flutter UI and network packages.
4. `presentation/` talks to repositories only through use cases / domain interfaces (via Riverpod providers once implemented).
5. `core/router` may compose feature `presentation/pages` (composition root). Features navigate via `go_router` APIs, not by importing other features’ pages.

---

## What This Scaffold Does Not Include

- Business logic, repository implementations, or API calls
- Designed UI screens or widgets beyond route shells
- Native Android Share Intent / iOS Share Extension wiring
- Backend, worker, or Docker changes

Those belong to later implementation phases on top of this structure.
