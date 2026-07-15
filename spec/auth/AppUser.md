# Build Spec for AppUser
- database schema: `.\database\auth.sql`

---

## Summary

`AppUser` is an application login account. It carries a business login id (`UserId`), a display
`UserName`, an `IsActive` flag, and password material (`PasswordHash`, `PasswordUpdatedTime`). It has an
N-N relationship with `AppRole` via the junction table `AppUserRole` (keyed on `UserId` / `RoleId`) —
this is the same junction the AppRole feature manages from the other side.

Follows the **AppRole reference feature** exactly (string business PK, surrogate `pkid`, n-n via a
delete-then-reinsert sync). The one departure: `PasswordHash` is a backend-only column — it is never
sent to or received from the frontend, defaulted from `SysConfig` on create, and only ever changed via
a dedicated reset-password endpoint.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` int IDENTITY (surrogate, 主代碼); `UserId` nvarchar(200) is the clustered business PK / route id |
| Foreign Keys | None on AppUser itself |
| Required Fields | `UserId`, `UserName`, `IsActive` (+ backend-managed `PasswordHash`) |
| N-N Relationships | `AppUserRole` — AppUser ↔ AppRole (keyed on `UserId` / `RoleId`) |
| Primary-Foreign Links | `AppUserRole` references `AppUser.UserId` — managed inline via the N-N section (no separate list-page link) |
| Query Filters | keyword (UserId, UserName), IsActive (tri-state) |
| Default Sort | `UserId ASC` |

---

## Localization

### Chinese Table Name

- AppUser: 使用者
- Description: 系統登入使用者帳號

### Chinese Column Names

- pkid: 主代碼
- UserId: 使用者代碼
- UserName: 使用者名稱
- IsActive: 啟用
- PasswordHash: 密碼雜湊 *(backend only — never surfaced to the frontend)*
- PasswordUpdatedTime: 密碼更新時間 *(read-only display)*
- (n-n) RoleIds: 角色
- (subquery) RoleCount: 角色數

---

## Required Fields

Required (NOT NULL, excluding IDENTITY PK):
- `UserId` NOT NULL — immutable business key, entered on add
- `UserName` NOT NULL
- `IsActive` NOT NULL (DB default `1`)
- `PasswordHash` NOT NULL — **backend-managed**, not part of the request DTO (see Password Handling)

Optional (nullable):
- `PasswordUpdatedTime` — set by the backend on create / password reset; read-only in the UI

---

## Foreign Keys

`AppUser` has no foreign key columns.

**N/A**

---

## Foreign-Primary Links

`AppUser` has no foreign key columns.

**N/A**

---

## Primary-Foreign Links

`AppUserRole` references `AppUser.UserId`. This relationship is managed inline via the N-N Relationships
section (role multiselect on the form, role tags on the detail page). No separate child-list-page link
is needed.

**N/A**

---

## N-N Relationships

### AppUser ↔ AppRole via `AppUserRole`

Junction table: `AppUserRole` (`UserId`, `RoleId`) — composite PK, FK to both `AppUser.UserId` and
`AppRole.RoleId`.

- The related entity (B) is **AppRole**; load options via **new** `GET /api/lookups/approles`
  (returns `RoleId`, `RoleName`, and a computed `Label` = `"RoleName (RoleId)"`, ordered by `RoleId`).
- **List / Detail view**: show the associated role labels (not raw ids). List shows a `RoleCount`
  subquery column; detail shows role tags.
- **Form (edit + new)**: `p-multiselect` of roles (`optionLabel="label"`, `optionValue="roleId"`).
- Request field: `RoleIds` — `List<string>` on `AppUserRequest`; populated on GET-by-id.
- Sync pattern on create / update (same connection + transaction):
  1. `DELETE FROM AppUserRole WHERE UserId = @UserId`
  2. Bulk `INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)` for each id.

---

## Query Filters

- **keyword**: string — LIKE on `UserId`, `UserName`.
- **IsActive**: bool? — exact match on the `IsActive` bit. Tri-state: null = 全部, true = 啟用, false = 停用.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/approles` | **New** | Slim AppRole list (`RoleId`, `RoleName`, `Label`) for the AppUser n-n role multiselect |

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/appusers` | List all |
| `POST` | `/api/appusers/query` | Filtered query (body: `AppUserQuery`) |
| `GET` | `/api/appusers/{id}` | Get by UserId (string PK; includes `RoleIds`) |
| `POST` | `/api/appusers` | Create (409 if `UserId` exists; sets `PasswordHash` from `SysConfig`) |
| `PUT` | `/api/appusers` | Update (`UserId` from body; **never** touches `PasswordHash`) |
| `DELETE` | `/api/appusers/{id}` | Delete (removes `AppUserRole` rows first) |
| `POST` | `/api/appusers/{id}/reset-password` | Reset `PasswordHash` to the `SysConfig` default; stamps `PasswordUpdatedTime`. 204 / 404 |

---

## Password Handling (backend only)

`PasswordHash` is **never** sent to or received from the frontend. It is excluded from `AppUserRequest`,
the `AppUser` response model, and every Angular model.

- **Default password source**: `SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'`.
  `configValue` is a JSON object; extract its `defaultPassword` property. If the row / property is
  missing, fall back to a constant default.
- **Hashing**: SHA-256 over the UTF-8 bytes of the default password, stored as a lowercase hex string
  (`Convert.ToHexStringLower`), which fits `nvarchar(800)`.
- **On CREATE**: hash the default password → `PasswordHash`; set `PasswordUpdatedTime = GETDATE()`.
- **On UPDATE**: do **not** modify `PasswordHash` or `PasswordUpdatedTime`.
- **On RESET (`POST /{id}/reset-password`)**: re-hash the current default password → `PasswordHash`;
  set `PasswordUpdatedTime = GETDATE()`.

---

## Backend Notes

### Models

```csharp
// AppUser.cs — response (NO PasswordHash)
public class AppUser
{
    public int Pkid { get; set; }               // 主代碼
    public string UserId { get; set; } = "";    // 使用者代碼 (business PK)
    public string UserName { get; set; } = "";  // 使用者名稱
    public bool IsActive { get; set; }           // 啟用
    public DateTime? PasswordUpdatedTime { get; set; } // 密碼更新時間 (read-only)
    public int RoleCount { get; set; }           // 角色數 (subquery)
    public List<string> RoleIds { get; set; } = []; // populated on GET by id
}

// AppUserRequest.cs — write DTO (NO PasswordHash, NO PasswordUpdatedTime)
public class AppUserRequest
{
    public int Pkid { get; set; }
    [Required, MaxLength(200)] public string UserId { get; set; } = "";
    [Required, MaxLength(200)] public string UserName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<string> RoleIds { get; set; } = [];
}

// AppUserQuery.cs — search DTO
public class AppUserQuery
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}
```

### SQL — SELECT

```sql
SELECT u.pkid, u.UserId, u.UserName, u.IsActive, u.PasswordUpdatedTime,
       (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
FROM AppUser u
```
- `GetAll`: `ORDER BY u.UserId ASC`.
- `Query`: `WHERE (@Keyword IS NULL OR u.UserId LIKE '%'+@Keyword+'%' OR u.UserName LIKE '%'+@Keyword+'%')
  AND (@IsActive IS NULL OR u.IsActive = @IsActive) ORDER BY u.UserId ASC`.
- `GetById`: main row + `SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId` via
  `QueryMultiple`.
- **PasswordHash is never selected** — it is not on the model.

### SQL — INSERT

```sql
INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
VALUES (@UserId, @UserName, @IsActive, @PasswordHash, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS int);
```
`@PasswordHash` is computed server-side (never from the request).

### SQL — UPDATE

```sql
UPDATE AppUser SET UserName = @UserName, IsActive = @IsActive WHERE UserId = @UserId;
```
Excludes `PasswordHash`, `PasswordUpdatedTime`, `UserId` (immutable).

### SQL — reset-password

```sql
UPDATE AppUser SET PasswordHash = @PasswordHash, PasswordUpdatedTime = GETDATE() WHERE UserId = @UserId;
```

### N-N Sync Pattern

After INSERT / UPDATE, inside the same transaction:
```sql
DELETE FROM AppUserRole WHERE UserId = @UserId;
-- for each roleId: INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId);
```

### Special Column Notes

- `PasswordHash nvarchar(800)` — backend-only (see Password Handling).
- `PasswordUpdatedTime datetime` NULL — Dapper returns `Kind = Unspecified`; frontend appends `'Z'`
  before display.
- No `DateOnly` / `nchar` columns → no extra type handlers or `RTRIM()` needed.

### Lookup — `AppRoleLookup`

```csharp
public class AppRoleLookup
{
    public string RoleId { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string Label => $"{RoleName} ({RoleId})";
}
```
`GetAppRolesAsync`: `SELECT RoleId, RoleName FROM AppRole ORDER BY RoleId`.

---

## Frontend Notes

### Angular Models (`app-user.model.ts`)

```ts
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  passwordUpdatedTime: string | null; // read-only
  roleCount: number;
  roleIds: string[];
}
export interface AppUserRequest {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
}
export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
}
```
No `passwordHash` field anywhere.

### Routes (`app.routes.ts`)

| Path | Component |
|------|-----------|
| `app-users` | `AppUserList` |
| `app-users/new` | `AppUserForm` |
| `app-users/:id/edit` | `AppUserForm` |
| `app-users/:id` | `AppUserDetail` |

`/new` registered before `/:id`. Service `getById` / `delete` / `resetPassword` `encodeURIComponent`
the `UserId`.

### List component

- Columns: 主代碼, 使用者代碼 (link), 使用者名稱, 啟用 (`p-tag` 啟用/停用), 角色數, 密碼更新時間, 操作.
- Filter drawer: keyword input; IsActive `p-select` (全部 / 啟用 / 停用).
- Session storage keys: `app-user-list-filters`, `app-user-list-sort`, `app-user-list-page`.
- Delete confirm: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.userId}」？`.

### Detail component

- Fields card + roles tags card (labels resolved via `GET /api/lookups/approles`).
- `密碼更新時間` shown with `{{ passwordUpdatedTime + 'Z' | date:'yyyy-MM-dd HH:mm' }}`.
- **重設密碼** button → `POST /api/appusers/{id}/reset-password`; success toast.

### Form component

- Reactive Forms; `forkJoin` for the role lookup (+ the user on edit).
- `userId` (required, disabled in edit mode), `userName` (required), `isActive` (`p-checkbox [binary]`),
  roles (`p-multiselect`, `optionLabel="label"`, `optionValue="roleId"`).
- No password field.

### Sidebar

**系統管理 Admin → 使用者 AppUser** (`/app-users`, icon `pi pi-user`) — the nav entry already exists in
`app.ts`; only the routes need wiring.

---

## Tests

### Backend (`CMS.API.Tests`)

- `Fakes/FakeAppUserRepository.cs` — in-memory `IAppUserRepository`, seeded with a couple of users +
  role assignments. Register it in `CmsApiFactory`.
- `AppUsersControllerTests.cs` — list, keyword filter, IsActive filter, get-by-id (found + 404),
  create (201 + retrievable), duplicate 409, missing-required 400, update (204 + persists), update
  unknown 404, delete (204), reset-password (204 + 404).

### Frontend (`CMS.NG`)

- `app-user.service.spec.ts` — asserts each method's URL/verb and `encodeURIComponent` on the string PK
  (including `reset-password`).
- `app-user-list.spec.ts`, `app-user-detail.spec.ts`, `app-user-form.spec.ts` — mount with a mocked
  service/lookup; assert render, required-field enforcement, and edit-mode `userId` disabled.

---

## Files to Create / Modify

| Layer | File | Action |
|-------|------|--------|
| Backend | `Models/AppUser.cs`, `AppUserRequest.cs`, `AppUserQuery.cs` | create |
| Backend | `Models/AppRoleLookup.cs` | create |
| Backend | `Repositories/IAppUserRepository.cs`, `AppUserRepository.cs` | create |
| Backend | `Controllers/AppUsersController.cs` | create |
| Backend | `Repositories/ILookupRepository.cs`, `LookupRepository.cs`, `Controllers/LookupsController.cs` | modify (add `approles`) |
| Backend | `Program.cs` | modify (DI) |
| Frontend | `core/models/app-user.model.ts`, `core/models/app-role-lookup.model.ts` | create |
| Frontend | `core/services/app-user.service.ts` (+ `.spec.ts`) | create |
| Frontend | `core/services/lookup.service.ts` | modify (add `getAppRoles`) |
| Frontend | `features/app-users/app-user-list|-detail|-form/*` | create |
| Frontend | `app.routes.ts` | modify (add routes) |
| Tests | `CMS.API.Tests/Fakes/FakeAppUserRepository.cs`, `AppUsersControllerTests.cs` | create |
| Tests | `CMS.API.Tests/CmsApiFactory.cs` | modify (register fake) |
</content>
</invoke>
