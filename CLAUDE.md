# CLAUDE.md

Guidance for working in this repository.

## Overview

Full-stack CMS generated from a SQL Server schema. Backend is a .NET 9 Web API (Dapper, **no EF**);
frontend is Angular 20 (standalone components) with PrimeNG. Chinese/English bilingual UI.

Keep this file lean — it loads every session. Deeper detail lives in reference files; read the one
the task calls for:

- **`spec/code-gen.convention.md`** — full backend + frontend conventions and app-shell gotchas.
  Read before adding or editing a feature.
- **`spec/features.md`** — as-built notes per implemented feature (incl. the full **Auth** section).
  Read the section for the one you touch.
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

## Conventions (full detail + gotchas in `spec/code-gen.convention.md` — read before adding/editing a feature)

**Backend** — Dapper only, async; connections via `IDbConnectionFactory` (`Infrastructure/`), `RTRIM()` nchar,
`DateOnly`/`TimeOnly` handlers in `Program.cs`. Per table: `{Table}.cs` / `{Table}Request.cs` / `{Table}Query.cs`
+ repo interface/impl in `Repositories/` (DI in `Program.cs`). Routes `/api/{plural}`: `GET`, `POST /query`,
`GET /{id}`, `POST`, `PUT` (key in body), `DELETE /{id}`; lookups `GET /api/lookups/{plural}`. n-n =
delete-then-reinsert in a transaction. Tests: `CmsApiFactory` swaps the repo for an in-memory fake (no SQL Server).

**Frontend** — standalone + signals; lazy `loadComponent` routes; providers in `app.config.ts`; path aliases
`@env`/`@core`/`@features`. Feature folders `features/{plural}/{table}-list|-detail|-form/`. List = `p-table` +
filter `p-drawer` (sessionStorage-persisted); form = Reactive Forms + `forkJoin` lookups. Gotchas (opt-in list
inline edit, sticky form toolbar, window-doesn't-scroll app shell, Ultima sidebar theming) are in the reference.

**RowAudit — applies to every CRUD repository; full detail in `spec/features.md` → RowAudit.** Every
Insert/Update/Delete must write one `RowAudit` row via the shared `Services/RowAuditWriter.cs`
(`AddScoped`, needs `IHttpContextAccessor`). Call `LogInsert/LogUpdate/LogDelete(table, …, conn, tx, ct)`
**on the operation's own connection + transaction** so a rolled-back change writes no audit row. Pattern:
Insert → load the new row inside the tx → `LogInsert`; Update → load `before`, apply, load `after` →
`LogUpdate` (diffs scalar columns only; nav/collections/counts ignored); Delete → load the row → delete →
`LogDelete`. Repos whose SELECT has nav joins/count subqueries use an own-columns-only `AuditSelectColumns`
for the snapshots. `ActionDesc` = first string property (Insert/Delete) or changed-column names (Update).
Read side: `GET /api/rowaudit?tableName=&pkid=` (newest first) feeds the reusable `RowAuditBadge`
(`core/components/row-audit-badge/`) — **every detail and form page carries it in the `page-header__actions`
toolbar**, passing the page's table name + the record's pkid (detail `record()?.pkid ?? 0`; form an `auditPkid`
signal, 0 in add mode). Add it to any new detail/form page.

**Auth — applies to every feature; full detail in `spec/features.md` → Auth.** The rules you must honor when
adding *any* feature:
- Backend: a global `RequireAuthenticatedUser` fallback protects **every controller by default** — do nothing
  to opt in; add `[AllowAnonymous]` only per-*action* for genuinely public endpoints.
- Frontend: **every new feature route needs `canActivate: [authGuard]`** (`@core/guards/auth.guard`); only
  `login` is public. The Bearer interceptor (+401→`/login`) is automatic — services need no auth code. Read
  profile/roles from `AuthService` (session storage), never a new API call; gate Admin UI on `auth.hasRole('Admin')`.
- Backend tests hit protected endpoints via `CmsApiFactory.CreateAuthenticatedClient()` (pass role names for
  role-gated cases), not `CreateClient()`.
- Password hashing/complexity, act-on-your-own-record endpoints (id from JWT, never the body), and the
  no-hash-over-the-wire rule are documented in the Auth section of `spec/features.md`.

## Implemented features

As-built notes: **`spec/features.md`** — read the section for the feature you touch. **AppRole** is the reference
feature (copy its structure for new tables). Customized features (e.g. **FeaturedPromoItem**) break the triad;
their specs live in `spec/custom/`. When you finish a feature, append its section to `features.md` and add a row below.

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `auth.sql` | **Reference feature** — copy for new tables. String PK `RoleId`; n-n with AppUser |
| AppUser | 使用者 | `/app-users` | `auth.sql` | String PK `UserId`; n-n with AppRole; backend-only `PasswordHash`; Admin-only reset-to-default (`POST …/reset-password`) |
| FeaturedPromoItem | 上稿作業 | `/featured-promo-items` | `promotion.sql` | **Custom** weekly board (center × Mon–Sun × 3 slots), not list/detail/form; slot `/move`; spec in `spec/custom/` |
| Auth | 登入 | `/login`, `/profile`, `POST /api/Auth/*` | `auth.sql` | Login + JWT authorization end-to-end; My Profile + Change Password |
| RowAudit | 異動記錄 | `GET /api/rowaudit` | `dbo.RowAudit` | **Cross-cutting** audit writer (`RowAuditWriter`); every CRUD Insert/Update/Delete writes one audit row on the op's transaction. Read side: `GET /api/rowaudit?tableName=&pkid=` + reusable `RowAuditBadge` on every detail/form toolbar |

## gstack

Use the **`/browse`** skill from gstack for **all** web browsing. **Never** use `mcp__claude-in-chrome__*` tools.

Available skills: `/office-hours`, `/plan-ceo-review`, `/plan-eng-review`, `/plan-design-review`,
`/design-consultation`, `/design-shotgun`, `/design-html`, `/review`, `/ship`, `/land-and-deploy`,
`/canary`, `/benchmark`, `/browse`, `/connect-chrome`, `/qa`, `/qa-only`, `/design-review`,
`/setup-browser-cookies`, `/setup-deploy`, `/setup-gbrain`, `/retro`, `/investigate`,
`/document-release`, `/document-generate`, `/codex`, `/cso`, `/autoplan`, `/plan-devex-review`,
`/devex-review`, `/careful`, `/freeze`, `/guard`, `/unfreeze`, `/gstack-upgrade`, `/learn`.
