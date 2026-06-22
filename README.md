# IdentityHub

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-18-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.5-3178C6?logo=typescript&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-ORM-6DB33F)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)
![License](https://img.shields.io/badge/License-Public-informational)

User management platform with an admin panel: accounts, roles, per-role permissions (`permission` claims), and dashboard metrics. Backend: **ASP.NET Core** + **Identity**. Frontend: **Angular** (standalone, Tailwind).

---

## Quick start (local)

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js + npm (compatible with Angular 18).

1. **API** (HTTPS profile, matches the URL used by the Angular app):

   ```bash
   cd IdentityHubServer/IdentityHub.API
   dotnet run --launch-profile https
   ```

   API at **`https://localhost:7039`**. Apply EF migrations when needed (from the API or Infrastructure project, following your usual `dotnet ef` workflow).

2. **SPA:**

   ```bash
   cd IdentityHubClient/IdentityHub.APP
   npm install
   npm start
   ```

   App at **`http://localhost:4200`**.

3. Open the browser, sign in with a seeded account (see table below), or use **Register** when signed out.

**Swagger:** while the API is running, open the Swagger UI on the same origin (e.g. `https://localhost:7039/swagger`) to inspect the contract and try authenticated calls with a JWT.

---

## Repository layout

| Path | Description |
|------|-------------|
| **`IdentityHubServer/`** | Layered .NET 10 solution: Web API, application, domain, infrastructure (EF Core + SQLite), IoC. Startup project: **`IdentityHub.API`**. |
| **`IdentityHubClient/IdentityHub.APP/`** | Angular 18 (standalone components), Tailwind, ngx-toastr, HTTP + guards; optional **SSR** build (Express). |

### Recent additions

- Dedicated pages for **Audit Log detail** and **Security Alert detail**.
- New modules for **Security Settings**, **User Invites**, **System Sessions**, **Recent Activity**, **Permissions Matrix**, and **Permissions Catalog**.
- Global **breadcrumbs**, explicit **Access Denied** screen, and centralized navigation/access catalogs.
- Permission hardening for session-revocation and invite flows, with explicit route-level permission checks.

---

## Backend — `IdentityHubServer`

### Projects

| Project | Role |
|---------|------|
| **`IdentityHub.API`** | Controllers, JWT, CORS, Swagger, authorization handlers (`PermissionHandler`, `PermissionPolicyProvider`), startup seed when no users exist. |
| **`IdentityHub.Application`** | DTOs, service interfaces, application services (auth, users, roles, dashboard, email, tokens). |
| **`IdentityHub.Domain`** | Entities (`ApplicationUser`, `RefreshToken`, `UserSession`, `SecurityEvent`, …) and repository interfaces. |
| **`IdentityHub.Infrastructure`** | `AppDbContext`, repositories, EF migrations, `UserSeed`. |
| **`IdentityHub.IoC`** | DI registration (`AddInfrastructure`). |

### Configuration (`IdentityHub.API/appsettings.json`)

| Section | Purpose |
|---------|---------|
| **`ConnectionStrings:DefaultConnection`** | SQLite (default `Data Source=identityhub.db` next to the API process). |
| **`Jwt`** | Signing key, issuer, audience, access token lifetime (`ExpireMinutes`, default **15**). |
| **`Frontend:BaseUrl`** | Public SPA base URL used when generating links for email/user-facing flows. |
| **`Smtp`** | Outbound email (account confirmation, password reset, etc.) when configured. |
| **`RateLimiting:Auth:*`** | Optional per-endpoint auth throttling settings (`Login`, `ForgotPassword`, `ResendConfirmation`) with sensible defaults. |

> The **refresh token** is delivered as an **HttpOnly / Secure / SameSite=Strict cookie** (`ih_refresh`); it is not exposed to JavaScript. The SPA keeps only the short-lived **access token** in `localStorage`/`sessionStorage` and refreshes via the cookie with `withCredentials`.

For non-local environments, **do not** commit real secrets; use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), environment variables, or a secret store.

Recommended local setup for secrets (from `IdentityHubServer/IdentityHub.API`):

```bash
dotnet user-secrets set "Jwt:Key" "your-long-random-jwt-key"
dotnet user-secrets set "Smtp:Username" "your-smtp-user"
dotnet user-secrets set "Smtp:Password" "your-smtp-password"
dotnet user-secrets set "Smtp:From" "no-reply@your-domain.com"
```

### Authentication and authorization

- **Authentication:** JWT Bearer; users and passwords via **ASP.NET Core Identity**.
- **Authorization:** dynamic policies aligned with **permissions** (e.g. `Users.View`). `PermissionPolicyProvider` maps the policy name to `PermissionRequirement`; `PermissionHandler` checks user claims of type **`permission`** (typically from roles).
- **Sessions:** the JWT carries `sid` (session id) and `permission_version`. Every authenticated request validates that the session is still active and that the token's `permission_version` matches the user's current value; changing a role's permissions increments `PermissionVersion` for affected users, invalidating their existing tokens.
- **Refresh tokens:** rotated on every use; reusing a revoked refresh token raises a `Security.Alert.RefreshTokenReuse` event and revokes the affected session.
- **Reference permissions** (`IdentityHub.Domain.Constants.AppPermissions`):
   - `Users.View`, `Users.Create`, `Users.Update`, `Users.Delete`, `Users.Roles.Update`
   - `Roles.View`, `Roles.Create`, `Roles.Update`, `Roles.Delete`, `Roles.Permissions.View`, `Roles.Permissions.Update`
   - `Dashboard.View`, `Sessions.View`, `Sessions.Revoke`, `Activity.View`
   - `Audit.View`, `SecurityEvents.View`, `SecurityEvents.Manage`
   - `SecuritySettings.View`, `SecuritySettings.Update`
   - `Permissions.Catalog.View`, `Permissions.Matrix.View`
   - `UserInvites.View`, `UserInvites.Create`, `UserInvites.Cancel`, `UserInvites.Resend`

> Note: `Users.Invites.View` still exists as a legacy permission constant on the server for compatibility. Current frontend routes/navigation use `UserInvites.View`.

### Access matrix by endpoint

| Method & route | Required access |
|----------------|------------------|
| `GET /api/dashboard` | `Dashboard.View` |
| `GET /api/users`, `GET /api/users/{id}` | `Users.View` |
| `POST /api/users` | `Users.Create` |
| `POST /api/users/invite` | `UserInvites.Create` |
| `PUT /api/users/{id}` | `Users.Update` |
| `DELETE /api/users/{id}` | `Users.Delete` |
| `PUT /api/users/{id}/roles` | `Users.Roles.Update` |
| `GET /api/users/{id}/sessions` | `Users.View` |
| `DELETE /api/users/{id}/sessions/{sessionId}` | `Sessions.Revoke` |
| `GET /api/users/{id}/audit-logs` | `Audit.View` |
| `GET /api/roles`, `GET /api/roles/{id}` | `Roles.View` |
| `POST /api/roles` | `Roles.Create` |
| `PUT /api/roles/{id}` | `Roles.Update` |
| `DELETE /api/roles/{id}` | `Roles.Delete` |
| `GET /api/roles/permissions/catalog`, `GET /api/roles/{id}/permissions` | `Roles.Permissions.View` |
| `PUT /api/roles/{id}/permissions` | `Roles.Permissions.Update` |
| `GET /api/role-claims/{roleId}` | `Roles.Permissions.View` |
| `POST /api/role-claims/{roleId}`, `PUT /api/role-claims/{roleId}`, `DELETE /api/role-claims/{roleId}` | `Roles.Permissions.Update` |
| `GET /api/audit-logs`, `GET /api/audit-logs/{id}`, `GET /api/audit-logs/export` | `Audit.View` |
| `GET /api/security-alerts`, `GET /api/security-alerts/{id}` | `SecurityEvents.View` |
| `PUT /api/security-alerts/{id}/status` | `SecurityEvents.Manage` |
| `GET /api/security-settings` | `SecuritySettings.View` |
| `PUT /api/security-settings` | `SecuritySettings.Update` |
| `GET /api/user-invites` | `UserInvites.View` |
| `POST /api/user-invites/{id}/resend` | `UserInvites.Resend` |
| `DELETE /api/user-invites/{id}` | `UserInvites.Cancel` |
| `POST /api/auth/register`, `GET /api/auth/confirm-email`, `POST /api/auth/resend-confirmation`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` | Anonymous |
| `GET /api/auth/me`, `GET /api/auth/sessions`, `GET /api/auth/sessions/recent`, `DELETE /api/auth/sessions/{sessionId}`, `DELETE /api/auth/sessions/others`, `POST /api/auth/logout`, `POST /api/auth/change-password`, `PUT /api/auth/profile` | Authenticated |
| `DELETE /api/auth/sessions/users/{targetUserId}` | `Sessions.Revoke` |

### Access matrix by screen

| Screen (route) | Minimum permission |
|----------------|--------------------|
| `/app/dashboard` | `Dashboard.View` |
| `/app/my-access`, `/app/profile`, `/app/access-denied` | Authenticated |
| `/app/users` | `Users.View` |
| `/app/users/create` | `Users.Create` |
| `/app/users/:id` | `Users.View` |
| `/app/users/:id/edit` | `Users.Update` |
| `/app/roles` | `Roles.View` |
| `/app/roles/:roleId/permissions` | `Roles.Permissions.View` |
| `/app/roles/:roleId/permissions/edit` | `Roles.Permissions.Update` |
| `/app/audit-logs`, `/app/audit-logs/:id` | `Audit.View` |
| `/app/security-alerts`, `/app/security-alerts/:id` | `SecurityEvents.View` |
| `/app/sessions` | `Sessions.View` |
| `/app/activity` | `Activity.View` |
| `/app/security-settings` | `SecuritySettings.View` |
| `/app/user-invites` | `UserInvites.View` |
| `/app/permissions/matrix` | `Permissions.Matrix.View` |
| `/app/permissions/catalog` | `Permissions.Catalog.View` |

### REST API (summary)

Typical local base: **`https://localhost:7039`**. Common prefix: **`/api/...`**.

| Area | Main routes | Notes |
|------|-------------|--------|
| **Auth** | `POST /api/auth/register`, `GET /api/auth/confirm-email`, `POST /api/auth/resend-confirmation`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`, `POST /api/auth/change-password`, `PUT /api/auth/profile`, `GET /api/auth/me`, `GET /api/auth/sessions`, `GET /api/auth/sessions/recent`, `DELETE /api/auth/sessions/{sessionId}`, `DELETE /api/auth/sessions/others`, `DELETE /api/auth/sessions/users/{targetUserId}` | `login` and `refresh` rotate/set the `ih_refresh` HttpOnly cookie. `DELETE /api/auth/sessions/users/{targetUserId}` requires `Sessions.Revoke`. |
| **Users** | `GET/POST /api/users`, `POST /api/users/invite`, `GET/PUT/DELETE /api/users/{id}`, `PUT /api/users/{id}/roles`, `GET /api/users/{id}/sessions`, `DELETE /api/users/{id}/sessions/{sessionId}`, `GET /api/users/{id}/audit-logs` | Supports CRUD, invite creation, role assignments, per-user sessions, and per-user audit history. |
| **Roles** | `GET/POST /api/roles`, `GET/PUT/DELETE /api/roles/{id}`, **`GET/PUT /api/roles/{id}/permissions`** | List of permission strings; `PUT` body `{ "permissions": [ "Users.View", ... ] }`. |
| **Role claims** (alternate) | `GET/POST/PUT/DELETE /api/role-claims/{roleId}` | Same `permission` claim model; the SPA primarily uses **Roles** + `.../permissions`. |
| **Dashboard** | `GET /api/dashboard` | Aggregates (totals, 7-day windows, growth). The Angular app maps the API DTO to the UI model. |
| **Audit Logs** | `GET /api/audit-logs`, `GET /api/audit-logs/{id}`, `GET /api/audit-logs/export` | Filtered paging plus CSV export. |
| **Security Alerts** | `GET /api/security-alerts`, `GET /api/security-alerts/{id}`, `PUT /api/security-alerts/{id}/status` | View requires `SecurityEvents.View`; status updates require `SecurityEvents.Manage`. |
| **Security Settings** | `GET /api/security-settings`, `PUT /api/security-settings` | Managed via CQRS + MediatR and permission-scoped endpoints. |
| **User Invites** | `GET /api/user-invites`, `POST /api/user-invites/{id}/resend`, `DELETE /api/user-invites/{id}` | Invite lifecycle operations with explicit `UserInvites.*` permissions. |

### Database and seed

- **SQLite** + versioned migrations under **`IdentityHub.Infrastructure/Migrations`**. The API applies pending migrations on startup (`Database.MigrateAsync()`) for every environment except `Testing` (integration tests use an in-memory SQLite created via `EnsureCreated`).
- **Seed** (`UserSeed`): roles `Admin`, `Manager`, `User` and development accounts. Seeding only runs in **Development** (and the test environment) and is **idempotent** — default accounts are **never** created automatically in Production.

#### EF Core migration commands

Package Manager Console:

```powershell
Add-Migration <Name> -StartupProject IdentityHub.API -Project IdentityHub.Infrastructure
Update-Database -StartupProject IdentityHub.API -Project IdentityHub.Infrastructure
```

.NET CLI (from `IdentityHubServer`):

```bash
dotnet ef migrations add <Name> --project IdentityHub.Infrastructure --startup-project IdentityHub.API
dotnet ef database update --project IdentityHub.Infrastructure --startup-project IdentityHub.API
```

> The running API applies pending migrations automatically on startup, so a manual `database update` is only needed for tooling or CI scenarios.

| Email | Password | Role |
|-------|----------|------|
| `admin@identityhub.com` | `Admin@123` | Admin |
| `manager@identityhub.com` | `Manager@123` | Manager |
| `user@identityhub.com` | `User@123` | User |

### Run the API only

```bash
cd IdentityHubServer/IdentityHub.API
dotnet run --launch-profile https   # https://localhost:7039 (+ http://localhost:5081)
# or
dotnet run --launch-profile http    # http://localhost:5081
```

**CORS** (`Program.cs`): allows `http://localhost:4200` and `https://localhost:4200` for local Angular development.

### Build and test commands

Server (`IdentityHubServer`):

```bash
cd IdentityHubServer
dotnet tool restore
dotnet build IdentityHub.slnx
dotnet test IdentityHub.API.Tests
```

Client (`IdentityHubClient/IdentityHub.APP`):

```bash
cd IdentityHubClient/IdentityHub.APP
npm install
npm run build
npm test
```

> Note: `IdentityHub.slnx` currently includes API/application/domain/infrastructure/IoC projects. The API test project is executed separately via `dotnet test IdentityHub.API.Tests`.

---

## Frontend — `IdentityHubClient/IdentityHub.APP`

### Stack

| Area | Technology |
|------|------------|
| Framework | Angular **18** (standalone, router, forms, `HttpClient`). |
| UI | **Tailwind CSS** 3.4, PostCSS, Autoprefixer. |
| Feedback | **ngx-toastr** 18. |
| SSR (optional) | **@angular/ssr**, Express — `npm run serve:ssr:IdentityHub.APP` after `ng build`. |
| Tests | Karma + Jasmine. |
| Language | TypeScript ~5.5. |

### Code layout

| Area | Location |
|------|----------|
| Authenticated shell | `src/app/layouts/main-layout/` |
| Public shell | `src/app/layouts/auth-layout/` |
| Pages | `src/app/features/` — auth, dashboard, users, role-claims, audit-logs, security-alerts, sessions, activity, security-settings, user-invites, permissions, profile, my-access, access-denied |
| Shared chrome | `src/app/shared/components/` (sidebar, top-navbar, breadcrumbs, ux-state) |
| Routing | `src/app/app.routes.ts` — `/app` uses `authGuard`; public routes use `guestGuard` where applicable |
| HTTP / core | `src/app/core/services/` and `src/app/core/interceptors/`; feature services under `features/**` |
| Shared UI (errors/loading/empty) | `src/app/shared/http/ui-load-error.ts` + `src/app/shared/components/ux-state/` |
| Known permissions (UI) | `src/app/shared/constants/permissions-catalog.ts` — aligned with server (checkboxes on role-claims edit) |
| Navigation/access catalogs | `src/app/shared/constants/navigation-catalog.ts` |
| Guards / interceptor | `src/app/core/guards/`, `src/app/core/interceptors/auth.interceptor.ts` (adds `Authorization: Bearer` from the stored access token) and `auth-refresh.interceptor.ts` (single retry per 401: refreshes via the HttpOnly cookie, then replays the request; on failure clears the session and redirects to login) |

### API integration

Services use `environment.apiUrl` (default local value: **`https://localhost:7039/api`**, matching `src/environments/environment.development.ts`). Keep CORS and origins consistent between SPA and API across environments.

### Profile and password (SPA)

- **Account:** **Full name** is editable; **email** is read-only in the UI (sign-in email is sent unchanged on save for API contract compatibility). Save calls **`PUT /api/auth/profile`** then refreshes the session when possible.
- **Password:** Client-side rules (**7–12** characters, one **uppercase**, **two digits**, one **special** character), confirmation must match, strength **progress bar** and contextual **suggestions**, plus a **help** (?) tooltip on the password card. **`POST /api/auth/change-password`** still enforces server-side Identity rules — align API password options with the SPA if you tighten policy in production.
- **Errors:** Failed loads and form submissions show a shared **load-error banner** (403 / 401 / 404 / network / server / unknown) with **ngx-toastr** as secondary feedback; **Retry** appears only when it is useful (not for 403/401).

### NPM scripts

| Script | Command |
|--------|---------|
| `npm start` | `ng serve` — defaults to `http://localhost:4200`. |
| `npm run build` | Production build → `dist/identity-hub.app`. |
| `npm test` | `ng test` (Karma). |
| `npm run serve:ssr:IdentityHub.APP` | Serve the SSR bundle from `dist/identity-hub.app/server/server.mjs`. |

### Navigation (summary)

- **Public:** login, registration, password and email confirmation flows (see `app.routes.ts`); forms use the same structured error UX as the app shell where applicable.
- **Authenticated (`/app`):** dashboard, users, role claims, profile, my-access, audit logs (list + detail), security alerts (list + detail), system sessions, recent activity, security settings, user invites, permissions matrix/catalog, and access-denied.

---

## Goals and business rules

**Goal:** Centralize identity and access for internal systems — administrators manage users and permissions with a consistent model.

**Core rules:**

- Accounts are created and maintained by authorized processes (per your production API policies).
- Users have one or more **roles**; effective permissions come from **`permission`** claims on roles (and appear in the JWT after login).
- Sensitive operations (role/permission updates, invite lifecycle, session revocation, security alert status changes, security settings updates) require dedicated fine-grained permissions.
- Token flow: access + refresh; logout and state rotation per `AuthService` / API implementation.

**Product features:**

- User CRUD, listing with **roles**, invite creation, admin user edit, role updates, per-user session revocation, and per-user audit history.
- **Self-service profile:** update **full name** via auth API; **change password** revokes sessions server-side.
- Role listing and **per-role permission** editing (via the Roles API).
- Dashboard with aggregate metrics, trends, security/audit widgets, and permission-scoped quick actions.
- Security alerts management, audit logs export/detail, security settings management, user invites management, permissions matrix/catalog, and activity/sessions modules.
- JWT, refresh tokens, and sessions persisted in the API data model.

---

## Security notes

- Change seeded passwords and accounts before any public deployment.
- Keep JWT and SMTP secrets out of source control in production.
- Ensure administrative endpoints are protected with `[Authorize]` and permission policies appropriate to your threat model.

---

## License

Public / reference use — adjust the license to your organization’s legal model if you fork the project.
