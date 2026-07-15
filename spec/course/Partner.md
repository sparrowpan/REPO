# Build Spec for Partner
- database schema: `.\database\course.sql`

---

## Summary

`Partner` is a lookup/master entity representing a training-course provider (合作廠商). It carries
display names used in different UI contexts (partner menu, course detail page), an `AppKey` code, a
`DisplayOrder`, and an optional logo image filename. It has **no foreign keys** and **no N-N
relationships**. Its `pkid` is a standard `smallint IDENTITY` surrogate key (auto-generated — unlike
`PublishStatus` whose pkid is user-entered). It is an FK target for `Course`, `Certification`, and
`PartnerCourseGroup`.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (auto-generated surrogate; C# `short`) |
| Foreign Keys | None |
| Required Fields | `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`, `DisplayOrder` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course.Partner_pkid`, `Certification.Partner_pkid`, `PartnerCourseGroup.Partner_pkid` (those features are not built yet — see note) |
| Query Filters | keyword (`Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`) |
| Default Sort | `DisplayOrder ASC` |

---

## Localization

### Chinese Table Name

- Partner: 合作廠商
- Description: 課程合作廠商主資料

### Chinese Column Names

- pkid: 主代碼
- Name: 廠商名稱
- AppKey: 廠商代碼
- NameOnPartnerMenu: 選單顯示名稱
- NameOnCourseDetailPage: 課程頁顯示名稱
- DisplayOrder: 顯示順序
- ImageFilename: 圖檔名稱

---

## Required Fields

Required (NOT NULL):
- `Name` — nvarchar(50)
- `AppKey` — varchar(10)
- `NameOnPartnerMenu` — nvarchar(200)
- `NameOnCourseDetailPage` — nvarchar(50)
- `DisplayOrder` — int

Optional (nullable):
- `ImageFilename` — varchar(50)

---

## Foreign Keys

**N/A** — `Partner` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns, so no outbound navigation links.

---

## Primary-Foreign Links

The following tables reference `Partner.pkid` as an FK target:

- **Course** (`Course.Partner_pkid`) — 對應課程, link `/courses?partnerPkid={pkid}`
- **Certification** (`Certification.Partner_pkid`) — 對應認證, link `/certifications?partnerPkid={pkid}`
- **PartnerCourseGroup** (`PartnerCourseGroup.Partner_pkid`) — 對應廠商課程群組, link `/partner-course-groups?partnerPkid={pkid}`

**Build note:** none of the `Course`, `Certification`, or `PartnerCourseGroup` features exist in `src/`
yet (only `AppRole` and `PublishStatus` are implemented). **Do not add these navigation buttons now** —
they would link to routes that 404. Documented here so they can be wired up when those features are
built. The list/detail pages are generated without Primary-Foreign link buttons for this pass.

---

## N-N Relationships

**N/A.** `PartnerCourseGroup` contains `Partner_pkid` but is **not** a pure junction table — it has its
own `pkid` IDENTITY plus `DisplayOrder` and `Description` columns, so it is a standalone child entity
(a Primary-Foreign link above), not an N-N relationship.

---

## Query Filters

`POST /api/partners/query` accepts:

- **keyword**: string — LIKE on `Name`, `AppKey`, `NameOnPartnerMenu`, `NameOnCourseDetailPage`.
  (`ImageFilename` is excluded — it is a file path, not an identifying name.)

No FK filters, bool filters, or date-range filters (the table has none).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **New** | Slim `{ pkid, name }[]` ordered by `DisplayOrder ASC` — for Course/Certification FK selects |

This feature itself needs **no** lookup endpoints (no FKs). The new `partners` lookup is added because
`Partner` is an FK target for other tables. Option label = `Name`, order by `DisplayOrder ASC`
(matches the reference Course spec).

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/partners` | List all |
| `POST` | `/api/partners/query` | Filtered query (body: `PartnerQuery`) |
| `GET` | `/api/partners/{id}` | Get by pkid (short) |
| `POST` | `/api/partners` | Create (pkid auto-generated) |
| `PUT` | `/api/partners` | Update (pkid from body) |
| `DELETE` | `/api/partners/{id}` | Delete |

No auth exceptions; same policy as `AppRolesController` / `PublishStatusesController`.

No create-conflict check needed — `pkid` is IDENTITY (auto-generated), so there is no user-entered
business key to collide (contrast with `AppRole.RoleId` / `PublishStatus.pkid`).

---

## Backend Notes

### Models

```csharp
// Models/Partner.cs
public class Partner
{
    public short Pkid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string NameOnPartnerMenu { get; set; } = string.Empty;
    public string NameOnCourseDetailPage { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageFilename { get; set; }
}

// Models/PartnerRequest.cs
public class PartnerRequest
{
    public short Pkid { get; set; }         // present on update, ignored on insert (IDENTITY)

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string AppKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NameOnPartnerMenu { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string NameOnCourseDetailPage { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [MaxLength(50)]
    public string? ImageFilename { get; set; }
}

// Models/PartnerQuery.cs
public class PartnerQuery
{
    public string? Keyword { get; set; }
}
```

### SQL — SELECT

No JOINs, no aliases. All columns are `nvarchar`/`varchar` (no `nchar`), so **no `RTRIM` needed**:

```sql
SELECT p.pkid, p.Name, p.AppKey, p.NameOnPartnerMenu, p.NameOnCourseDetailPage,
       p.DisplayOrder, p.ImageFilename
FROM Partner p
-- GetAll:  ORDER BY p.DisplayOrder ASC
-- GetById: WHERE p.pkid = @Pkid
```

Query WHERE:
```sql
WHERE (@Keyword IS NULL
       OR p.Name LIKE '%' + @Keyword + '%'
       OR p.AppKey LIKE '%' + @Keyword + '%'
       OR p.NameOnPartnerMenu LIKE '%' + @Keyword + '%'
       OR p.NameOnCourseDetailPage LIKE '%' + @Keyword + '%')
ORDER BY p.DisplayOrder ASC
```

### SQL — INSERT

`pkid` is IDENTITY — exclude it, return the generated key via `SCOPE_IDENTITY()`:

```sql
INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

### SQL — UPDATE

```sql
UPDATE Partner
SET Name = @Name, AppKey = @AppKey, NameOnPartnerMenu = @NameOnPartnerMenu,
    NameOnCourseDetailPage = @NameOnCourseDetailPage, DisplayOrder = @DisplayOrder,
    ImageFilename = @ImageFilename
WHERE pkid = @Pkid;
```

### SQL — DELETE

```sql
DELETE FROM Partner WHERE pkid = @Pkid;
```

### N-N Sync Pattern

**N/A.**

### Special Column Notes

- **`pkid` is `smallint IDENTITY`** → C# `short`; standard auto-generated surrogate (use `SCOPE_IDENTITY()`
  cast to `smallint`). No user-entered key, so no duplicate-key 409 handling.
- No `nchar`, `date`, `time`, `decimal`, `bit`, or computed columns — no `RTRIM`, type handlers, or
  special mapping needed.
- **RowAudit**: the codebase's reference features (`AppRole`, `PublishStatus`) do **not** use RowAudit —
  no RowAudit infrastructure exists. Follow that pattern; do not add RowAudit here.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `/partners` | `PartnerListComponent` |
| `/partners/new` | `PartnerFormComponent` (add) |
| `/partners/:id` | `PartnerDetailComponent` |
| `/partners/:id/edit` | `PartnerFormComponent` (edit) |

Register lazy `loadComponent` routes; **`/new` before `/:id`**. `:id` is the numeric `pkid`.

### Angular model

```ts
export interface Partner {
  pkid: number;
  name: string;
  appKey: string;
  nameOnPartnerMenu: string;
  nameOnCourseDetailPage: string;
  displayOrder: number;
  imageFilename: string | null;
}
```

### List component

- Columns: 主代碼 (`pkid`), 廠商名稱 (`name`), 廠商代碼 (`appKey`), 選單顯示名稱 (`nameOnPartnerMenu`),
  課程頁顯示名稱 (`nameOnCourseDetailPage`), 顯示順序 (`displayOrder`).
- Sortable/paginated `p-table`; default sort `displayOrder ASC`.
- Filter drawer (`p-drawer`): single keyword input. `p-select` (n/a here — no dropdowns).
- Session storage keys: `partner-list-filters`, `partner-list-sort`, `partner-list-page`.

### Detail component

- Show all fields; `imageFilename` shown as text (or `—` when null).
- **No** Primary-Foreign link buttons this pass (Course/Certification/PartnerCourseGroup not built — see above).

### Form component

- Reactive Forms; sticky `p-toolbar`.
- No lookups needed → no `forkJoin` required (keep the standard init shape).
- Fields:
  - `name` — `input` text, required, maxlength 50.
  - `appKey` — `input` text, required, maxlength 10.
  - `nameOnPartnerMenu` — `input` text, required, maxlength 200.
  - `nameOnCourseDetailPage` — `input` text, required, maxlength 50.
  - `displayOrder` — `p-inputnumber`, required.
  - `imageFilename` — `input` text, optional, maxlength 50.
- `pkid` is IDENTITY (auto) → **not shown as an editable field**; no immutable-key handling needed
  (contrast with AppRole/PublishStatus).

### Delete Confirmation Message

```
確定要刪除主代碼 <b>${item.pkid}</b>「${item.name}」？
```

### Session Storage Keys

| Key | Contents |
|-----|----------|
| `partner-list-filters` | Last query filter values |
| `partner-list-sort` | `{ sortField, sortOrder }` |
| `partner-list-page` | `{ first, rows }` |

---

## Sidebar Placement

- Nav group: **課程管理 Course** (currently an empty group in `app.ts` `navSections` — add the first child).
- Entry: **合作廠商 Partner** → `/partners` (icon e.g. `pi pi-building`).
- The 課程管理 Course group already exists in the MENU section but has `children: []`; add the child there.

---

## Files to Create / Modify

### Backend (`CMS.API`)
| File | Action |
|------|--------|
| `Models/Partner.cs`, `PartnerRequest.cs`, `PartnerQuery.cs` | create |
| `Models/PartnerLookup.cs` | create (slim lookup) |
| `Repositories/IPartnerRepository.cs`, `PartnerRepository.cs` | create |
| `Controllers/PartnersController.cs` | create |
| `Repositories/ILookupRepository.cs` + `LookupRepository.cs` + `Controllers/LookupsController.cs` | modify — add `GET partners` |
| `Program.cs` | modify — register `IPartnerRepository` |

### Frontend (`CMS.NG`)
| File | Action |
|------|--------|
| `core/models/partner.model.ts`, `partner-lookup.model.ts` | create |
| `core/services/partner.service.ts` | create |
| `core/services/lookup.service.ts` | modify — add `getPartners()` |
| `features/partners/partner-list/`, `partner-detail/`, `partner-form/` | create |
| `app.routes.ts` | modify — add lazy routes |
| `app.ts` | modify — sidebar entry under 課程管理 Course |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/PartnersControllerTests.cs` + `Fakes/FakePartnerRepository.cs` + `CmsApiFactory.cs` registration | create/modify |
| `partner.service.spec.ts` | create — URL/verb per method |
| `partner-list/detail/form.spec.ts` | create — render + required-field enforcement |
