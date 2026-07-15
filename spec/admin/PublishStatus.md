# Build Spec for PublishStatus
- database schema: `.\database\admin.sql`

---

## Summary

`PublishStatus` is a small reference/lookup table describing the publishing lifecycle state of
content (draft / published / discontinued). Its primary key `pkid` is a **user-entered `tinyint`**
(NOT an IDENTITY column) — the same "entered on add, immutable on edit, is the route id" pattern as
`AppRole.RoleId`, but numeric. It has no foreign keys and no N-N relationships. It is itself an FK
target: `Course.PublishStatus_pkid` and `Promotion2.PublishStatus_pkid` reference it.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **tinyint, NOT IDENTITY** (user-entered `byte`; editable on add, immutable on edit; route id) |
| Foreign Keys | None |
| Required Fields | `pkid` (on add), `Description`, `IsDraft`, `IsPublished`, `IsDiscontinued` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course.PublishStatus_pkid`, `Promotion2.PublishStatus_pkid` (those features are not built yet — see note) |
| Query Filters | keyword (`Description`); tri-state bool `IsDraft`, `IsPublished`, `IsDiscontinued` |
| Default Sort | `pkid ASC` |

---

## Localization

### Chinese Table Name

- PublishStatus: 發布狀態
- Description: 內容發布狀態代碼表（草稿／已發布／已停用）

### Chinese Column Names

- pkid: 主代碼
- Description: 狀態說明
- IsDraft: 草稿
- IsPublished: 已發布
- IsDiscontinued: 已停用

---

## Required Fields

Required (NOT NULL):
- `pkid` — tinyint, user-entered on **add** only; immutable on edit (it is the PK / route id)
- `Description` — nvarchar(50)
- `IsDraft` — bit
- `IsPublished` — bit
- `IsDiscontinued` — bit

Optional (nullable): none.

> All three bit flags are NOT NULL. In the form they default to `false` (unchecked) on add.

---

## Foreign Keys

**N/A** — `PublishStatus` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns, so no outbound navigation links.

---

## Primary-Foreign Links

The following tables reference `PublishStatus.pkid` as an FK target:

- **Course** (`Course.PublishStatus_pkid`) — 對應課程, link `/courses?publishStatusPkid={pkid}`
- **Promotion2** (`Promotion2.PublishStatus_pkid`) — 對應促銷活動, link `/promotions?publishStatusPkid={pkid}`

**Build note:** neither the `Course` nor the `Promotion2` feature exists in `src/` yet (only `AppRole`
is implemented). **Do not add these navigation buttons now** — they would link to routes that 404.
Documented here so they can be wired up when those features are built. The list/detail pages are
generated without Primary-Foreign link buttons for this pass.

---

## N-N Relationships

**N/A** — no junction tables reference `PublishStatus`.

---

## Query Filters

`POST /api/publish-statuses/query` accepts:

- **keyword**: string — LIKE on `Description`.
- **IsDraft**: bool? — tri-state (null = no filter, true = only drafts, false = only non-drafts). Exact match on `IsDraft`.
- **IsPublished**: bool? — tri-state. Exact match on `IsPublished`.
- **IsDiscontinued**: bool? — tri-state. Exact match on `IsDiscontinued`.

No FK filters and no date-range filters (table has neither).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/publish-statuses` | **New** | Slim `{ pkid, description }[]` ordered by `pkid ASC` — for Course/Promotion FK selects |

This feature itself needs **no** lookup endpoints (no FKs). The new `publish-statuses` lookup is added
because `PublishStatus` is an FK target for other tables (`Course`, `Promotion2`).

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/publish-statuses` | List all |
| `POST` | `/api/publish-statuses/query` | Filtered query (body: `PublishStatusQuery`) |
| `GET` | `/api/publish-statuses/{id}` | Get by pkid (byte; route `{id}`, no `:int` constraint) |
| `POST` | `/api/publish-statuses` | Create (pkid supplied in body; 409 if pkid already exists) |
| `PUT` | `/api/publish-statuses` | Update (pkid from body; pkid immutable) |
| `DELETE` | `/api/publish-statuses/{id}` | Delete |

No auth exceptions; follows the same policy as `AppRolesController`.

**Create conflict:** because `pkid` is user-entered, `POST` should return **409 Conflict** if a row with
that `pkid` already exists (mirror however `AppRolesController` handles duplicate `RoleId`; if it does a
plain insert and lets the DB PK violation surface, match that behavior for consistency).

---

## Backend Notes

### Models

```csharp
// Models/PublishStatus.cs
public class PublishStatus
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// Models/PublishStatusRequest.cs
public class PublishStatusRequest
{
    public byte Pkid { get; set; }          // user-entered; used for both create and update
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}

// Models/PublishStatusQuery.cs
public class PublishStatusQuery
{
    public string? Keyword { get; set; }
    public bool? IsDraft { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsDiscontinued { get; set; }
}
```

### SQL — SELECT

No JOINs, no aliases, no `nchar` (Description is `nvarchar`, no `RTRIM` needed):

```sql
SELECT pkid, Description, IsDraft, IsPublished, IsDiscontinued
FROM PublishStatus
-- GetAll / GetById:   ORDER BY pkid ASC   /   WHERE pkid = @Pkid
```

Query builds a dynamic WHERE:
```sql
WHERE (@Keyword IS NULL OR Description LIKE '%' + @Keyword + '%')
  AND (@IsDraft IS NULL OR IsDraft = @IsDraft)
  AND (@IsPublished IS NULL OR IsPublished = @IsPublished)
  AND (@IsDiscontinued IS NULL OR IsDiscontinued = @IsDiscontinued)
ORDER BY pkid ASC
```

### SQL — INSERT

`pkid` **is** written (it is user-entered, not IDENTITY) — do **not** use `SCOPE_IDENTITY()`:

```sql
INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
```

### SQL — UPDATE

`pkid` is the key (immutable), not in the SET list:

```sql
UPDATE PublishStatus
SET Description = @Description, IsDraft = @IsDraft,
    IsPublished = @IsPublished, IsDiscontinued = @IsDiscontinued
WHERE pkid = @Pkid;
```

### N-N Sync Pattern

**N/A.**

### Special Column Notes

- **`pkid` is `tinyint` NOT IDENTITY** → C# `byte`, mapped like `AppRole.RoleId`: writable in INSERT,
  the WHERE key in UPDATE/DELETE/GetById, entered on add, immutable on edit. No `SCOPE_IDENTITY()`.
- No `nchar`, `date`, `time`, `decimal`, or computed columns — no type handlers or `RTRIM` needed.
- **RowAudit**: inject `RowAuditWriter`; log INSERT / UPDATE / DELETE keyed on `pkid`, mirroring
  `AppRoleRepository`. On UPDATE, load existing row and log changed columns via the same helper AppRole uses.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `/publish-statuses` | `PublishStatusListComponent` |
| `/publish-statuses/new` | `PublishStatusFormComponent` (add) |
| `/publish-statuses/:id` | `PublishStatusDetailComponent` |
| `/publish-statuses/:id/edit` | `PublishStatusFormComponent` (edit) |

Register lazy `loadComponent` routes; **`/new` before `/:id`**. `:id` is the numeric `pkid`.

### Angular model

```ts
export interface PublishStatus {
  pkid: number;
  description: string;
  isDraft: boolean;
  isPublished: boolean;
  isDiscontinued: boolean;
}
```

### List component

- Columns: 主代碼 (`pkid`), 狀態說明 (`description`), 草稿 (`isDraft`), 已發布 (`isPublished`),
  已停用 (`isDiscontinued`). Render the three bits as boolean chips / `pi-check` icons.
- Sortable/paginated `p-table`; default sort `pkid ASC`.
- Filter drawer (`p-drawer`): keyword input + three tri-state controls (`p-select` with
  未指定/是/否, or a tri-state checkbox) for `isDraft` / `isPublished` / `isDiscontinued`.
  `p-select` uses `appendTo="body"`.
- Session storage keys: `publishStatus-list-filters`, `publishStatus-list-sort`, `publishStatus-list-page`.

### Detail component

- `RowAuditBadgeComponent` in toolbar `#start`.
- Show all five fields; bits as chips.
- **No** Primary-Foreign link buttons this pass (Course/Promotion features not built — see above).

### Form component

- Reactive Forms; sticky `p-toolbar`; `RowAuditBadgeComponent` in toolbar `#start`.
- No lookups needed → no `forkJoin` required (still fine to keep the standard init shape).
- Fields:
  - `pkid` — `p-inputnumber` (integer, min 0 / max 255). **Enabled on add, disabled on edit**
    (immutable business key — mirror how the AppRole form disables `RoleId` in edit mode).
  - `description` — `input` text, required, maxlength 50.
  - `isDraft`, `isPublished`, `isDiscontinued` — `p-checkbox` / `p-toggleswitch`, each required
    (default `false` on add).

### Delete Confirmation Message

```
確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？
```

### Session Storage Keys

| Key | Contents |
|-----|----------|
| `publishStatus-list-filters` | Last query filter values |
| `publishStatus-list-sort` | `{ sortField, sortOrder }` |
| `publishStatus-list-page` | `{ first, rows }` |

### Sidebar placement

- Nav group: **系統管理 Admin** (existing group — same one `AppRole` lives under).
- Entry: **發布狀態 PublishStatus** → `/publish-statuses`. Add to `app.ts` `navSections` and `app.html`.

---

## Files to Create / Modify

### Backend (`CMS.API`)
| File | Action |
|------|--------|
| `Models/PublishStatus.cs` | create |
| `Models/PublishStatusRequest.cs` | create |
| `Models/PublishStatusQuery.cs` | create |
| `Repositories/IPublishStatusRepository.cs` | create |
| `Repositories/PublishStatusRepository.cs` | create |
| `Controllers/PublishStatusesController.cs` | create |
| `Controllers/LookupsController.cs` | modify — add `GET publish-statuses` |
| `Program.cs` | modify — register `IPublishStatusRepository` / `PublishStatusRepository` |

### Frontend (`CMS.NG`)
| File | Action |
|------|--------|
| `src/app/core/models/publish-status.model.ts` | create |
| `src/app/core/services/publish-status.service.ts` | create |
| `src/app/core/services/lookup.service.ts` | modify — add `getPublishStatuses()` |
| `src/app/features/publish-statuses/publish-status-list/` | create |
| `src/app/features/publish-statuses/publish-status-detail/` | create |
| `src/app/features/publish-statuses/publish-status-form/` | create |
| `src/app/app.routes.ts` | modify — add lazy routes |
| `src/app/app.ts` / `app.html` | modify — sidebar entry under 系統管理 Admin |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/PublishStatusesControllerTests.cs` | create — list+filter, get (found/404), create (+409 dup pkid), update, delete |
| `publish-status.service.spec.ts` | create — URL/verb per method |
| `publish-status-list/detail/form.spec.ts` | create — render + required-field enforcement |
