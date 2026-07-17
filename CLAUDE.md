# CLAUDE.md

**Keep this file lean — it loads every session.** It's an index, not a manual: detail lives in the
reference files below. Add a fact there and point to it here; a fact in two places drifts out of sync.

## Overview

Full-stack CMS generated from a SQL Server schema. Backend .NET 9 Web API (Dapper, **no EF**);
frontend Angular 20 (standalone components) + PrimeNG. Chinese/English bilingual UI.

```
src/
  CMS.sln
  CMS.API/          .NET 9 Web API — http://localhost:5000 (Swagger at /swagger)
  CMS.API.Tests/    xUnit (WebApplicationFactory + in-memory fake repo, no DB)
  CMS.NG/           Angular 20 + PrimeNG — http://localhost:4200
```

## Reference files — read the one your task calls for

| File | Read it for |
|------|-------------|
| **`spec/code-gen.convention.md`** | **Before adding/editing any feature.** Whole convention: per-table file layout, endpoint table, special column types, list/form/app-shell gotchas, test harness, and **Environment** (connection string, ports, CORS). |
| **`spec/features.md`** | As-built notes per feature + the full **RowAudit / ErrorHandling / Auth** cross-cutting sections. Read the section for the feature you touch. |
| `spec/custom/{Table}/` | Specs + mockups for features that break the list/detail/form triad (e.g. FeaturedPromoItem's weekly board). |
| `spec/{schema}/{Table}.md`, `spec/feature-spec.template.md` | Per-feature build specs and the template. |
| `database/*.sql` | Source-of-truth schema (`auth`, `admin`, `course`, `promotion`). |
| `spec/ui-sample-*.png` | UI style reference only — not literal content. |

## Run & test

```bash
cd src/CMS.API && dotnet run                          # Swagger at /swagger
dotnet test src/CMS.API.Tests/CMS.API.Tests.csproj    # no DB needed
cd src/CMS.NG && npm install && npm start
npm test                                              # CI: npx ng test --watch=false --browsers=ChromeHeadless
```

## Building a feature

**AppRole is the reference feature — copy its structure.** Read `spec/code-gen.convention.md` first: it
carries inline list edit, the sticky form toolbar, the window-doesn't-scroll app shell, and Ultima sidebar
theming. Printing is separate — the global `@media print` shell-undo in `styles.scss`, written up under
**CourseBrochure** in `spec/features.md`. When done, append an as-built section to `spec/features.md` and
add a row to the table below.

## Cross-cutting rules — every feature, no exceptions

One-line triggers; the full rule (and each way it silently breaks) is the same-named section in
`spec/features.md`. **Read that section before you touch the area.**

- **RowAudit** — each CRUD Insert/Update/Delete writes one audit row via `Services/RowAuditWriter.cs` on the
  operation's **own `conn` + `tx`**; every detail/form page carries `<row-audit-badge>`.
- **ErrorHandling** — `authInterceptor` owns the **single** 5xx toast; each component `error:` handler must
  `if (isServerError(err)) return;` (`@core/utils/http-error.util`) first, or one failure toasts twice.
- **Auth** — global `RequireAuthenticatedUser` guards every controller; every feature route needs
  `canActivate: [authGuard]`; anything administering users/roles/permissions **also** needs class-level
  `[Authorize(Roles = "Admin")]` (sidebar `requiresAdmin` is presentation, the API is reachable directly).

## Implemented features

Every row has an as-built section in `spec/features.md`; ⚠ marks the ones that don't yet.

| Feature | 中文 | Routes | Schema | Notes |
|---------|------|--------|--------|-------|
| AppRole | 角色 | `/app-roles` | `auth.sql` | **Reference feature.** String PK; n-n AppUser; Admin-only |
| AppUser | 使用者 | `/app-users` | `auth.sql` | String PK; n-n AppRole; backend-only `PasswordHash`; `…/reset-password`; Admin-only |
| Course | 課程 | `/courses` | `course.sql` | ⚠ Triad; **inline-edit reference**; n-n JobCategory + Certification |
| CourseGroup | 課程群組 | `/course-groups` | `course.sql` | ⚠ Triad |
| Partner | 合作廠商 | `/partners` | `course.sql` | ⚠ Triad |
| PublishStatus | 發布狀態 | `/publish-statuses` | `admin.sql` | ⚠ Triad; user-entered PK; only real-DB (SQLite) audit tests |
| FeaturedPromoItem | 上稿作業 | `/featured-promo-items` | `promotion.sql` | **Custom** weekly board, not the triad; slot `/move`; spec in `spec/custom/` |
| Auth | 登入 | `/login`, `/profile`, `POST /api/Auth/*` | `auth.sql` | Login + JWT; My Profile + Change Password |
| CourseBrochure | 課程簡介 | — (on `/courses/:id`) | `course.sql` | **Print document**, no API/route; prose via `@core/utils/prose-html.util`, never `pre-wrap` |
| RowAudit | 異動記錄 | `GET /api/rowaudit` | `dbo.RowAudit` | Cross-cutting — see rules above |
| ErrorHandling | 錯誤處理 | — (middleware + interceptor) | — | Cross-cutting — see rules above |

## gstack

Use the **`/browse`** skill for **all** web browsing. **Never** use `mcp__claude-in-chrome__*` tools.
(Available gstack skills are listed automatically every session — don't duplicate that list here.)
