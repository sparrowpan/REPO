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
