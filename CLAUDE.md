# CLAUDE.md

Guidance for working in this repository. **Keep this file lean — it loads every session.** Detail belongs in
the reference files below; add it there and point to it from here rather than restating it.

## Overview

Full-stack CMS generated from a SQL Server schema. Backend is a .NET 9 Web API (Dapper, **no EF**);
frontend is Angular 20 (standalone components) with PrimeNG. Chinese/English bilingual UI.

```
src/
  CMS.sln
  CMS.API/          .NET 9 Web API (Dapper, Swagger, CORS) — http://localhost:5000
  CMS.API.Tests/    xUnit (WebApplicationFactory + in-memory fake repo, no DB needed)
  CMS.NG/           Angular 20 + PrimeNG — http://localhost:4200
```

## Reference files — read the one your task calls for

| File | Read it for |
|------|-------------|
| **`spec/code-gen.convention.md`** | Full backend/frontend conventions, special column types, endpoint table, app-shell gotchas. **Before adding or editing any feature.** |
| **`spec/features.md`** | As-built notes per feature, incl. the full **Auth**, **RowAudit**, **ErrorHandling** sections. **The section for the feature you touch.** |
| **`spec/custom/{Table}/`** | Specs + mockups for features that break the list/detail/form triad (e.g. FeaturedPromoItem's weekly board). |
| `spec/{schema}/{Table}.md`, `spec/feature-spec.template.md` | Per-feature build specs and the template. |
| `database/*.sql` | Source-of-truth schema (`auth`, `admin`, `course`, `promotion`). |
| `spec/ui-sample-*.png` | UI style reference only — not literal content. |

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

## Conventions — summary only

**Full detail + gotchas in `spec/code-gen.convention.md`. Read it before adding or editing a feature.**

**Backend** — Dapper only, async; connections via `IDbConnectionFactory`; `RTRIM()` nchar. Per table:
`{Table}.cs` / `{Table}Request.cs` / `{Table}Query.cs` + repo interface/impl in `Repositories/` (DI in
`Program.cs`). Routes `/api/{plural}`: `GET`, `POST /query`, `GET /{id}`, `POST`, `PUT` (key in body),
`DELETE /{id}`; lookups `GET /api/lookups/{plural}`. n-n = delete-then-reinsert in a transaction.

**Frontend** — standalone + signals; lazy `loadComponent` routes; providers in `app.config.ts`; aliases
`@env`/`@core`/`@features`. Folders `features/{plural}/{table}-list|-detail|-form/`. List = `p-table` +
filter `p-drawer` (sessionStorage-persisted); form = Reactive Forms + `forkJoin` lookups. Four gotchas will
bite you if you don't read the reference: opt-in list inline edit, sticky form toolbar, the
window-doesn't-scroll app shell, Ultima sidebar theming.

**Tests** — `CmsApiFactory` swaps the repo for an in-memory fake (no SQL Server); its `CustomizeServices`
hook re-overrides one service for a single test (e.g. a repo that throws).

## Cross-cutting rules — every feature, no exceptions

Not optional, not per-feature decisions, and easy to get silently wrong. Each has a full section of the same
name in `spec/features.md` — read it when you need the why or the edge cases.

**RowAudit** — every CRUD repo's Insert/Update/Delete writes one audit row via `Services/RowAuditWriter.cs`
(`LogInsert` / `LogUpdate` / `LogDelete`). Copy `Repositories/AppRoleRepository.cs`.
- Pass the operation's **own `conn` + `tx`** — a rolled-back change must leave no audit row.
- **Update**: load `before` first. **Delete**: load the row first — its first string column is the description,
  and after the delete there is nothing to read.
- Repos whose SELECT has nav joins/count subqueries need an own-columns-only `AuditSelectColumns` for snapshots.
- Never insert `pkid` (IDENTITY). `ActionDesc` / `PrimaryKeyValues` / `UserName` are the writer's job — don't hand-roll.
- **Every detail + form page**: `<row-audit-badge tableName="{Table}" [pkid]="…" />` first inside
  `<div class="page-header__actions">` — detail `record()?.pkid ?? 0`; form an `auditPkid` signal, 0 in add mode.

**ErrorHandling** — `Middleware/ExceptionHandlingMiddleware.cs` (registered first in `Program.cs`) turns any
unhandled exception into one generic `{ message, traceId }` 500 and logs the real detail server-side.
- No per-controller try/catch for unexpected errors; never return raw exception text.
- Leave 401/403/validation-400 alone — they don't throw, so they pass through.
- `authInterceptor` owns the **single** 5xx toast (and 401→`/login`). So every component `error:` handler must
  run its state cleanup, then `if (isServerError(err)) return;` (`@core/utils/http-error.util`) before its own
  `messages.add(...)` — otherwise one failure toasts twice. Handle only what you can improve on (400, 409).

**Auth** — a global `RequireAuthenticatedUser` fallback protects **every controller by default**; add
`[AllowAnonymous]` only per-*action*, for genuinely public endpoints.
- **Every new feature route needs `canActivate: [authGuard]`** (`@core/guards/auth.guard`); only `login` is public.
- Bearer interceptor is automatic — services need no auth code. Read profile/roles from `AuthService` (session
  storage), never a new API call; gate Admin UI on `auth.hasRole('Admin')`.
- Backend tests hit protected endpoints via `CmsApiFactory.CreateAuthenticatedClient()` (pass role names for
  role-gated cases), not `CreateClient()`.
- Password hashing/complexity, act-on-your-own-record endpoints (id from JWT, never the body), and the
  no-hash-over-the-wire rule: see the Auth section of `spec/features.md`.

## Implemented features

**AppRole is the reference feature — copy its structure for new tables.** When you finish a feature, append its
as-built section to `spec/features.md` and add a row here.

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `auth.sql` | **Reference feature.** String PK `RoleId`; n-n with AppUser |
| AppUser | 使用者 | `/app-users` | `auth.sql` | String PK `UserId`; n-n with AppRole; backend-only `PasswordHash`; Admin-only `POST …/reset-password` |
| FeaturedPromoItem | 上稿作業 | `/featured-promo-items` | `promotion.sql` | **Custom** weekly board (center × Mon–Sun × 3 slots), not the triad; slot `/move`; spec in `spec/custom/` |
| Auth | 登入 | `/login`, `/profile`, `POST /api/Auth/*` | `auth.sql` | Login + JWT authorization end-to-end; My Profile + Change Password |
| RowAudit | 異動記錄 | `GET /api/rowaudit` | `dbo.RowAudit` | **Cross-cutting** — see the rules above |
| ErrorHandling | 錯誤處理 | — (middleware + interceptor) | — | **Cross-cutting** — see the rules above |

## gstack

Use the **`/browse`** skill for **all** web browsing. **Never** use `mcp__claude-in-chrome__*` tools.
(The available gstack skills are listed automatically every session — don't duplicate that list here.)
