# CLAUDE.md

Guidance for working in this repository.

## Overview

Full-stack CMS generated from SQL Server schema. Backend is a .NET 9 Web API (Dapper, **no EF**);
frontend is Angular 20 (standalone components) with PrimeNG. Chinese/English bilingual UI.

- **`database/*.sql`** — source-of-truth SQL Server schema (`auth`, `admin`, `course`, `promotion`).
- **`spec/code-gen.convention.md`** — the code-generation conventions. Read this before adding a feature.
- **`spec/*.spec.md`**, **`spec/feature-spec.template.md`** — per-feature build specs and template.
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

- **Dapper only, async.** Connections come from `IDbConnectionFactory` (`Infrastructure/`).
  `DateOnly`/`TimeOnly` handlers are registered in `Program.cs`; `nchar` columns must be `RTRIM()`ed in SQL.
- **Per table**: `{Table}.cs` (response, incl. nav objects / subquery counts), `{Table}Request.cs`
  (write DTO, FK pkids + n-n as `List<>`), `{Table}Query.cs` (search DTO). Repository interface + impl
  in `Repositories/`; register in `Program.cs` DI.
- **n-n**: delete-then-reinsert inside a transaction on create/update; read via a second query on the same connection.
- **Routes**: `/api/{tablePlural}` — `GET` (all), `POST /query` (filter), `GET /{id}`, `POST`, `PUT`
  (pkid/business-key in body, no route param), `DELETE /{id}`. String PKs use `{id}` with no `:int` constraint.
- **Lookups**: `GET /api/lookups/{plural}` returns slim option lists for FK / n-n selects.
- **Tests**: swap the repository for an in-memory fake via `CmsApiFactory` (`WebApplicationFactory<Program>`);
  `Program.cs` exposes `public partial class Program` so the factory can boot it. No SQL Server required.

## Frontend conventions

- Standalone components, signals. Config in `app.config.ts` (`provideRouter`, `provideHttpClient(withFetch())`,
  `provideAnimationsAsync`, `providePrimeNG` with Aura preset). Routes are lazy `loadComponent`.
- **Path shorthands** (`tsconfig.json`): `@env/*` → `src/environments`, `@core/*` → `src/app/core`,
  `@features/*` → `src/app/features`.
- **Environments**: `environment.ts` (prod) / `environment.development.ts` (dev), swapped via
  `fileReplacements` in `angular.json`. `apiBaseUrl` is absolute — **no dev proxy**.
- **Feature folders**: `features/{table-plural}/{table}-list|-detail|-form/`.
- **List page**: `p-table` (sortable/paginated), filter `p-drawer`; persist to sessionStorage keys
  `{table}-list-filters` / `-sort` / `-page`. `p-select`/`p-multiselect` in drawer use `appendTo="body"`.
- **Form page**: Reactive Forms, `forkJoin` for parallel lookups on init; `p-multiselect` for n-n
  (`[maxSelectedLabels]="9999"`, chips wrapped via `::ng-deep`); immutable business keys disabled in edit mode.
- **Sidebar**: add nav entries in `app.ts` (`navSections` → groups → children) and they render in
  `app.html`. Shell style follows the PrimeNG **Ultima** template (https://ultima.primeng.org):
  light/white topbar (hamburger + logo + action icons) and light sidebar with uppercase gray section
  titles, rounded menu items, chevron on expandable groups, and a primary-tinted rounded pill for the
  active item. Styling uses theme tokens (`--p-surface-*`, `--p-highlight-*`, `--p-primary-*`) in `app.scss`.

## Reference feature: AppRole (角色)

First implemented feature — copy its structure for new tables. Schema: `database/auth.sql`.

- `AppRole` PK is the string `RoleId` (clustered key; `pkid` is a surrogate IDENTITY shown as 主代碼).
  `RoleId` is entered on add, immutable on edit, and is the route id (`encodeURIComponent`).
- n-n `AppRole ↔ AppUser` via `AppUserRole` (keyed on `UserId`/`RoleId`). List shows `userCount`
  (subquery); detail/form load the user select from `GET /api/lookups/appusers` (label `UserName (UserId)`).
- Sidebar: **系統管理 Admin → 角色 AppRole** (`/app-roles`).
- Backend: `Controllers/AppRolesController.cs`, `Repositories/AppRoleRepository.cs`, `Models/AppRole*.cs`.
  Frontend: `features/app-roles/*`, `core/services/app-role.service.ts`, `core/services/lookup.service.ts`.
- Tests: `CMS.API.Tests/AppRolesControllerTests.cs` (13); `app-role-*.spec.ts` + `app-role.service.spec.ts` (Angular).
