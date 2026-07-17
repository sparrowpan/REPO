# Code Generation Patterns

Full backend + frontend conventions and app-shell gotchas. Read before adding or editing a feature.
CLAUDE.md carries only a short summary — the detail lives here.

## Backend

  ### Models
  - `{TABLE}.cs` — response model (nav objects for FKs, subquery counts for n-n)
  - `{TABLE}Request.cs` — write DTO (FK pkids only; n-n as `List<int>`)
  - `{TABLE}Query.cs` — search DTO (Keyword?, FK pkids?, bool fields?, date ranges?)

  ### Repository
  - `I{TABLE}Repository.cs` + `{TABLE}Repository.cs`; DI registration in `Program.cs`.
  - Dapper only, async (no EF). Connections via `IDbConnectionFactory` (`Infrastructure/`). Multi-map when JOINing nav objects.
  - `nchar` columns: always `RTRIM()` in SQL.
  - n-n: delete-then-reinsert in a transaction on create/update; read via a second query on the same connection.

  ### Controller
  - Route `/api/{tablePlural}`; `PUT` takes pkid/business-key from body (no route param).
  - String PKs: route `{id}` (no `:int` constraint); service uses `encodeURIComponent`.
  - `DateOnly`/`TimeOnly` fields: Dapper type handlers already registered in `Program.cs`.

  ### Tests
  - `CmsApiFactory` (`WebApplicationFactory<Program>`) swaps the repo for an in-memory fake — no SQL Server needed
    (`Program.cs` exposes `public partial class Program`). One `Fake{Table}Repository.cs` + `{Table}sControllerTests.cs` per feature.
  - `CmsApiFactory.CustomizeServices` re-overrides one service for a single test (e.g. a repo that throws — see
    the ErrorHandling section of `features.md`).
  - Protected endpoints: `CmsApiFactory.CreateAuthenticatedClient()` (pass role names for role-gated cases),
    not `CreateClient()`.

## Frontend

  ### Setup
  - Standalone components + signals; lazy `loadComponent` routes. `app.config.ts` providers: `provideRouter`,
    `provideHttpClient(withFetch())`, `provideAnimationsAsync`, `providePrimeNG` (Aura preset).
  - Path aliases (`tsconfig.json`): `@env/*`, `@core/*` → `src/app/core`, `@features/*` → `src/app/features`.
  - Environments: `environment.ts` (prod) / `.development.ts` (dev), swapped by `fileReplacements` in `angular.json`.
    `apiBaseUrl` is absolute — **no dev proxy**.

  ### Files
  - `features/{table-plural}/{table}-list/`, `{table}-detail/`, `{table}-form/`
  - `core/services/{table}.service.ts` (+ `.spec.ts`); shared FK/n-n option lists in `core/services/lookup.service.ts`.

  ### List page
  - Sortable/paginated `p-table` + filter drawer (`p-drawer`).
  - Session storage keys: `{table}-list-filters`, `{table}-list-sort`, `{table}-list-page`.
  - `p-select`/`p-multiselect` in drawer: always `appendTo="body"`; mapped `{ pkid, label }[]` getter; `[filter]="true"` for 10+ options.
  - `p-select`/`p-multiselect` with 100+ items: add `[virtualScroll]="true" [virtualScrollItemSize]="43"`.

  ### List inline edit (opt-in; Course is the reference)
  - Editable `<td>`s use `(dblclick)="startEdit(row, 'field')"` — single-click never edits; PK/FK-lookup columns
    stay read-only, plain text, no handler.
  - One shared `editValue` buffer bound via `[ngModel]="$any(editValue)"` + `(ngModelChange)` (satisfies `strictTemplates`);
    per-column editor (`pInputText`/`p-inputNumber`/`p-datepicker`/`p-select`/`p-checkbox`).
  - Persist on blur/change → `validate` → skip-if-unchanged → rebuild the full `{Table}Request` from the row → `update`.
    Validation failure shows an inline `editError` and keeps the cell open; save failure reverts (row signal is only
    mutated on success) + toast. FK-select edits refresh the nested lookup label from the lookup signal.
  - **Rebuilding the request from a list row only works if the row carries every field the request writes.**
    `PUT` is a full replace and n-n sync is delete-then-reinsert, so any n-n array the row is missing arrives
    as `[]` and **wipes those links** — silently, on an edit to an unrelated column, with a success toast, and
    n-n changes are not audited so nothing records what was lost. A list `SELECT` that returns only
    `{Nn}Count` (the natural shape) is exactly this trap. Either return the id arrays on the list query too
    (`CourseRepository.AttachLinkIdsAsync` — batched, not a subquery per row) or don't reuse the row as a
    write DTO. Fixed for Course in `9ef16b8`; regression: `CourseRepositoryLinkIdsTests.cs`.
    **The API fakes cannot catch this** — `Fake{Table}Repository` hands the ids back from list *and* detail,
    so it is more generous than the real SQL. Assert list-query shape against a real DB (see Backend → Tests).
  - **`p-datepicker` cells must not persist on raw `(onBlur)`.** Its overlay is `appendTo="body"`, so pressing the
    mouse on a date blurs the input *before* the click selects it — a plain blur handler sees the value unchanged,
    closes the cell, and destroys the panel mid-click, so the pick is silently lost. Wire
    `(onShow)`/`(onClose)` to track the panel and ignore any blur while it is open; commit on `(onSelect)` and
    `(onClose)`. Guards in `onEditBlur` (`!isEditing` / `savingCell`) keep the overlapping events to one save.
  - Cover date cells by driving the real event order (blur → `ngModelChange` → `onSelect` → `onClose`), not by
    calling `onEditBlur` directly — a direct call cannot reproduce the ordering bug above.

  ### Form page
  - Reactive Forms; `forkJoin` for parallel lookup calls on init; immutable business keys disabled in edit.
  - `p-datepicker`: convert ISO string ↔ `Date` on load/save. `[timeOnly]="true"` for `time` columns (`parseTime`/`toTimeStr` helpers).
  - `p-multiselect` for n-n: `[maxSelectedLabels]="9999"`; wrap chips via `::ng-deep`.
  - Sticky action toolbar (Course is the reference): the `.page-header` holding Save/Cancel is
    `position: sticky; top: 0; z-index: 20`, pinned to the top of the content region while the form body scrolls under it.
    `top: 0` (not `60px`) pins flush because `.content` (not the window) is the scroll container — see scroll model.

  ### App shell scroll model
  - The **window does not scroll**. `.content` and `.sidebar` (`app.scss`) are each their own bounded scroll region
    (`height: calc(100vh - $topbar-height); overflow-y: auto`) beneath the fixed 60px topbar.
  - Descendant `position: sticky` toolbars therefore pin against `.content`, not the window — keep `.content` a scroll
    container (any `overflow` ≠ `visible`) with a bounded height, or sticky descendants silently stop pinning.

  ### Sidebar nav
  - Nav entries in `app.ts` (`navSections` → groups → children), rendered by `app.html`; add under the appropriate group.
  - Shell follows PrimeNG **Ultima** (https://ultima.primeng.org): light topbar + sidebar, uppercase gray section
    titles, rounded items, primary-tinted pill for active. Theme tokens (`--p-surface/highlight/primary-*`) in `app.scss`.

## Special Types

  | Column type | Handling |
  |-------------|---------|
  | `nchar(n)` | `RTRIM()` in all SQL SELECTs |
  | `time(7)` | C# `TimeOnly` via `TimeOnlyTypeHandler`; display with `\| slice:0:5`; `p-datepicker [timeOnly]` in form |
  | `date` | C# `DateOnly` via `DateOnlyTypeHandler`; `p-datepicker` in form; bind raw — see Local time |
  | `datetime` | Local, unmarked — see Local time. Bind raw |
  | `smallint` PK | No special handling |
  | `nvarchar` PK (string) | Controller route `{id}` (no `:int`); service calls `encodeURIComponent(id)` |

  ### Local time — the whole app, both directions

  **Every timestamp this app stores is local.** Writes use `DateTime.Now` (`RowAuditWriter`,
  `AppUserRepository`) or `GETDATE()` (`ResetPasswordAsync`, `AuthRepository.ChangePassword`); only
  `JwtTokenService` uses `UtcNow`, for token lifetime. They serialize with **no timezone designator**
  (`"2026-07-17T13:44:13.29"`).

  - **Reading — bind the value raw.** An unmarked date-time parses as local, which is already correct.
    `{{ v | date: '...' }}`. `row-audit-badge.html` is the reference.
  - **Never append `'Z'`** (or otherwise mark it UTC). That re-converts local→local and renders **+8** for
    the Taipei team — a password reset at 13:44 showed 21:44. Fixed in `5a9d8ed`; regression:
    `app-user-list.regression-1.spec.ts`.
  - **Never `toISOString()`** when serializing a picked date — it converts to UTC first and shifts the **day**.
    Use `@core/utils/date.util` (`toIso`/`fromIso`, local components only).
  - Both directions are invisible at offset zero, so a **UTC CI cannot catch either** — these only
    discriminate where the offset is non-zero. Assert the input's own wall clock survives.

## API Endpoints

  | Method | Route | Description |
  |--------|-------|-------------|
  | GET    | `/api/{plural}` | All records |
  | POST   | `/api/{plural}/query` | Filtered search |
  | GET    | `/api/{plural}/{id}` | Single record |
  | POST   | `/api/{plural}` | Create |
  | PUT    | `/api/{plural}` | Update (pkid in body) |
  | DELETE | `/api/{plural}/{id}` | Delete |
  | GET    | `/api/lookups/{plural}` | Slim lookup list (if used as FK target) |

## Environment (dev)

  Run/test commands live in `CLAUDE.md`; these are the settings behind them.

  - **DB connection string** — `src/CMS.API/appsettings.json` → `ConnectionStrings:CMS`. That file is the
    source of truth; it currently points at `Server=.\SQLEXPRESS;Database=CMS;Trusted_Connection=True;`
    `TrustServerCertificate=True;Encrypt=False` (local SQL Express, no TLS — dev only).
  - **Ports** — API `http://localhost:5000` (via `launchSettings.json`), Angular `http://localhost:4200`.
  - **CORS** — `Program.cs` allows any loopback origin, which is a dev convenience, not a deployable policy.
  - **Tests need no database.** `CmsApiFactory` swaps every repo for an in-memory fake, so the backend suite
    runs anywhere (see Backend → Tests). Two suites are the exception, both running a **real** repository
    against in-memory **SQLite**: `PublishStatusRepositoryAuditTests` (audit rows) and
    `CourseRepositoryLinkIdsTests` (list-query shape). That only works for provider-portable SQL — a
    user-entered PK with no `SCOPE_IDENTITY()`, and no `'%' + @x + '%'` concatenation (SQLite uses `||`),
    which is why the Course suite covers `GetAllAsync` and not `QueryAsync`.
  - **Windows:** a running `CMS.API` locks `bin/Debug/net9.0/CMS.API.exe`, so `dotnet build`/`dotnet test`
    fails with `MSB3027`. Stop it first — `dotnet test` on `CMS.API.Tests` rebuilds the API and hits this
    too, even though the tests need no DB. `sqlcmd` against `.\SQLEXPRESS` needs `-C` (ODBC Driver 18
    rejects the cert otherwise).
