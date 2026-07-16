# CLAUDE.md

Guidance for working in this repository. **Keep this file lean — it loads every session.** It is an index,
not a manual: detail belongs in the reference files below. Add it there and point to it from here rather
than restating it — a fact in two places drifts out of sync.

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
| **`spec/code-gen.convention.md`** | The complete backend/frontend convention: per-table file layout, endpoint table, special column types, list/form/app-shell gotchas, test harness. **Before adding or editing any feature.** |
| **`spec/features.md`** | As-built notes per feature, plus the full **RowAudit**, **ErrorHandling**, and **Auth** cross-cutting sections. **Read the section for the feature you touch — and the cross-cutting section for any rule below that your change trips.** |
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

## Building a feature

**AppRole is the reference feature — copy its structure for new tables.** Read
**`spec/code-gen.convention.md`** first; it carries the whole convention. The shape, for orientation only:

- **Backend** — Dapper only, async; connections via `IDbConnectionFactory`. Per table: `{Table}.cs` /
  `{Table}Request.cs` / `{Table}Query.cs` + repo interface/impl in `Repositories/` (DI in `Program.cs`).
  Routes `/api/{plural}`; lookups `GET /api/lookups/{plural}`.
- **Frontend** — standalone + signals; lazy `loadComponent` routes; providers in `app.config.ts`; aliases
  `@env`/`@core`/`@features`. Folders `features/{plural}/{table}-list|-detail|-form/`.

Five things will bite you if you skip the reference: opt-in list inline edit, the sticky form toolbar, the
window-doesn't-scroll app shell, Ultima sidebar theming, and — for anything printed — the global `@media
print` shell-undo in `styles.scss`, which needs `!important` to beat `.content`'s component-scoped
`(0,2,0)` rule (skip it and multi-page output silently clips to one viewport; see CourseBrochure in
features.md).

When you finish a feature, append its as-built section to `spec/features.md` and add a row to the table below.

## Cross-cutting rules — every feature, no exceptions

Not optional, not per-feature decisions, and easy to get **silently** wrong. These lines are the trigger, not
the rule — each has a full section of the same name in `spec/features.md`. **Read that section before you
touch the area.**

- **RowAudit** — every CRUD repo's Insert/Update/Delete writes one audit row via `Services/RowAuditWriter.cs`,
  passing the operation's **own `conn` + `tx`** (a rolled-back change must leave no audit row). Update and
  Delete must load the row *before* the change. Every detail + form page carries `<row-audit-badge>`.
- **ErrorHandling** — `Middleware/ExceptionHandlingMiddleware.cs` turns any unhandled exception into one
  generic 500, so controllers need no try/catch for unexpected errors. `authInterceptor` owns the **single**
  5xx toast, so every component `error:` handler must `if (isServerError(err)) return;`
  (`@core/utils/http-error.util`) before its own `messages.add(...)` — or one failure toasts twice.
- **Auth** — a global `RequireAuthenticatedUser` fallback protects **every controller by default**;
  `[AllowAnonymous]` goes on genuinely public *actions* only. **Every new feature route needs
  `canActivate: [authGuard]`.** Read profile/roles from `AuthService`, never a new API call.

## Implemented features

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `auth.sql` | **Reference feature.** String PK `RoleId`; n-n with AppUser |
| AppUser | 使用者 | `/app-users` | `auth.sql` | String PK `UserId`; n-n with AppRole; backend-only `PasswordHash`; Admin-only `POST …/reset-password` |
| FeaturedPromoItem | 上稿作業 | `/featured-promo-items` | `promotion.sql` | **Custom** weekly board (center × Mon–Sun × 3 slots), not the triad; slot `/move`; spec in `spec/custom/` |
| Auth | 登入 | `/login`, `/profile`, `POST /api/Auth/*` | `auth.sql` | Login + JWT authorization end-to-end; My Profile + Change Password |
| CourseBrochure | 課程簡介 | — (on `/courses/:id`) | `course.sql` | **Print document**, no API/route. A4 preview + `window.print()`. Prose is ~10% HTML — render via `@core/utils/prose-html.util`, never `pre-wrap`. Print needs the **global** shell-undo in `styles.scss` (`!important` — see features.md) |
| RowAudit | 異動記錄 | `GET /api/rowaudit` | `dbo.RowAudit` | **Cross-cutting** — see the rules above |
| ErrorHandling | 錯誤處理 | — (middleware + interceptor) | — | **Cross-cutting** — see the rules above |

## gstack

Use the **`/browse`** skill for **all** web browsing. **Never** use `mcp__claude-in-chrome__*` tools.
(The available gstack skills are listed automatically every session — don't duplicate that list here.)
