# IdentityHub Project Documentation

## 1. Project Objectives

IdentityHub is an Identity and Access Management (IAM) platform focused on secure user administration and operational visibility.

Primary objectives:
- Provide a complete account lifecycle: registration, email confirmation, password recovery, profile updates, and password change.
- Offer robust administration of users, roles, and permission claims.
- Enforce fine-grained authorization using policy-permission mapping.
- Strengthen session security with token/session validation and permission versioning.
- Expose security observability through audit logs, alerts, and activity timelines.

## 2. Repository and Solution Structure

Top-level:
- `IdentityHubServer/`: .NET backend solution and projects.
- `IdentityHubClient/IdentityHub.APP/`: Angular frontend application.
- `README.md`: quick start guide.
- `PROJECT_DOCUMENTATION.md`: complete project documentation.

Backend projects (`IdentityHubServer`):
- `IdentityHub.API`: presentation layer (controllers, middleware, authentication, authorization, Swagger, rate limiting).
- `IdentityHub.Application`: application services, CQRS handlers, DTOs, contracts.
- `IdentityHub.Domain`: entities, domain constants, interfaces.
- `IdentityHub.Infrastructure`: EF Core data access, repositories, migrations, security and infrastructure services.
- `IdentityHub.IoC`: dependency injection composition.
- `IdentityHub.API.Tests`: integration and authorization test suite.

## 3. Architecture

### 3.1 Architectural Style

The backend follows layered architecture with clear separation of concerns:
- API layer orchestrates HTTP concerns and middleware pipeline.
- Application layer holds use cases and business orchestration.
- Domain layer defines core business model and rules.
- Infrastructure layer implements persistence and external integrations.

The frontend follows feature-oriented modularization using Angular standalone components.

### 3.2 Backend Runtime Flow

High-level request flow:
1. ASP.NET middleware pipeline receives request.
2. JWT is validated.
3. Session and permission version are validated against the database.
4. Permission policy is evaluated through claim-based authorization.
5. Controller delegates to application services/CQRS handlers.
6. Infrastructure persists/retrieves data via EF Core.

### 3.3 Frontend Runtime Flow

High-level UI flow:
1. User authenticates via auth screens.
2. Access token is used for API calls; refresh token stays in secure cookie.
3. Route guards check required permissions.
4. Layout shell renders navigation based on access catalog.
5. Feature pages consume shared components and services.

## 4. Core Rules

### 4.1 Authentication Rules
- Access token is JWT-based and short-lived.
- Refresh token is stored as `ih_refresh` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`).
- Refresh token is rotated on refresh requests.

### 4.2 Authorization Rules
- Policies are dynamically mapped to permission names (e.g. `Users.View`).
- Effective permissions are provided as `permission` claims (primarily role-based).
- Frontend route access is enforced by permission guards and navigation catalog rules.

### 4.3 Session and Token Hardening Rules
- JWT includes `sid` (session id).
- JWT includes `permission_version`.
- API validates active session state and permission version on authenticated requests.
- Permission updates invalidate previously issued tokens by version increment.

### 4.4 Abuse Protection Rules
Rate limiting is applied to sensitive auth endpoints:
- Login.
- Forgot password.
- Resend confirmation.

### 4.5 Data and Environment Rules
- API applies pending migrations automatically on startup, except in `Testing` environment.
- Development/test seed is idempotent and only runs in non-production contexts.
- Sensitive values (JWT key, SMTP credentials) must be supplied through User Secrets or environment variables, not committed in source control.

## 5. Layouts and Frontend UI Organization

Main layout zones:
- `auth-layout`: public/authentication shell (`/login`, `/register`, `/forgot-password`, etc.).
- `main-layout`: authenticated shell for application routes under `/app`.

Frontend organization:
- `core/`: guards, interceptors, and cross-cutting client concerns.
- `features/`: domain-specific screens (users, roles, security, dashboard, profile).
- `shared/components/`: reusable visual components.
- `shared/constants/`: permission and navigation catalogs.
- `shared/services/`, `shared/pipes/`, `shared/directives/`: reusable behavior and utilities.

Design-system highlights:
- Standardized components for states, table/filter patterns, dialogs, and metrics.
- Consistent loading/error/empty/content UX patterns.
- Tailwind-based design tokens for color, spacing, and typography consistency.

## 6. Libraries and Versions

### 6.1 Backend Stack
- .NET target framework: `net10.0`.
- ASP.NET Core Web API.
- ASP.NET Core Identity.
- JWT Bearer authentication.
- Entity Framework Core + SQLite.
- Swagger via Swashbuckle.
- MediatR-style CQRS patterns in application layer.

### 6.2 Frontend Stack
- Angular: `^18.2.0`.
- TypeScript: `~5.5.2`.
- Tailwind CSS: `^3.4.19`.
- ngx-toastr: `^18.0.0`.
- RxJS: `~7.8.0`.
- Optional SSR support through `@angular/ssr` and Express.

### 6.3 Test Stack
- Backend: xUnit + `Microsoft.AspNetCore.Mvc.Testing`.
- Frontend: Karma + Jasmine + ChromeHeadless launcher.

## 7. Permission Model

Permission domains include:
- `Users.*`: user viewing and lifecycle operations.
- `Roles.*`: role CRUD and permission management.
- `Dashboard.View`.
- `Sessions.*` and `Activity.View`.
- `Audit.View`.
- `SecurityEvents.*`.
- `SecuritySettings.*`.
- `Permissions.Catalog.View`, `Permissions.Matrix.View`.
- `UserInvites.*`.

Compatibility note:
- `Users.Invites.View` remains as legacy backend constant while frontend access catalogs use `UserInvites.View`.

## 8. API and Screen Domains

Backend domain groups:
- Auth and session management.
- User and role administration.
- Role permission/claim management.
- Dashboard and operational metrics.
- Audit logs and security alerts.
- Security settings.
- User invites.

Frontend route domains:
- Public auth routes.
- Authenticated app routes under `/app` with permission-driven visibility.
- Dedicated access-denied route for unauthorized navigation.

## 9. Testing Strategy

### 9.1 Backend Tests
- Integration tests use `WebApplicationFactory<Program>` with in-memory SQLite.
- Authorization tests verify permission-policy enforcement per endpoint.
- Security tests cover token/session hardening and critical auth flows.

Command:
```bash
cd IdentityHubServer
dotnet test IdentityHub.API.Tests
```

### 9.2 Frontend Tests
- Unit/component tests run with Karma/Jasmine.
- Headless browser execution depends on Chrome binary availability.

Command:
```bash
cd IdentityHubClient/IdentityHub.APP
npm test -- --watch=false --browsers=ChromeHeadless
```

## 10. Build and Run

Backend:
```bash
cd IdentityHubServer/IdentityHub.API
dotnet run --launch-profile https
```

Backend build:
```bash
cd IdentityHubServer
dotnet tool restore
dotnet build IdentityHub.slnx
```

Frontend dev:
```bash
cd IdentityHubClient/IdentityHub.APP
npm install
npm start
```

Frontend production build:
```bash
cd IdentityHubClient/IdentityHub.APP
npm run build
```

## 11. Quality and Delivery Notes

- Backend and frontend builds are healthy in current local validation.
- Backend test suite is stable and extensive.
- Frontend tests require Chrome/`CHROME_BIN` setup in environments where Chrome is not installed.
- No repository-level GitHub Actions workflow is currently versioned; adding CI is recommended for build/test automation.

## 12. Extension Checklist

When implementing a new feature:
1. Define/extend permission constants in backend and frontend catalogs.
2. Protect backend endpoints with matching policies.
3. Add/update frontend routes with permission metadata.
4. Add navigation entries with `requiredAny` rules.
5. Reuse shared UI components and state patterns.
6. Add tests (authorization, integration, and frontend tests where applicable).
7. Update `README.md` and this documentation file.

## 13. Glossary

- Permission claim: fine-grained authorization unit.
- Policy: authorization contract mapped to permission names.
- Session ID (`sid`): token-bound session identifier.
- Permission version: mechanism to invalidate stale tokens after permission changes.
- Invite lifecycle: pending, accepted, expired, canceled states.
