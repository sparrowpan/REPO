# Implemented features (as-built notes)

Per-feature implementation notes for features already built. Read the relevant section only when a
task touches that feature. Build **new** features from `spec/code-gen.convention.md` + the per-feature
build spec (`spec/{schema}/{Table}.md`), then append an as-built section here. Keep CLAUDE.md's
feature index in sync (one line per feature).

## AppRole (角色) — reference feature

First implemented feature — copy its structure for new tables. Schema: `database/auth.sql`.

- `AppRole` PK is the string `RoleId` (clustered key; `pkid` is a surrogate IDENTITY shown as 主代碼).
  `RoleId` is entered on add, immutable on edit, and is the route id (`encodeURIComponent`).
- n-n `AppRole ↔ AppUser` via `AppUserRole` (keyed on `UserId`/`RoleId`). List shows `userCount`
  (subquery); detail/form load the user select from `GET /api/lookups/appusers` (label `UserName (UserId)`).
- Sidebar: **系統管理 Admin → 角色 AppRole** (`/app-roles`).
- Backend: `Controllers/AppRolesController.cs`, `Repositories/AppRoleRepository.cs`, `Models/AppRole*.cs`.
  Frontend: `features/app-roles/*`, `core/services/app-role.service.ts`, `core/services/lookup.service.ts`.
- Tests: `CMS.API.Tests/AppRolesControllerTests.cs` (13); `app-role-*.spec.ts` + `app-role.service.spec.ts` (Angular).

## AppUser (使用者)

Login accounts. Schema: `database/auth.sql`. Mirrors AppRole (string business PK, surrogate `pkid`,
n-n via delete-then-reinsert). Spec: `spec/auth/AppUser.md`.

- `AppUser` PK is the string `UserId` (clustered; `pkid` is the surrogate IDENTITY shown as 主代碼).
  `UserId` is entered on add, immutable on edit, and is the route id (`encodeURIComponent`).
- n-n `AppUser ↔ AppRole` via `AppUserRole`. List shows `roleCount` (subquery); detail/form load the
  role multiselect from `GET /api/lookups/approles` (label `RoleName (RoleId)`) — the mirror of
  AppRole's `appusers` lookup.
- **`PasswordHash` is backend-only** — excluded from `AppUserRequest`, the `AppUser` response model, and
  all Angular models. On create it is derived from `SysConfig` (`configKey='appConfig'` → JSON
  `defaultPassword`, SHA-256 → lowercase hex; fallback constant if missing/malformed) and stamps
  `PasswordUpdatedTime`. `PUT` never touches it. It changes only via
  `POST /api/appusers/{id}/reset-password` (204/404) — **`[Authorize(Roles = "Admin")]`**, so a non-Admin
  caller gets `403`. No password/hash crosses the boundary (client sends only the UserId in the route; the
  server reads `defaultPassword` from SysConfig, hashes it, and returns a bare 204). Surfaced as a 重設密碼
  button on the **detail** page and a 重設密碼為預設值 button on the **edit form**; both are shown only when
  `auth.hasRole('Admin')` (the form button also only in edit mode).
- Query filters: keyword (UserId, UserName) + tri-state `IsActive` (`p-select` 全部/啟用/停用).
  `PasswordUpdatedTime` displayed with the `+ 'Z'` UTC fix. `IsActive` edited via `p-checkbox [binary]`.
- Sidebar: **系統管理 Admin → 使用者 AppUser** (`/app-users`) — nav entry pre-existed in `app.ts`.
- Backend: `Controllers/AppUsersController.cs`, `Repositories/AppUserRepository.cs`, `Models/AppUser*.cs`,
  `Models/AppRoleLookup.cs`. Frontend: `features/app-users/*`, `core/services/app-user.service.ts`.
- Tests: `CMS.API.Tests/AppUsersControllerTests.cs` (incl. reset-password: Admin 204, non-Admin 403,
  anonymous 401, hash = `SHA256(defaultPassword)` + `PasswordUpdatedTime` moved, no password/hash in the
  response) + `FakeAppUserRepository.cs` (models the reset hash via `GetPasswordHash`);
  `app-user-*.spec.ts` (button visibility per role) + `app-user.service.spec.ts` (Angular).

## FeaturedPromoItem (上稿作業)

Customized (non-CRUD-triad) weekly scheduling **board** for the home page. Schema: `database/promotion.sql`.
Build spec: `spec/custom/FeaturedPromoItem/FeaturedPromoItem.spec.md` (+ `ui-query/ui-update/ui-new.spec.png`).

- **Board, not list/detail/form.** One component `features/featured-promo-items/featured-promo-item-board/`
  renders TrainingCenter **tabs** × a Monday–Sunday **week** × **3 slots** per day, with inline
  Edit / New / Copy / Paste / Delete and slot `+` / `−` move. No detail/form pages, no route params.
- Data model: `pkid` (IDENTITY), `ScheduleOn` (`date`→`DateOnly`), `TrainingCenter_pkid` (`smallint`→`short`),
  `Slot` (`tinyint`→`byte`, 1–3), `Promotion_pkid` (`int` FK), `Topic`, `Description`. Response joins
  `Promotion2.PromoCode` (flat column, no nav object). Unique index `(ScheduleOn, TrainingCenter, Slot)`.
- **PromoCode lookup**: the Edit form has the user type a `Promotion2.PromoCode`; `resolvePromoCode()`
  matches it against `GET /api/lookups/promotions` (pkid + PromoCode + Topic + Description) to set
  `Promotion_pkid` and default blank Topic/Description. Tabs come from `GET /api/lookups/training-centers`.
- **Slot move** (`+`/`−`): `POST /api/featured-promo-items/move` `{ pkid, targetSlot }` swaps with any
  occupant of the target slot in a transaction via a temp slot (0), honoring the unique index.
- Query: `POST /query` with `TrainingCenterPkid` + `ScheduleOnFrom`/`ScheduleOnTo` (the visible week).
- Sidebar: **首頁 Home → 上稿作業 FeaturedPromoItem** (`/featured-promo-items`) — new child added to the
  previously-empty Home group (relabeled from 首頁管理 to 首頁).
- Backend: `Controllers/FeaturedPromoItemsController.cs` (adds `/move`), `Repositories/FeaturedPromoItemRepository.cs`,
  `Models/FeaturedPromoItem*.cs`, `Models/TrainingCenterLookup.cs`, `Models/PromotionLookup.cs`; lookups extended.
  Frontend: `features/featured-promo-items/*`, `core/services/featured-promo-item.service.ts`, `LookupService`
  (+`getTrainingCenters`/`getPromotions`), `core/models/{featured-promo-item,training-center-lookup,promotion-lookup}.model.ts`.
- Tests: `CMS.API.Tests/FeaturedPromoItemsControllerTests.cs` (17: one-week filter, center filter, PromoCode
  lookup, CRUD, move) + `FakeFeaturedPromoItemRepository.cs` + `FakeLookupRepository.cs`;
  `featured-promo-item-board.spec.ts` (list/tabs/week-nav/Edit/New/Paste/Copy/Delete/Move),
  `featured-promo-item.service.spec.ts`, `lookup.service.spec.ts` (Angular).

## Auth (登入) — login / JWT + end-to-end authorization

Login API + JWT bearer authorization across the stack. Authenticates against `AppUser`, issues a JWT,
and protects every endpoint/route. Schema: `database/auth.sql` (`AppUser`, `AppUserRole`, `SysConfig`).

- **`POST /api/Auth/login`** `{ userId, password }` → `200` `{ userId, userName, accessToken }`, or `401`
  `{ message: "invalid credentials" }` for **every** failure (unknown UserId, `IsActive = 0`, or wrong
  password) so the response never reveals which check failed. `PasswordHash` is never returned.
- **Credential check** (Dapper, `AuthRepository`): fetch UserId/UserName/IsActive/PasswordHash by exact
  `UserId` regardless of IsActive, then in the controller require `IsActive` **and**
  `SHA256(password)` == stored hash. Hashing is lowercase hex via `Services/PasswordHasher` — the same
  scheme `AppUserRepository` stores (`Convert.ToHexStringLower(SHA256…)`); compared with
  `CryptographicOperations.FixedTimeEquals`.
- **JWT** (`Services/JwtTokenService`, `System.IdentityModel.Tokens.Jwt`): HMAC-SHA256, 24h lifetime
  (`TokenLifetime`), claims `userId`, `userName`, and one `ClaimTypes.Role` per RoleId in `AppUserRole`.
  Signing secret is read **at runtime** (not hard-coded) from `SysConfig` (`configKey='appConfig'` → JSON
  `symmetricSecurityKey`); missing/malformed secret → `500` (server misconfig, distinct from a bad login).
- **`PUT /api/Auth/profile`** `{ userName }` → `200` `{ userId, userName }`. **Protected** (no
  `[AllowAnonymous]`): the target user is the JWT `userId` claim (`User.FindFirstValue("userId")`), never
  the request body — any `UserId` in the body is ignored, so a user can only rename **themselves**, and
  roles/password/`IsActive` are untouched. `UserName` is `[Required]` + trimmed (empty/whitespace → `400`);
  unknown user → `404`. Repo: `AuthRepository.UpdateUserNameAsync` (`UPDATE AppUser SET UserName …`).
- **`POST /api/Auth/change-password`** `{ currentPassword, newPassword, confirmNewPassword }` → `200`.
  **Protected**; acts on the JWT `userId` only (no id in the body — you can only change your own password).
  **Plain-text passwords only cross the wire, never a hash, in either direction.** Checks in order: (1)
  `SHA256(currentPassword)` must equal the stored `PasswordHash` (`PasswordHasher.Verify`) else `400`; (2)
  new-password complexity via `Services/PasswordPolicy` — length ≥ 8 **and** ≥ 3 of 4 classes (upper / lower /
  digit / symbol), else `400` with the bilingual `PasswordPolicy.ComplexityMessage`; (3) `newPassword ==
  confirmNewPassword` else `400`. On success `AuthRepository.UpdatePasswordAsync` sets `PasswordHash =
  SHA256(new)` and `PasswordUpdatedTime = GETDATE()`. The session/token is unchanged (still valid).
- **Authorization (backend)**: JWT bearer auth (`Microsoft.AspNetCore.Authentication.JwtBearer`) with a
  global **fallback policy** `RequireAuthenticatedUser` — every controller is protected. `AuthController`
  is no longer blanket-anonymous: only its **`login`** action carries `[AllowAnonymous]`; `profile` (and any
  future action) is protected by the fallback policy. No token / invalid token / expired token → `401`.
  The validation signing key is resolved lazily from the same `SysConfig` secret via
  `Services/SigningKeyProvider` (`IssuerSigningKeyResolver`, cached; keeps issuing ⇄ validating in sync).
  Tokens carry no issuer/audience, so only signature + lifetime are validated. `app.UseAuthentication()`
  precedes `app.UseAuthorization()`; Swagger middleware runs before auth so it stays reachable in dev.
- **Frontend auth** (`CMS.NG`): `core/services/auth.service.ts` stores the profile in **session storage**
  (`cms-auth`) as a signal; `roles` are decoded from the JWT's `ClaimTypes.Role` claim (no extra API call).
  `core/interceptors/auth.interceptor.ts` attaches `Authorization: Bearer <token>` to API requests and, on
  any 401 (except the login call), clears the session and redirects to `/login`.
  `core/guards/auth.guard.ts` blocks routes without a token (→ `/login`); every feature route has
  `canActivate: [authGuard]`, `login` stays public. `features/auth/login/` is the public login page.
  The app shell (`app.ts`/`app.html`) renders only when authenticated (bare `<router-outlet>` otherwise),
  shows the signed-in `userName` in a topbar **user menu** (`p-menu` popup: **個人資料 My Profile** →
  `/profile`, and logout), and hides the **系統管理 Admin** group unless `roles` include `Admin`
  (`visibleSections` computed). Interceptor wired in `app.config.ts`.
- **My Profile page** (`features/profile/`): reads `userId` + `roles` **read-only** from `AuthService`
  (session/JWT, no API call) and edits only `userName` (Reactive Forms, required + trimmed). On save calls
  `AuthService.updateProfile(userName)` → `PUT /api/Auth/profile`, which refreshes `userName` in the session
  profile signal + session storage so the topbar updates live. Route `/profile` has `canActivate: [authGuard]`.
  A second **變更密碼 Change Password** card (own `passwordForm`) takes current / new / confirm passwords and
  validates client-side to mirror the server: `passwordComplexityValidator` (length ≥ 8 + ≥ 3 of 4 classes,
  message = `PASSWORD_COMPLEXITY_MESSAGE`, kept in sync with `PasswordPolicy.ComplexityMessage`) and a
  group-level `passwordsMatchValidator`. On success `AuthService.changePassword(...)` →
  `POST /api/Auth/change-password`; the form resets and a server-side rejection (e.g. wrong current password)
  surfaces `err.error.message` in a toast. No session change on success.
- Backend files: `Controllers/AuthController.cs` (`login` = `[AllowAnonymous]`, `profile` protected),
  `Repositories/{IAuthRepository,AuthRepository}.cs`,
  `Services/{IJwtTokenService,JwtTokenService,PasswordHasher,PasswordPolicy,SigningKeyProvider}.cs`,
  `Models/{LoginRequest,LoginResponse,LoginCredential,UpdateProfileRequest,ProfileResponse,ChangePasswordRequest}.cs`;
  DI + auth pipeline in `Program.cs`; JWT packages in `CMS.API.csproj`.
- Tests — backend: `AuthControllerTests.cs` (9) + `AuthProfileControllerTests.cs` (6: renames the JWT user,
  ignores a `UserId` in the body, rejects empty + whitespace names, trims, 401 without a token) +
  `AuthChangePasswordControllerTests.cs` (6: wrong current password changes nothing, complexity enforced —
  too short + too few classes, new/confirm mismatch, valid change sets `PasswordHash = SHA256(new)` and
  advances `PasswordUpdatedTime`, 401 without a token) +
  `AuthorizationTests.cs` (6); `CmsApiFactory.CreateAuthenticatedClient()` / `CreateAuthenticatedClientAs(userId,…)`
  mint valid tokens (the latter targets a specific seeded user); `Fakes/FakeAuthRepository.cs` supplies the
  fixed signing secret + active `helen` / inactive `miles` and in-memory `UpdateUserNameAsync` /
  `UpdatePasswordAsync` (+ `GetPasswordHash` / `GetPasswordUpdatedTime` test hooks).
  Frontend: `auth.service.spec.ts`, `auth.interceptor.spec.ts` (Bearer header + 401 clears session/redirects),
  `auth.guard.spec.ts` (redirect when no token), `login.spec.ts`, `profile.spec.ts` (UserId/roles read-only;
  save PUTs + refreshes shell/session; trims; skips API when blank; **change-password client validation**:
  empty fields, length < 8, < 3 classes, valid 3-class password, confirm mismatch, and a valid POST carrying
  no hash), and `app.spec.ts` (Admin group only with the `Admin` role; shell hidden when signed out).

## RowAudit (異動記錄) — cross-cutting row auditing

Cross-cutting audit trail: every business-table Insert / Update / Delete writes one row to the
`RowAudit` table (`dbo.RowAudit`). Not a list/detail/form feature — it is a service repositories call.

- **`Services/RowAuditWriter.cs`** — the single reusable writer, generic over any entity via reflection.
  Registered `AddScoped<RowAuditWriter>()`; needs `IHttpContextAccessor` (`AddHttpContextAccessor()` in
  `Program.cs`). API: `LogInsert(table, entity, conn, tx?, ct)`, `LogUpdate(table, before, after, conn, tx?, ct)`,
  `LogDelete(table, entity, conn, tx?, ct)`.
- **Writes on the caller's connection + transaction** — the audit INSERT runs inside the same transaction as
  the change, so a rolled-back / failed change leaves no audit row. The writer opens no connection of its own.
- Column mapping (writer → `RowAudit`): `TableName` = the real table name passed in; `UserName` = JWT
  `userName` claim (falls back to `ClaimTypes.Name`, then `"system"` when unauthenticated); `PrimaryKeyValues`
  = the entity's `pkid` (found case-insensitively by reflection) as a string; `ActionType` =
  `Insert|Update|Delete`; `ActionDesc` per rule below (truncated to `ActionDescMaxLength` = 1000);
  `[DateTime]` = `DateTime.Now` (mapped from the model's `ActionTime` property). `pkid` is IDENTITY — never inserted.
- **`ActionDesc`**: Insert / Delete = the entity's **first string property** in declaration order (metadata-token
  ordered), e.g. `Title` / `Name` / `Description`. Update = comma-separated **names of the changed properties**;
  when nothing changed the writer returns null and **no row is written**.
- **Change detection compares scalar (column-like) properties only** — primitives, enums, `string`, `decimal`,
  `DateTime`/`DateOnly`/`TimeOnly`, `Guid`, and their nullables. Navigation objects, collections (n-n id/label
  lists), and derived counts are ignored (they reference-compare unequal and aren't real columns).
- **Repository wiring pattern** — **copy `Repositories/AppRoleRepository.cs`**, the reference implementation.
  Across all seven CRUD repos (AppRole, AppUser, PublishStatus, Partner, CourseGroup,
  Course, FeaturedPromoItem): each Create/Update/Delete runs in a transaction. Insert → load the new row
  *inside the tx* (a post-commit reload can't see it yet) → `LogInsert` → commit. Update → load `before`
  (scalar snapshot) → apply → load `after` → `LogUpdate` → commit; returns `false` (rolled back) if the row is
  missing. Delete → load the row (for its first string column) → delete → `LogDelete` → commit. Entities whose
  read SELECT carries nav joins / count subqueries (Course, AppRole, AppUser) use a dedicated **own-columns-only**
  `AuditSelectColumns` for the before/after snapshots so the diff stays limited to real table columns.
- **Not audited (custom, non-triad ops):** `AppUserRepository.ResetPasswordAsync` and
  `FeaturedPromoItemRepository.MoveToSlotAsync`. They aren't standard Insert/Update/Delete (reset touches the
  backend-only `PasswordHash`; move swaps up to two rows), so they're intentionally left for a follow-up.
- **History viewer (read side).** `GET /api/rowaudit?tableName={T}&pkid={n}` returns one record's audit trail,
  newest first — a lean `RowAuditEntry` projection (`DateTime`, `UserName`, `ActionType`, `ActionDesc`; the
  `TableName`/`PrimaryKeyValues` keys are the query, not the response). `pkid` is the record's **surrogate**
  pkid (matched against `PrimaryKeyValues`, stored as text). Missing `tableName` → `400`; a record with no
  history → `200` `[]`. Backend: `Controllers/RowAuditController.cs`, `Repositories/{IRowAuditRepository,RowAuditRepository}.cs`,
  `Models/RowAuditEntry.cs`; `ORDER BY [DateTime] DESC, pkid DESC` (pkid tiebreaker for same-timestamp rows).
  Protected by the global auth policy like every other controller.
- **Reusable `RowAuditBadge`** (`core/components/row-audit-badge/`, selector `row-audit-badge`): a standalone
  badge with inputs `tableName` + `pkid`. An `effect` (re)fetches the trail (`core/services/row-audit.service.ts`)
  whenever the target record changes — so a form that loads its record after init works — and shows the
  **latest** change inline on the badge (`{ActionType} by {UserName} · {date}`); clicking opens a
  `p-dialog` listing the full trail newest-first. Neutral "no history" state when the trail is empty or `pkid` is
  falsy (an unsaved new record) — the latter skips the fetch. Its own `error:` handler clears state silently;
  the interceptor owns the toast (see ErrorHandling). Placed **first inside**
  `<div class="page-header__actions">` — a plain div, **not** a PrimeNG toolbar slot — on **every** detail and
  form page (AppRole, AppUser, PublishStatus, Partner, CourseGroup, Course), passing that page's table name and
  the current record's pkid (detail: `record()?.pkid ?? 0`; form: an `auditPkid` signal set on edit-load, 0 in
  add mode). Example — `app-role-detail.html`:
  `<row-audit-badge tableName="AppRole" [pkid]="role()?.pkid ?? 0" />`.
- Tests — backend: `RowAuditControllerTests.cs` (5: filters by tableName + pkid newest-first, excludes a
  same-pkid different-table row, empty trail → `[]`, missing tableName → `400`, no token → `401`) +
  `Fakes/FakeRowAuditRepository.cs` (seeds out-of-order rows across tables/pkids). Frontend:
  `row-audit-badge.spec.ts` (latest shown inline on load, dialog lists the full trail newest-first, "no history"
  empty state, and no fetch when pkid is falsy) + `row-audit.service.spec.ts`. The 12 existing detail/form specs
  gained `provideHttpClient()`/`provideHttpClientTesting()` so the embedded badge can resolve `HttpClient`.
- Tests — `RowAuditWriterTests.cs` (12): reflection logic (first-string `ActionDesc` for Insert/Delete incl.
  declaration order, changed-name list + empty-when-unchanged for Update, `PrimaryKeyValues` from pkid,
  `UserName` → `"system"` fallback, 1000-char truncation). `PublishStatusRepositoryAuditTests.cs` (7): runs the
  **real** `PublishStatusRepository` against an in-memory **SQLite** DB (its SQL is provider-portable — user-entered
  PK, no `SCOPE_IDENTITY()`) with a real `RowAudit` table, asserting the actual rows: Insert/Update (exact changed
  columns) / Delete, no-op update writes nothing, and failed changes (missing row, duplicate-PK insert) leave no
  audit row. Needs no SQL Server; the `Microsoft.Data.Sqlite` package is a test-only dependency.

## ErrorHandling (錯誤處理) — cross-cutting exception handling

One generic error response for any unhandled server failure, and one toast for it in the UI. Not a
list/detail/form feature — middleware on the backend, interceptor policy on the frontend.

- **`Middleware/ExceptionHandlingMiddleware.cs`** — registered **first** in `Program.cs`
  (`app.UseMiddleware<ExceptionHandlingMiddleware>()`), so it wraps the whole pipeline. Catches anything a
  controller or repository lets escape, logs it via `ILogger` at Error with the message + stack trace + a
  trace id, and replies `500` with `Models/ErrorResponse.cs` — `{ "message", "traceId" }`, camelCase.
  `message` is always the constant `ErrorResponse.GenericMessage`; **stack traces, SQL, and connection
  strings never cross the wire.** `traceId` is `Activity.Current?.Id` (the W3C traceparent), so a user's
  report maps to the logged exception.
- **Because the middleware handles them, controllers write no try/catch for *unexpected* errors** and never
  return raw exception text — let it escape and the middleware does the rest. Catch only what you can improve
  on for the caller (400, 409).
- **Being registered first also puts it *inside* the Developer Exception Page** that minimal hosting adds
  automatically in Development — an exception unwinds to the innermost handler, so this middleware answers
  the client in every environment. Tests rely on that (`CmsApiFactory` forces `Development`).
- **It only sees exceptions.** 401 challenges, 403 forbids, and `[ApiController]` ModelState 400s are
  produced without throwing, so they pass through untouched — 401/403 keep their empty bodies +
  `WWW-Authenticate`, validation keeps its `ValidationProblemDetails` (`application/problem+json`), and
  controllers' own `{ message }` 400/404/409 bodies are unchanged. Anything already meaningful stays as-is.
- `Response.Clear()` drops a partially-staged response before writing; CORS headers survive it because the
  CORS middleware applies them from an `OnStarting` callback. A client disconnect
  (`OperationCanceledException` + `RequestAborted`) unwinds quietly rather than logging an error, and a
  response whose headers already started is rethrown rather than corrupted.
- **Frontend — `core/interceptors/auth.interceptor.ts` owns 5xx reporting.** On a 5xx from the API it raises
  one `MessageService` toast (severity `error`, summary `錯誤 Error`) carrying the server's safe `message`,
  falling back to `FALLBACK_ERROR_MESSAGE` when the body has none. Only a **string `message` on an object
  body** is displayed — a proxy's HTML error page arrives as a raw string and is never rendered. 401 still
  clears the session + redirects (and does not toast); 400/409 are untouched and still surface on the form.
  The error is always re-thrown, so components still react.
- **Root toast host**: `MessageService` is provided in `app.config.ts` and `<p-toast />` sits in `app.html`
  **outside** the `@if (auth.isAuthenticated())`, so a 5xx surfaces on the login screen too. `App` must not
  declare `providers: [MessageService]` or it would shadow the root instance the interceptor injects.
  Feature components keep their own scoped `MessageService` + `<p-toast />` for their own messages.
- **Components must not report 5xx themselves.** `core/utils/http-error.util.ts` exports `isServerError(err)`
  (the same predicate the interceptor uses). Every component `error:` handler runs its state cleanup first,
  then `if (isServerError(err)) return;` before its `messages.add(...)` — otherwise one failure toasts twice.
  Applied across all 18 feature components (39 handlers). **Follow this in any new feature.** Exception:
  `features/auth/login/login.ts` reports inline via an `error` signal, not a toast, and is left as-is.
- Tests — backend: `ExceptionHandlingTests.cs` (10: generic 500 body + shape, no SQL/stack/connection leak,
  every verb, full exception logged with the client's trace id, and 401/403/validation-400/`{ message }`-400/
  200 all unchanged). `CmsApiFactory.CustomizeServices` swaps in `Fakes/ThrowingPublishStatusRepository.cs`
  (whose message deliberately contains SQL + a password) and `Fakes/RecordingLoggerProvider.cs` captures logs.
  Frontend: `auth.interceptor.spec.ts` (500 toasts the safe message + no redirect, 503 too, fallback when the
  body has no message, HTML body never rendered, 401 redirects + does not toast, 400/409 do not toast) and
  `app.spec.ts` (toast host renders when signed out).

## CourseBrochure (課程簡介) — client-facing print document

Frontend-only. **No API surface, no route, no service call** — the brochure renders from the `Course`
`course-detail` has already loaded. `course-qr-code` is the precedent for this shape.

- **Files** — `features/courses/course-brochure-print/` (`.ts` / `.html` / `.scss` / `.spec.ts`) +
  `core/utils/prose-html.util.ts`. Touches `course-detail.{ts,html,scss}`, `course-qr-code.ts`
  (extracted `publicCourseUrl()`), and **global `styles.scss`**.
- **Flow** — 課程簡介 in `.page-header__actions` toggles an on-screen **A4 preview**; a 列印 button then
  calls `window.print()`. Printing is deliberately gated on the preview: the brochure is only in the DOM
  while it is open, so the QR code's async data URL and the CJK glyphs resolve *before* the print dialog
  (a print-only `display:none` component would race both), and Ctrl+P with the preview closed behaves
  exactly as it did before this feature.
- **The admin cards leave the DOM** while the preview is open (`@if (!showBrochure())`) rather than being
  hidden by a print rule. A CSS hide regresses silently and the failure mode is a client receiving a page
  of 主代碼 / 備註. `.page-header` + `p-toast` stay mounted for on-screen use and are hidden via
  `@media print` in `course-detail.scss`.

### Prose is not plain text — `core/utils/prose-html.util.ts`

Measured against the live catalogue (1,085 courses): **~10-13% of prose values carry benign HTML** —
`Objective` 137/1082, `Outline` 112/1082, `TowardCertOrExam` 82/924, with ~224 `<a>` in total. There is
**no `<script>`, `<img>`, `<table>` or `<span>` anywhere**. The rest is plain text whose only structure is
newlines. `course-detail` renders all of it with `white-space: pre-wrap`, so the admin page shows operators
literal `<br />` and `<a href>` tags — harmless there, unacceptable in a client document.

`toProseHtml(value)` returns HTML for `[innerHTML]`, or `null` meaning **omit the section entirely**:
- tag-bearing → passed through for Angular's `DomSanitizer` to strip at bind time. **Never
  `bypassSecurityTrustHtml`** — the sanitizer is the whole safety story.
- plain text → escaped **first** (`&` before the rest), then `\n` → `<br>`.
- **Absence is tested by content, never by length.** 639 of 1,076 `Material` values are under ten
  characters and every one is real, so a minimum-length rule would delete most of that column. Five
  courses do hold placeholder junk in `Outline` ("Test", "string", "00") — a data-quality issue, not
  something the renderer guesses at.

### The print cascade — three stylesheets, one surface

- **`styles.scss` (global) — the shell-undo, and `!important` is load-bearing.** `app.ts` sets
  `styleUrl: './app.scss'` with no encapsulation override, so `.content` compiles to
  `.content[_ngcontent-%COMP%]` — **(0,2,0)**. A global `.content` print rule is (0,1,0) and `@media`
  adds no specificity, so **without `!important` the block silently does nothing** and a multi-page
  brochure clips to the first viewport. Override `overflow` (shorthand) — `.content` sets **both**
  `overflow-y` and `overflow-x`. This cannot live in a component stylesheet: `.layout` / `.content` /
  `.topbar` / `.sidebar` are ancestors, and `::ng-deep` only pierces downward.
- **`@page { size: A4; margin: 14mm 16mm }`** in `styles.scss` (not a component — `@page` there is a
  ShadowCss bet). Non-zero margin is what gives pages 2+ their top margin; `margin: 0` suppresses the
  browser's headers/footers but takes the page-2 margin with it. Trade-off: Chrome draws its own
  header/footer until the rep unticks it once.
- **`course-brochure-print.scss`** — `break-inside: avoid` on sections (but `auto` on 課程大綱: 48 courses
  exceed 2,000 chars and it *must* paginate), `break-after: avoid` on headings,
  **`-webkit-print-color-adjust: exact`** (the prefix is required) on chips/panels or browsers drop the
  backgrounds and the credibility markers print as plain black text. Anchors print as inherited-colour
  text — a link is dead on paper, so the label stays and the affordance goes; the QR is the way back.
  CJK-first font stack + 10.5pt / 1.75 leading / 38em measure, because the app stack (`styles.scss:9`)
  puts `Segoe UI` first and CJK would fall back per glyph.
- **`::ng-deep` is used once, correctly**: `course-qr-code`'s CourseId caption (an excluded admin field)
  and download button are hidden inside the brochure. Those nodes carry the QR component's own
  encapsulation attribute, so a plain descendant selector would never match.

### Content contract

**Included** — `title`, `officialTitle`, `objective`, `target`, `prerequisites`, `outline`, `material`,
`towardCertOrExam`, `hour`, `certifications[]`, `jobCategories[]`, `partner.name`, rep name
(`AuthService.userName` — free from the session; **email/phone are not on `AuthProfile`**).
**Excluded (admin)** — `pkid`, `displayOrder`, `courseId`, `prodCourseId`, `friendlyUrl`, `publishStatus`,
`scheduleOn`, `scheduleOff`, `courseGroup`, `row-audit-badge`.
**Excluded (unresolved)** — `note`, `otherInfo`, `listPrice`, `learningCredit`, `canRepeat`.
**Adding a field here is a decision, not a default.**

Sections are ordered for the reader, not the admin DOM: hero → 原廠 → 時數/對應認證 → 適合對象/先修條件
→ 課程目標 → 課程大綱 → 教材 → 對應認證考試 → 職務類別 → rep + QR footer. Required set is
課程名稱/課程目標/課程大綱/時數; missing any raises a **screen-only** warning banner (hidden in print).

### Cross-cutting

- **RowAudit — no exception claimed.** The brochure does no Insert/Update/Delete and is not a page;
  `course-detail` keeps its badge and the print rule hides it. (An earlier design claimed an exception
  here; it never needed one.)
- **Auth** — no new route, so no `authGuard` entry. **ErrorHandling** — no new error path; the brochure
  renders only inside `@if (course(); as c)` and inherits `course-detail`'s load/404/5xx handling.
- Tests — `course-brochure-print.spec.ts` (contract fields render, admin fields absent, NULL/whitespace
  omits the whole section with no `—`, required-field warning, markup renders as structure, `<script>` is
  sanitized, plain-text newlines survive, rep + public URL) on a **complete 33-field `Course` fixture** —
  `course-detail.spec.ts`'s `as unknown as Course` leaves **19 fields undefined**, so a brochure bound to
  it would render `undefined` while the suite stayed green. **Do not copy that cast.**
  `prose-html.util.spec.ts` (both branches + the length trap). `course-detail.spec.ts` adds preview
  toggling, admin cards leaving the DOM, 列印 → `window.print()`, and the title being set for Chrome's
  Save-as-PDF filename **and restored on destroy** (otherwise it leaks to every later route).
- **Known gaps** — v1 is **unbranded** (no logo asset; `public/` holds only `favicon.ico`; a bundled CJK
  webfont would fail `angular.json`'s 1MB `initial` budget). **Print CSS has no standing automated
  coverage** — Karma cannot emulate print media. P1/P2/P3 (admin DOM hidden, shell-undo specificity,
  multi-page no clip) were **manually verified 2026-07-16** via `/browse` PDF generation against live
  data: course 3334 (`Outline` 4,413 chars) printed **7 A4 pages** and 3233 (bare-`<li>` markup, 3,277
  chars) printed **3** — both would be a single clipped page if the `!important` shell-undo had lost to
  `.content[_ngcontent]`'s (0,2,0). CJK rendered with no 豆腐 and chip/panel backgrounds survived. There
  is still **no committed print-media test**; a regression in the cascade would ship silently. The
  content contract is
  **unvalidated** — no rep was asked what they actually send a client. And the brochure structurally
  **cannot state when the course runs, where, or what it costs this client**: `course.sql` has no
  session/class-date table, `TrainingCenter` has no FK to Course, and `ListPrice` is a catalog price.
