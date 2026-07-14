# CMS — Full-Stack App

Generated from the database schema in `../database/*.sql` following `../spec/code-gen.convention.md`.

- **CMS.API** — .NET 9 Web API, Dapper (no EF), Swagger, CORS. Runs on **http://localhost:5000**.
- **CMS.API.Tests** — xUnit tests for the AppRole endpoints (in-memory fake repository, no DB required).
- **CMS.NG** — Angular 20 (standalone) + PrimeNG. Runs on **http://localhost:4200**.

## Prerequisites

- .NET 9 SDK
- Node 20+ / npm
- SQL Server (`.\SQLEXPRESS`) with the `CMS` database created from `../database/*.sql`

## Backend

```bash
cd CMS.API
dotnet run                 # http://localhost:5000  (Swagger UI at /swagger)
```

Connection string lives in `CMS.API/appsettings.json` (`ConnectionStrings:CMS`).

Run backend tests:

```bash
dotnet test CMS.API.Tests/CMS.API.Tests.csproj
```

## Frontend

```bash
cd CMS.NG
npm install
npm start                  # ng serve on http://localhost:4200
```

- API base URL is set per environment in `src/environments/environment*.ts` (no dev proxy).
- Path shorthands (`@env/*`, `@core/*`, `@features/*`) are defined in `tsconfig.json`.

Run frontend tests (Karma + Jasmine):

```bash
npm test                   # ng test  (add --watch=false --browsers=ChromeHeadless for CI)
```

## First feature — AppRole (角色)

Full CRUD with list/filter, view, add, edit, and the AppRole ↔ AppUser (使用者) n-n relationship.

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/approles` | list all |
| POST | `/api/approles/query` | filtered search (keyword, permissionLevel) |
| GET | `/api/approles/{roleId}` | single role (+ assigned user ids) |
| POST | `/api/approles` | create (409 on duplicate RoleId) |
| PUT | `/api/approles` | update (RoleId immutable) |
| DELETE | `/api/approles/{roleId}` | delete |
| GET | `/api/lookups/appusers` | user options for the n-n select |

Sidebar entry: **系統管理 Admin → 角色 AppRole** (`/app-roles`).
