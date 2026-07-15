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
  | `date` | C# `DateOnly` via `DateOnlyTypeHandler`; `p-datepicker` in form |
  | `smallint` PK | No special handling |
  | `nvarchar` PK (string) | Controller route `{id}` (no `:int`); service calls `encodeURIComponent(id)` |

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
