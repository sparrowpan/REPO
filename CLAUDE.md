# CLAUDE.md

**Index, not a manual — this file loads every session.** Put the detail in a reference file and point at
it from here; a fact in two places drifts.

## Overview

Full-stack CMS generated from a SQL Server schema. .NET 9 Web API (Dapper, **no EF**) + Angular 20
(standalone components) + PrimeNG. Bilingual 中文/English UI.

```
src/CMS.sln
  CMS.API/        .NET 9 API      — :5000 (/swagger)
  CMS.API.Tests/  xUnit           — in-memory fakes, no DB
  CMS.NG/         Angular+PrimeNG — :4200
```

## Reference files — read the one your task calls for

| File | Read it for |
|------|-------------|
| **`spec/code-gen.convention.md`** | **Before any feature work.** File layout, endpoints, column types, local-time rule, inline edit (+ its n-n wipe trap), form/shell gotchas, tests, Environment. |
| **`spec/features.md`** | **What exists** (index at the top) + as-built notes + the full **RowAudit / ErrorHandling / Auth** sections. Read your feature's section. |
| `spec/{schema}/{Table}.md` | Per-feature build spec; `spec/feature-spec.template.md` is the template. |
| `spec/custom/{Table}/` | Specs + mockups for non-triad features (e.g. FeaturedPromoItem's board). |
| `database/*.sql` | Source-of-truth schema: `auth`, `admin`, `course`, `promotion`. |
| `spec/ui-sample-*.png` | Style reference only — not literal content. |

## Run & test

```bash
cd src/CMS.API && dotnet run                          # :5000/swagger
dotnet test src/CMS.API.Tests/CMS.API.Tests.csproj    # no DB needed
cd src/CMS.NG && npm install && npm start             # :4200
npm test                                              # CI: npx ng test --watch=false --browsers=ChromeHeadless
```

Windows: a running `CMS.API` locks its `.exe` — stop it before `dotnet build`/`test`.

## Building a feature

Copy **AppRole**, the reference feature; read `spec/code-gen.convention.md` first. Printing is separate
from the app shell (global `@media print` undo in `styles.scss` — see **CourseBrochure** in
`spec/features.md`). When done, add an index row + as-built section to `spec/features.md`.

## Cross-cutting rules — every feature, no exceptions

Triggers only. The full rule, and each way it silently breaks, is the same-named section in
`spec/features.md`. **Read it before you touch the area.**

- **RowAudit** — each CRUD Insert/Update/Delete writes one audit row via `Services/RowAuditWriter.cs` on the
  operation's **own `conn` + `tx`**; every detail/form page carries `<row-audit-badge>`.
- **ErrorHandling** — `authInterceptor` owns the **single** 5xx toast; every component `error:` handler must
  start `if (isServerError(err)) return;` (`@core/utils/http-error.util`) or one failure toasts twice. It
  never toasts 4xx — a 400/409 message reaches the user only if the component surfaces `err.error?.message`.
- **Auth** — global `RequireAuthenticatedUser` guards every controller; every feature route needs
  `canActivate: [authGuard]`; anything administering users/roles/permissions **also** needs class-level
  `[Authorize(Roles = "Admin")]` (sidebar `requiresAdmin` is presentation, the API is reachable directly).
- **Local time** — every stored timestamp is local and serialized unmarked. Bind it **raw**; never append
  `'Z'` and never `toISOString()` (both shift +8 / roll the day). Details in the convention → Local time.

## gstack

Use the **`/browse`** skill for **all** web browsing; **never** `mcp__claude-in-chrome__*`.
(Available skills are listed each session — don't duplicate that list here.)
