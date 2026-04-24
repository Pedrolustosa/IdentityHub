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
| **`Jwt`** | Signing key, issuer, audience, access token lifetime. |
| **`Smtp`** | Outbound email (account confirmation, password reset, etc.) when configured. |

For non-local environments, **do not** commit real secrets; use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), environment variables, or a secret store.

### Authentication and authorization

- **Authentication:** JWT Bearer; users and passwords via **ASP.NET Core Identity**.
- **Authorization:** dynamic policies aligned with **permissions** (e.g. `Users.View`). `PermissionPolicyProvider` maps the policy name to `PermissionRequirement`; `PermissionHandler` checks user claims of type **`permission`** (typically from roles).
- **Reference permissions** (`IdentityHub.Domain.Constants.AppPermissions`): `Users.View`, `Users.Create`, `Users.Update`, `Roles.View`, `Roles.Manage` — assigned to roles in **`UserSeed`**.

### REST API (summary)

Typical local base: **`https://localhost:7039`**. Common prefix: **`/api/...`**.

| Area | Main routes | Notes |
|------|-------------|--------|
| **Auth** | `POST /api/auth/register`, `login`, `refresh`, `logout`, `forgot-password`, `reset-password`, `change-password` (policy **`Users.ChangePassword`**), **`PUT /api/auth/profile`** (any authenticated user) | `PUT /api/auth/profile` updates **display name** for the signed-in user; response JSON `{ id, email, fullName }` (camelCase). Email is not modified server-side by this handler. Other routes return text or DTOs per Swagger. |
| **Users** | `GET/POST /api/users`, `GET/PUT/DELETE /api/users/{id}`, **`PUT /api/users/{id}/roles`** | `GET` returns `roles` per user; **`roles`** in the body (JSON camelCase) updates membership. |
| **Roles** | `GET/POST /api/roles`, `GET/PUT/DELETE /api/roles/{id}`, **`GET/PUT /api/roles/{id}/permissions`** | List of permission strings; `PUT` body `{ "permissions": [ "Users.View", ... ] }`. |
| **Role claims** (alternate) | `GET/POST/PUT/DELETE /api/role-claims/{roleId}` | Same `permission` claim model; the SPA primarily uses **Roles** + `.../permissions`. |
| **Dashboard** | `GET /api/dashboard` | Aggregates (totals, 7-day windows, growth). The Angular app maps the API DTO to the UI model. |

### Database and seed

- **SQLite** + migrations under **`IdentityHub.Infrastructure/Migrations`**.
- **Seed** (`UserSeed`): roles `Admin`, `Manager`, `User` and development accounts (**change or disable** for production):

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
| Authenticated shell | `src/app/layout/shell.component.*` |
| Pages | `src/app/features/` — auth (`login`, `register`, forgot/reset password, resend/confirm email), `dashboard`, `users` (list, detail, edit), `role-claims` (list, detail, edit), `profile` |
| Shared chrome | `src/app/layout/` (shell, sidebar, top-navbar) |
| Routing | `src/app/app.routes.ts` — `/app` uses `authGuard`; public routes use `guestGuard` where applicable |
| HTTP / core | `src/app/core/services/` — `auth.service`, guards, token usage; feature services under `features/**` (e.g. `users.service`, `roles.service`, `dashboard.service`) |
| Shared UI (errors) | `src/app/shared/http/ui-load-error.ts` — maps `HttpErrorResponse` to typed UI errors; `src/app/shared/components/load-error-banner/` — banner + optional retry (used on dashboard, users, role-claims, profile forms, auth flows) |
| Known permissions (UI) | `src/app/shared/constants/permissions-catalog.ts` — aligned with server (checkboxes on role-claims edit) |
| Guards / interceptor | `src/app/core/guards/`, `src/app/core/interceptors/auth.interceptor.ts` (`Authorization: Bearer` from `localStorage` or `sessionStorage`) |

### API integration

Services use a fixed development base URL **`https://localhost:7039`** (same as the API HTTPS profile). For other environments, the typical next step is to drive the base URL from **`environment`** files. Keep CORS and origins consistent between the SPA and the API.

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
- **Authenticated (`/app`):** dashboard, users, role claims (view and edit per-role permissions; persistence via **`RolesService`** → `PUT /api/roles/{id}/permissions`), **profile** (display name + password).

---

## Goals and business rules

**Goal:** Centralize identity and access for internal systems — administrators manage users and permissions with a consistent model.

**Core rules:**

- Accounts are created and maintained by authorized processes (per your production API policies).
- Users have one or more **roles**; effective permissions come from **`permission`** claims on roles (and appear in the JWT after login).
- Sensitive operations (role and permission management) should require permissions such as `Roles.Manage` when policies are applied on controllers.
- Token flow: access + refresh; logout and state rotation per `AuthService` / API implementation.

**Product features:**

- User CRUD, listing with **roles**, admin user edit, and **role updates** (`PUT .../users/{id}/roles`).
- **Self-service profile:** update **full name** via auth API; **change password** revokes sessions server-side.
- Role listing and **per-role permission** editing (via the Roles API).
- Dashboard with aggregate metrics.
- JWT, refresh tokens, and sessions persisted in the API data model.

---

## Security notes

- Change seeded passwords and accounts before any public deployment.
- Keep JWT and SMTP secrets out of source control in production.
- Ensure administrative endpoints are protected with `[Authorize]` and permission policies appropriate to your threat model.

---

## License

Public / reference use — adjust the license to your organization’s legal model if you fork the project.
