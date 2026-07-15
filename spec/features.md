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
  `POST /api/appusers/{id}/reset-password` (204/404), surfaced as a 重設密碼 button on the detail page.
- Query filters: keyword (UserId, UserName) + tri-state `IsActive` (`p-select` 全部/啟用/停用).
  `PasswordUpdatedTime` displayed with the `+ 'Z'` UTC fix. `IsActive` edited via `p-checkbox [binary]`.
- Sidebar: **系統管理 Admin → 使用者 AppUser** (`/app-users`) — nav entry pre-existed in `app.ts`.
- Backend: `Controllers/AppUsersController.cs`, `Repositories/AppUserRepository.cs`, `Models/AppUser*.cs`,
  `Models/AppRoleLookup.cs`. Frontend: `features/app-users/*`, `core/services/app-user.service.ts`.
- Tests: `CMS.API.Tests/AppUsersControllerTests.cs` (14) + `FakeAppUserRepository.cs`;
  `app-user-*.spec.ts` + `app-user.service.spec.ts` (Angular).
