# IdentityHub

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-5C2D91?logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-18-DD0031?logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework%20Core-ORM-6DB33F)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)
![License](https://img.shields.io/badge/License-Private-informational)

IdentityHub is a user management platform with an administrative panel focused on creating, editing, and assigning roles to users in a secure and controlled way.

## Project Goal

The main goal of IdentityHub is to provide a centralized identity and access management experience for internal systems, enabling administrators to manage users and permissions with consistency, traceability, and security.

## Project Description

IdentityHub combines a backend API and a web interface to support the complete lifecycle of user administration.  
It was designed to simplify identity operations, reduce manual access control errors, and standardize role-based access rules across applications.

## Core Business Rules

- User accounts must be created and managed only by authorized administrators.
- Every user can have one or more roles, and role assignment is required to define access scope.
- Access to protected resources must be validated through authentication and authorization policies.
- Sensitive operations (such as role/permission changes) must be restricted to privileged profiles.
- User status and access data must remain consistent to prevent unauthorized actions.
- Authentication tokens must be validated and handled securely throughout the session lifecycle.

## Main Features

- User creation and profile updates
- Role assignment and role management
- Permission-aware access control
- Administrative interface for identity operations
- Token-based authentication for protected endpoints

## Technologies Used

- **Backend:** .NET, ASP.NET Core Web API
- **Authentication & Authorization:** JWT, ASP.NET Identity, policy-based authorization
- **Data Access:** Entity Framework Core
- **Database:** SQLite
- **Frontend:** Angular, TypeScript
- **Documentation & API Testing:** Swagger / OpenAPI

## Why IdentityHub

IdentityHub helps teams enforce access governance with less operational overhead.  
By centralizing user and role administration, it improves security posture, supports scalability, and provides a clearer operational model for identity management.
