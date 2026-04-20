# IdentityHub

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-18-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.5-3178C6?logo=typescript&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-ORM-6DB33F)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)
![License](https://img.shields.io/badge/License-Public-informational)

IdentityHub is a user management platform with an administrative panel focused on creating, editing, and assigning roles to users in a secure and controlled way.

## Repository layout

| Path | Description |
|------|-------------|
| **`IdentityHubServer/`** | .NET 10 ASP.NET Core Web API, layered solution, JWT + permission policies, EF Core + SQLite. Host project: **`IdentityHub.API`**. |
| **`IdentityHubClient/IdentityHub.APP/`** | Angular 18 standalone SPA (Tailwind CSS, ngx-toastr, Karma tests, optional SSR via Express). |

Angular-only details (routes, `src/app` map, CLI): [`IdentityHubClient/IdentityHub.APP/README.md`](IdentityHubClient/IdentityHub.APP/README.md).

---

## Backend — `IdentityHubServer`

### Solution structure

| Project | Responsibility |
|---------|------------------|
| **`IdentityHub.API`** | HTTP API: controllers, JWT/CORS/Swagger wiring, authorization handlers, startup seeding. |
| **`IdentityHub.Application`** | DTOs, application services (e.g. token generation). |
| **`IdentityHub.Domain`** | Domain entities (`ApplicationUser`, sessions, security events, etc.). |
| **`IdentityHub.Infrastructure`** | EF Core `AppDbContext`, SQLite provider, migrations, `UserSeed`. |
| **`IdentityHub.IoC`** | Dependency injection registration for infrastructure and application services. |

### Configuration (`IdentityHub.API/appsettings.json`)

| Section | Purpose |
|---------|---------|
| **`ConnectionStrings:DefaultConnection`** | SQLite file (default `Data Source=identityhub.db` next to the running API). |
| **`Jwt`** | Signing key, issuer, audience, access token lifetime (`ExpireMinutes`, default 60). |

For non-local environments, **do not** ship production secrets in `appsettings.json`; use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), environment variables, or a secret store.

### Authorization model

- **Authentication:** JWT bearer (ASP.NET Core Identity for users/passwords).
- **Authorization:** Dynamic policies named after **permissions** (e.g. `Users.View`). `PermissionPolicyProvider` maps any such policy to a `PermissionRequirement` checked against role claims of type **`permission`**.
- **Seeded permissions** (see `IdentityHub.Infrastructure/Data/Seed/UserSeed.cs`): `Users.View`, `Users.Create`, `Users.Update`, `Users.Delete`, `Roles.View`, `Roles.Manage` — assigned to `Admin`, `Manager`, and `User` roles as defined in seed.

### API overview

The **`IdentityHub.API`** project exposes a REST API. For local development with the HTTPS launch profile, the API base URL is **`https://localhost:7039`**. Feature areas are implemented in controllers for **authentication**, **users**, **roles**, **role permission claims**, and **dashboard** metrics. **Swagger / OpenAPI** is enabled so you can inspect the contract and try authenticated calls from the browser.

### Database & seed users

- EF Core creates/updates the SQLite database on startup (see `Program.cs` migration path).
- **Development seed** (`UserSeed`): roles `Admin`, `Manager`, `User` and sample accounts (change or disable for real deployments):

| Email | Password | Role |
|-------|----------|------|
| `admin@identityhub.com` | `Admin@123` | Admin |
| `manager@identityhub.com` | `Manager@123` | Manager |
| `user@identityhub.com` | `User@123` | User |

### Run the API

```bash
cd IdentityHubServer/IdentityHub.API
dotnet run --launch-profile https
```

- **HTTPS:** `https://localhost:7039` (matches the Angular dev service URLs).
- **HTTP only:** `dotnet run --launch-profile http` → `http://localhost:5081`.
- **Docker:** `Container (Dockerfile)` profile exists in `launchSettings.json`.

**CORS** (from `Program.cs`): allows `http://localhost:4200` and `https://localhost:4200` for the SPA.

---

## Frontend — `IdentityHubClient/IdentityHub.APP`

### Stack

| Area | Packages / tooling |
|------|---------------------|
| Framework | Angular **18.2** (standalone components, router, forms, HTTP client). |
| UI | **Tailwind CSS** 3.4, **PostCSS**, **Autoprefixer**. |
| Feedback | **ngx-toastr** 18. |
| SSR | **@angular/ssr**, **Express** (`server.ts`, `npm run serve:ssr:IdentityHub.APP` after build). |
| Tests | **Karma**, **Jasmine**. |
| Language | **TypeScript** ~5.5. |

### Application structure

| Area | Path / notes |
|------|----------------|
| Shell layout | `src/app/layout/shell.component.*` — sidebar, top navbar, child `<router-outlet>`. |
| Pages | `src/app/pages/` — `login`, `register`, `dashboard`, `users` (+ detail/edit/delete), `role-claims` (+ detail/edit). |
| Shared chrome | `src/app/components/sidebar`, `top-navbar`. |
| Routing | `src/app/app.routes.ts` — `/app` uses `authGuard`; `/login` and `/register` use `guestGuard`. |
| State / API | `src/app/services/` — `auth.service`, `users.service`, `roles.service`, `role-claims.service`, `dashboard.service` (base URL **`https://localhost:7039`** in code). |
| Cross-cutting | `src/app/guards/`, `src/app/interceptors/auth.interceptor.ts` (adds `Authorization: Bearer` from `localStorage` or `sessionStorage`). |

### SPA navigation

Public **login** and **register** screens; authenticated area under **`/app`** with a **dashboard**, **users** (list, detail, edit, delete), and **role claims** (list, detail, edit). Unmatched URLs send the user back to login. See [`IdentityHubClient/IdentityHub.APP/README.md`](IdentityHubClient/IdentityHub.APP/README.md) for a concise route map.

### NPM scripts

| Script | Command |
|--------|---------|
| `npm start` | `ng serve` — dev server, default `http://localhost:4200`. |
| `npm run build` | Production build → `dist/identity-hub.app`. |
| `npm test` | `ng test` (Karma). |
| `npm run serve:ssr:IdentityHub.APP` | Serves SSR bundle from `dist/identity-hub.app/server/server.mjs` (run after `ng build`). |

---

## Running the full stack locally

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download), Node.js + npm (compatible with Angular 18).

1. **API** (terminal 1):

   ```bash
   cd IdentityHubServer/IdentityHub.API
   dotnet run --launch-profile https
   ```

2. **Angular app** (terminal 2):

   ```bash
   cd IdentityHubClient/IdentityHub.APP
   npm install
   npm start
   ```

3. Open **`http://localhost:4200`**, sign in with a seeded admin account or use the registration screen as needed.

Use **Swagger** at the running API origin (see launch profile) to explore and test the API with a JWT.

---

## Project goal

Provide a centralized identity and access management experience for internal systems: administrators manage users and permissions with consistency, traceability, and security.

## Project description

IdentityHub combines a backend API and a web UI for the full user-administration lifecycle: fewer manual access mistakes and consistent role-based rules across applications.

## Core business rules

- User accounts are created and managed by authorized administrators (per API policies).
- Users have one or more roles; role assignment defines permission scope.
- Protected resources require valid authentication and authorization policies.
- Sensitive operations (roles, permission claims) require privileged permissions such as `Roles.Manage`.
- Tokens and sessions are validated; refresh and logout revoke or rotate state in the auth flow.

## Main features (product)

- User creation, update, delete, and listing
- Role assignment on users from the admin UI
- Role CRUD and permission claims on roles (API + admin UI for role claims)
- Dashboard aggregates (users, sessions, weekly trends, security events)
- JWT access tokens + refresh tokens and session tracking

## Why IdentityHub

Centralized user and role administration improves security posture, reduces operational overhead, and keeps a clear model for identity governance.
