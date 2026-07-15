# CLAUDE.md

Guidance for working in this repository.

## Overview

Full-stack CMS generated from a SQL Server schema. Backend is a .NET 9 Web API (Dapper, **no EF**);
frontend is Angular 20 (standalone components) with PrimeNG. Chinese/English bilingual UI.

Keep this file lean — it loads every session. Deeper detail lives in reference files; read the one
the task calls for:

- **`spec/code-gen.convention.md`** — full backend + frontend conventions and app-shell gotchas.
  Read before adding or editing a feature.
- **`spec/features.md`** — as-built notes per implemented feature. Read the section for the one you touch.
- **`spec/{schema}/{Table}.md`** + **`spec/feature-spec.template.md`** — per-feature build specs and template.
- **`spec/custom/{Table}/`** — specs (+ mockups) for **customized** features that break the standard
  list/detail/form triad (e.g. FeaturedPromoItem's weekly board). Read before touching one.
- **`database/*.sql`** — source-of-truth schema (`auth`, `admin`, `course`, `promotion`).
- **`spec/ui-sample-*.png`** — UI style references only (not literal content).

## Layout

```
src/
  CMS.sln
  CMS.API/          .NET 9 Web API (Dapper, Swagger, CORS) — http://localhost:5000
  CMS.API.Tests/    xUnit tests (WebApplicationFactory + in-memory fake repo, no DB needed)
  CMS.NG/           Angular 20 + PrimeNG — http://localhost:4200
```

## Run & test

```bash
cd src/CMS.API && dotnet run                          # Swagger UI at /swagger
dotnet test src/CMS.API.Tests/CMS.API.Tests.csproj

cd src/CMS.NG && npm install && npm start
npm test                                              # CI: npx ng test --watch=false --browsers=ChromeHeadless
```

- DB connection string: `src/CMS.API/appsettings.json` → `ConnectionStrings:CMS`
  (`Server=.\SQLEXPRESS;Database=CMS;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False`).
- CORS allows any loopback origin (dev). API listens on port 5000 via `launchSettings.json`.

## Conventions (summary — full detail + gotchas in `spec/code-gen.convention.md`)

**Backend** — Dapper only, async; connections via `IDbConnectionFactory` (`Infrastructure/`), `RTRIM()` nchar,
`DateOnly`/`TimeOnly` handlers in `Program.cs`. Per table: `{Table}.cs` / `{Table}Request.cs` / `{Table}Query.cs`
+ repo interface/impl in `Repositories/` (DI in `Program.cs`). Routes `/api/{plural}`: `GET`, `POST /query`,
`GET /{id}`, `POST`, `PUT` (key in body), `DELETE /{id}`; lookups `GET /api/lookups/{plural}`. n-n =
delete-then-reinsert in a transaction. Tests: `CmsApiFactory` swaps the repo for an in-memory fake (no SQL Server).

**Frontend** — standalone + signals; lazy `loadComponent` routes; providers in `app.config.ts`; path aliases
`@env`/`@core`/`@features`. Feature folders `features/{plural}/{table}-list|-detail|-form/`. List = `p-table` +
filter `p-drawer` (sessionStorage-persisted); form = Reactive Forms + `forkJoin` lookups. Gotchas covered in the
reference: opt-in list inline edit, sticky form toolbar, the app-shell scroll model (**window doesn't scroll**),
and Ultima sidebar theming.

## Implemented features

As-built notes live in **`spec/features.md`** — read the section for the feature you're touching.
**AppRole (角色)** is the reference feature: copy its structure for new tables. When you finish a feature,
append its section there and add a row below. Most features follow the list/detail/form triad; **customized**
ones (e.g. **FeaturedPromoItem**, a weekly board with a non-standard `POST /api/{plural}/move`) don't — their
specs live under `spec/custom/`.

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `database/auth.sql` | Reference feature; string PK `RoleId`; n-n with AppUser |
| AppUser | 使用者 | `/app-users` | `database/auth.sql` | String PK `UserId`; n-n with AppRole; backend-only `PasswordHash` + reset endpoint |
| FeaturedPromoItem | 上稿作業 | `/featured-promo-items` | `database/promotion.sql` | Custom weekly **board** (center tabs × Mon–Sun × 3 slots), inline edit, PromoCode lookup, slot `+`/`−` move (`/move`) |
