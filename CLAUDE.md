# CLAUDE.md

Guidance for working in this repository.

## Overview

Full-stack CMS generated from SQL Server schema. Backend is a .NET 9 Web API (Dapper, **no EF**);
frontend is Angular 20 (standalone components) with PrimeNG. Chinese/English bilingual UI.

This file holds the **always-needed** core (layout, run/test, conventions). Deeper detail lives in
reference files — read them only when the task calls for it:

- **`spec/code-gen.convention.md`** — code-generation conventions. Read before adding a feature.
- **`spec/features.md`** — as-built notes for features already implemented (AppRole, AppUser, …).
  Read the relevant section when a task touches that feature.
- **`spec/{schema}/{Table}.md`** + **`spec/feature-spec.template.md`** — per-feature build specs and template.
- **`database/*.sql`** — source-of-truth SQL Server schema (`auth`, `admin`, `course`, `promotion`).
- **`spec/ui-sample-*.png`** — UI style references only (not literal content).
- **`src/`** — application code (see below).

## Layout

```
src/
  CMS.sln
  CMS.API/          .NET 9 Web API (Dapper, Swagger, CORS) — http://localhost:5000
  CMS.API.Tests/    xUnit tests (WebApplicationFactory + in-memory fake repo, no DB needed)
  CMS.NG/           Angular 20 + PrimeNG — http://localhost:4200
```

## Run & test

Backend (Swagger UI at `/swagger`):
```bash
cd src/CMS.API && dotnet run
dotnet test src/CMS.API.Tests/CMS.API.Tests.csproj
```

Frontend (Karma + Jasmine is the default `ng test`):
```bash
cd src/CMS.NG && npm install && npm start
npm test                       # CI: npx ng test --watch=false --browsers=ChromeHeadless
```

- DB connection string: `src/CMS.API/appsettings.json` → `ConnectionStrings:CMS`
  (`Server=.\SQLEXPRESS;Database=CMS;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False`).
- CORS allows any loopback origin (dev). API listens on port 5000 via `launchSettings.json`.

## Backend conventions

- **Dapper only, async.** Connections via `IDbConnectionFactory` (`Infrastructure/`). `DateOnly`/`TimeOnly`
  handlers registered in `Program.cs`; `RTRIM()` `nchar` columns in SQL.
- **Per table**: `{Table}.cs` (response + nav objects / subquery counts), `{Table}Request.cs` (write DTO —
  FK pkids + n-n as `List<>`), `{Table}Query.cs` (search DTO). Repo interface + impl in `Repositories/`, DI in `Program.cs`.
- **n-n**: delete-then-reinsert in a transaction on create/update; read via a second query on the same connection.
- **Routes** `/api/{tablePlural}`: `GET` (all), `POST /query` (filter), `GET /{id}`, `POST`, `PUT`
  (pkid/business-key in body, no route param), `DELETE /{id}`. String PKs use `{id}` (no `:int`).
- **Lookups**: `GET /api/lookups/{plural}` → slim option lists for FK / n-n selects.
- **Tests**: `CmsApiFactory` (`WebApplicationFactory<Program>`) swaps the repo for an in-memory fake; no
  SQL Server needed (`Program.cs` exposes `public partial class Program`).

## Frontend conventions

- Standalone components + signals; lazy `loadComponent` routes. `app.config.ts`: `provideRouter`,
  `provideHttpClient(withFetch())`, `provideAnimationsAsync`, `providePrimeNG` (Aura preset).
- **Path shorthands** (`tsconfig.json`): `@env/*`, `@core/*` → `src/app/core`, `@features/*` → `src/app/features`.
- **Environments**: `environment.ts` (prod) / `.development.ts` (dev), swapped by `fileReplacements` in
  `angular.json`. `apiBaseUrl` absolute — **no dev proxy**.
- **Feature folders**: `features/{table-plural}/{table}-list|-detail|-form/`.
- **List page**: `p-table` (sortable/paginated) + filter `p-drawer`; persist to sessionStorage
  `{table}-list-filters`/`-sort`/`-page`. Drawer `p-select`/`p-multiselect` use `appendTo="body"`.
- **Form page**: Reactive Forms, `forkJoin` for parallel lookups on init; n-n via `p-multiselect`
  (`[maxSelectedLabels]="9999"`, chips wrapped via `::ng-deep`); immutable business keys disabled in edit.
- **Sidebar**: nav entries in `app.ts` (`navSections` → groups → children), rendered by `app.html`. Shell
  follows PrimeNG **Ultima** (https://ultima.primeng.org): light topbar + sidebar, uppercase gray section
  titles, rounded items, primary-tinted pill for active. Theme tokens (`--p-surface/highlight/primary-*`) in `app.scss`.

## Implemented features

As-built notes live in **`spec/features.md`** — read the section for the feature you're touching.
**AppRole (角色)** is the reference feature: copy its structure for new tables. When you finish a
feature, append its section there and add a row below.

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `database/auth.sql` | Reference feature; string PK `RoleId`; n-n with AppUser |
| AppUser | 使用者 | `/app-users` | `database/auth.sql` | String PK `UserId`; n-n with AppRole; backend-only `PasswordHash` + reset endpoint |
