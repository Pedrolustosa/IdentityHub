# IdentityHub

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-18-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.5-3178C6?logo=typescript&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-ORM-6DB33F)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)
![License](https://img.shields.io/badge/License-Public-informational)

IdentityHub is an Identity and Access Management (IAM) platform focused on secure user administration and operational visibility.

Backend: ASP.NET Core + Identity + EF Core (SQLite).
Frontend: Angular standalone + Tailwind.

## 1. Objectives

- Provide a complete account lifecycle: registration, email confirmation, password recovery, profile updates, and password change.
- Offer robust administration of users, roles, and permission claims.
- Enforce fine-grained authorization using policy-permission mapping.
- Strengthen session security with token/session validation and permission versioning.
- Expose security observability through audit logs, alerts, and activity timelines.

## 2. Repository Structure

Top-level:

- `IdentityHubServer/`: .NET backend solution and projects.
- `IdentityHubClient/IdentityHub.APP/`: Angular frontend application.
- `README.md`: complete project documentation.

Backend projects (`IdentityHubServer`):

- `IdentityHub.API`: controllers, middleware, authentication, authorization, Swagger, rate limiting.
- `IdentityHub.Application`: application services, CQRS handlers, DTOs, contracts.
- `IdentityHub.Domain`: entities, domain constants, interfaces.
- `IdentityHub.Infrastructure`: EF Core data access, repositories, migrations, security and infrastructure services.
- `IdentityHub.IoC`: dependency injection composition.
- `IdentityHub.API.Tests`: integration, authorization, and unit tests.

## 3. Architecture

### 3.1 Backend architecture

The backend follows layered architecture:

- API layer orchestrates HTTP concerns and middleware pipeline.
- Application layer holds use cases and business orchestration.
- Domain layer defines the business model and rules.
- Infrastructure layer implements persistence and external integrations.

### 3.2 Backend request flow

1. ASP.NET middleware pipeline receives request.
2. JWT is validated.
3. Session and permission version are validated against the database.
4. Permission policy is evaluated through claim-based authorization.
5. Controller delegates to application services/CQRS handlers.
6. Infrastructure persists/retrieves data via EF Core.

### 3.3 Frontend architecture

The frontend uses feature-oriented modularization with Angular standalone components.

Layout zones:

- `auth-layout`: public/authentication shell (`/login`, `/register`, `/forgot-password`, and related auth pages).
- `main-layout`: authenticated shell under `/app`.

## 4. Core Rules

### 4.1 Authentication

- Access token is JWT-based and short-lived.
- Refresh token is stored as `ih_refresh` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`).
- Refresh token is rotated on refresh requests.

### 4.2 Authorization

- Policies are dynamically mapped to permission names (for example, `Users.View`).
- Effective permissions are provided as `permission` claims (primarily role-based).
- Frontend route access is enforced by permission guards and navigation catalog rules.

### 4.3 Session and token hardening

- JWT includes `sid` (session id).
- JWT includes `permission_version`.
- API validates active session state and permission version on authenticated requests.
- Permission updates invalidate previously issued tokens by version increment.

### 4.4 Abuse protection

Rate limiting is applied to sensitive auth endpoints:

- Login.
- Forgot password.
- Resend confirmation.

### 4.5 Data and environment

- API applies pending migrations automatically on startup, except in `Testing` environment.
- Development/test seed is idempotent and only runs in non-production contexts.
- Sensitive values (JWT key, SMTP credentials) must be supplied through User Secrets or environment variables.

## 5. Backend Details

### 5.1 Configuration (`IdentityHub.API/appsettings.json`)

| Section | Purpose |
|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQLite connection (`Data Source=identityhub.db`). |
| `Jwt` | Signing key, issuer, audience, access token lifetime (`ExpireMinutes`, default 15). |
| `Frontend:BaseUrl` | Public SPA base URL used when generating links for user-facing flows. |
| `Smtp` | Outbound email settings for confirmation/reset flows. |
| `RateLimiting:Auth:*` | Optional per-endpoint auth throttling settings. |

Recommended local secrets setup (from `IdentityHubServer/IdentityHub.API`):

```bash
dotnet user-secrets set "Jwt:Key" "your-long-random-jwt-key"
dotnet user-secrets set "Smtp:Username" "your-smtp-user"
dotnet user-secrets set "Smtp:Password" "your-smtp-password"
dotnet user-secrets set "Smtp:From" "no-reply@your-domain.com"
```

### 5.2 Permission model

Permission domains include:

- `Users.*`
- `Roles.*`
- `Dashboard.View`
- `Sessions.*` and `Activity.View`
- `Audit.View`
- `SecurityEvents.*`
- `SecuritySettings.*`
- `Permissions.Catalog.View`, `Permissions.Matrix.View`
- `UserInvites.*`

Compatibility note:

- `Users.Invites.View` remains as legacy backend constant.
- Frontend navigation and access catalogs use `UserInvites.View`.

### 5.3 Access matrix by endpoint

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

### 5.4 Database and seed

- Database: SQLite with migrations under `IdentityHub.Infrastructure/Migrations`.
- Startup migration: `Database.MigrateAsync()` in all environments except `Testing`.
- Seed: roles `Admin`, `Manager`, `User` and development users.
- Seeding behavior: idempotent, only in Development and Testing.

Seeded development users:

| Email | Password | Role |
|-------|----------|------|
| `admin@identityhub.com` | `Admin@123` | Admin |
| `manager@identityhub.com` | `Manager@123` | Manager |
| `user@identityhub.com` | `User@123` | User |

## 6. Frontend Details

### 6.1 Stack

| Area | Technology |
|------|------------|
| Framework | Angular 18 (standalone, router, forms, `HttpClient`). |
| UI | Tailwind CSS 3.4, PostCSS, Autoprefixer. |
| Feedback | ngx-toastr 18. |
| SSR (optional) | `@angular/ssr` + Express. |
| Tests | Karma + Jasmine. |
| Language | TypeScript ~5.5. |

### 6.2 Frontend code layout

| Area | Location |
|------|----------|
| Authenticated shell | `src/app/layouts/main-layout/` |
| Public shell | `src/app/layouts/auth-layout/` |
| Features | `src/app/features/` |
| Shared chrome/components | `src/app/shared/components/` |
| Routing | `src/app/app.routes.ts` and `src/app/features/auth/auth.routes.ts` |
| Core services/guards/interceptors | `src/app/core/` |
| UI error mapping/state | `src/app/shared/http/ui-load-error.ts` and `src/app/shared/components/ux-state/` |
| Permission and navigation catalogs | `src/app/shared/constants/` |

### 6.3 Frontend routes by access

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

### 6.4 Profile and password UX

- Full name is editable; email is read-only in profile UI.
- Password form includes local validation and strength feedback.
- API remains source of truth for final password policy enforcement.

## 7. Build, Run, and Test

### 7.1 Quick start (local)

Prerequisites: .NET 10 SDK, Node.js + npm compatible with Angular 18.

1. Start API:

```bash
cd IdentityHubServer/IdentityHub.API
dotnet run --launch-profile https
```

2. Start SPA:

```bash
cd IdentityHubClient/IdentityHub.APP
npm install
npm start
```

3. Open browser:

- API: `https://localhost:7039`
- Swagger: `https://localhost:7039/swagger`
- SPA: `http://localhost:4200`

### 7.2 Backend commands

```bash
cd IdentityHubServer
dotnet tool restore
dotnet build IdentityHub.slnx
dotnet test IdentityHub.API.Tests
```

EF migration examples:

```bash
dotnet ef migrations add <Name> --project IdentityHub.Infrastructure --startup-project IdentityHub.API
dotnet ef database update --project IdentityHub.Infrastructure --startup-project IdentityHub.API
```

### 7.3 Frontend commands

```bash
cd IdentityHubClient/IdentityHub.APP
npm install
npm run build
npm test
```

SSR serve command:

```bash
npm run serve:ssr:IdentityHub.APP
```

## 8. Current Status and Known Gaps

### 8.1 Current alignment

The current codebase aligns with this README for:

- Layered backend architecture and modular frontend structure.
- Dynamic permission-policy authorization.
- Session hardening with `sid` and `permission_version`.
- Environment-specific migration and seed behavior.

### 8.2 Known API/frontend contract gaps

These route contracts are currently inconsistent and should be fixed:

1. Current user session history
- Frontend call: `GET /api/auth/sessions/history`
- Backend route: `GET /api/auth/sessions/recent`

2. Admin user session history
- Frontend call: `GET /api/users/{id}/sessions/history`
- Backend route: `GET /api/users/{id}/sessions`

3. User audit history
- Frontend call: `GET /api/users/{id}/audit`
- Backend route: `GET /api/users/{id}/audit-logs`

4. Security alert unread count
- Frontend call: `GET /api/security-alerts/unread-count`
- Backend route: not implemented.

Impact:

- Can cause runtime `404 Not Found` in profile, user detail, and top navbar scenarios.

Recommended action:

- Standardize one contract source (prefer backend routes), then update frontend service paths and add route-compatibility tests.

### 8.3 Dependency security snapshot

- Backend build currently raises NuGet advisory warning `NU1903` for transitive SQLite package chain.
- Frontend dependency audit currently reports multiple Angular advisories, including SSR-related critical severity in current version range.

Recommended action:

1. Remediate backend transitive advisory via package updates.
2. Upgrade Angular dependencies to patched compatible versions.
3. Re-run build and test suites, including SSR smoke checks.

## 9. Extension Checklist

When implementing a new feature:

1. Define or extend permission constants in backend and frontend catalogs.
2. Protect backend endpoints with matching policies.
3. Add or update frontend routes with permission metadata.
4. Add navigation entries with `requiredAny` rules.
5. Reuse shared UI components and state patterns.
6. Add tests (authorization, integration, and frontend where applicable).
7. Update this README.

## 10. Security Notes

- Change seeded accounts/passwords before any public deployment.
- Keep JWT and SMTP secrets out of source control.
- Ensure sensitive endpoints remain protected by `[Authorize]` and proper permission policies.

## 11. License

Public/reference use. Adjust license terms to your organization if you fork this project.
