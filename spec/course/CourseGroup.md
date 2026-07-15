# Build Spec for CourseGroup
- database schema: `.\database\course.sql`

---

## Summary

`CourseGroup` is a small lookup/master entity (課程群組) used to categorize courses. It has only two
columns: a `pkid` surrogate key and a `Description`. It has **no foreign keys** and **no N-N
relationships**. Its `pkid` is a `smallint IDENTITY` (auto-generated — like `Partner`, unlike
`PublishStatus` whose pkid is user-entered). It is an FK target for `Course` (`Course.CourseGroup_pkid`,
nullable) and `PartnerCourseGroup` (`PartnerCourseGroup.CourseGroup_pkid`).

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **smallint IDENTITY** (auto-generated surrogate; C# `short`) |
| Foreign Keys | None |
| Required Fields | `Description` |
| N-N Relationships | N/A |
| Primary-Foreign Links | `Course.CourseGroup_pkid`, `PartnerCourseGroup.CourseGroup_pkid` (those features are not built yet — see note) |
| Query Filters | keyword (`Description`) |
| Default Sort | `pkid ASC` |

---

## Localization

### Chinese Table Name

- CourseGroup: 課程群組
- Description: 課程分類群組主資料

### Chinese Column Names

- pkid: 主代碼
- Description: 群組名稱

---

## Required Fields

Required (NOT NULL):
- `Description` — nvarchar(100)

Optional (nullable):
- *(none)*

---

## Foreign Keys

**N/A** — `CourseGroup` has no foreign key columns.

---

## Foreign-Primary Links

**N/A** — no foreign key columns, so no outbound navigation links.

---

## Primary-Foreign Links

The following tables reference `CourseGroup.pkid` as an FK target:

- **Course** (`Course.CourseGroup_pkid`, nullable, `ON DELETE CASCADE`) — 對應課程, link `/courses?courseGroupPkid={pkid}`
- **PartnerCourseGroup** (`PartnerCourseGroup.CourseGroup_pkid`) — 對應廠商課程群組, link `/partner-course-groups?courseGroupPkid={pkid}`

**Build note:** neither the `Course` nor `PartnerCourseGroup` feature exists in `src/` yet (only
`AppRole`, `PublishStatus`, and `Partner` are implemented). **Do not add these navigation buttons now** —
they would link to routes that 404. Documented here so they can be wired up when those features are
built. The list/detail pages are generated without Primary-Foreign link buttons for this pass.

---

## N-N Relationships

**N/A.** `PartnerCourseGroup` contains `CourseGroup_pkid` but is **not** a pure junction table — it has
its own `pkid` IDENTITY plus `Partner_pkid`, `DisplayOrder`, and `Description` columns, so it is a
standalone child entity (a Primary-Foreign link above), not an N-N relationship.

---

## Query Filters

`POST /api/course-groups/query` accepts:

- **keyword**: string — LIKE on `Description` (the only string column).

No FK filters, bool filters, or date-range filters (the table has none).

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/course-groups` | **New** | Slim `{ pkid, description }[]` ordered by `pkid ASC` — for the `Course.CourseGroup_pkid` FK select |

This feature itself needs **no** lookup endpoints (no FKs). The new `course-groups` lookup is added
because `CourseGroup` is an FK target for `Course`. Option label = `Description`, order by `pkid ASC`
(matches the reference Course spec's `course-groups` lookup).

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/course-groups` | List all |
| `POST` | `/api/course-groups/query` | Filtered query (body: `CourseGroupQuery`) |
| `GET` | `/api/course-groups/{id}` | Get by pkid (short) |
| `POST` | `/api/course-groups` | Create (pkid auto-generated) |
| `PUT` | `/api/course-groups` | Update (pkid from body) |
| `DELETE` | `/api/course-groups/{id}` | Delete |

No auth exceptions; same policy as `PartnersController` / `PublishStatusesController`.

No create-conflict check needed — `pkid` is IDENTITY (auto-generated), so there is no user-entered
business key to collide (contrast with `AppRole.RoleId` / `PublishStatus.pkid`).

---

## Backend Notes

### Models

```csharp
// Models/CourseGroup.cs
public class CourseGroup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}

// Models/CourseGroupRequest.cs
public class CourseGroupRequest
{
    public short Pkid { get; set; }         // present on update, ignored on insert (IDENTITY)

    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;
}

// Models/CourseGroupQuery.cs
public class CourseGroupQuery
{
    public string? Keyword { get; set; }
}
```

Also add a slim lookup model:

```csharp
// Models/CourseGroupLookup.cs
public class CourseGroupLookup
{
    public short Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

### SQL — SELECT

No JOINs, no aliases. `Description` is `nvarchar` (no `nchar`), so **no `RTRIM` needed**:

```sql
SELECT g.pkid, g.Description
FROM CourseGroup g
-- GetAll:  ORDER BY g.pkid ASC
-- GetById: WHERE g.pkid = @Pkid
```

Query WHERE:
```sql
WHERE (@Keyword IS NULL OR g.Description LIKE '%' + @Keyword + '%')
ORDER BY g.pkid ASC
```

### SQL — INSERT

`pkid` is IDENTITY — exclude it, return the generated key via `SCOPE_IDENTITY()`:

```sql
INSERT INTO CourseGroup (Description)
VALUES (@Description);
SELECT CAST(SCOPE_IDENTITY() AS smallint);
```

### SQL — UPDATE

```sql
UPDATE CourseGroup
SET Description = @Description
WHERE pkid = @Pkid;
```

### SQL — DELETE

```sql
DELETE FROM CourseGroup WHERE pkid = @Pkid;
```

Note: `Course.CourseGroup_pkid` has `ON DELETE CASCADE`, so deleting a CourseGroup cascades to
`Course` rows. `PartnerCourseGroup` FK has no cascade, so a delete may be blocked if referenced.
No special handling added this pass (matches Partner's straightforward DELETE).

### N-N Sync Pattern

**N/A.**

### Special Column Notes

- **`pkid` is `smallint IDENTITY`** → C# `short`; standard auto-generated surrogate (use `SCOPE_IDENTITY()`
  cast to `smallint`). No user-entered key, so no duplicate-key 409 handling.
- No `nchar`, `date`, `time`, `decimal`, `bit`, or computed columns — no `RTRIM`, type handlers, or
  special mapping needed.
- **RowAudit**: the codebase's reference features (`AppRole`, `PublishStatus`, `Partner`) do **not** use
  RowAudit — no RowAudit infrastructure exists. Follow that pattern; do not add RowAudit here.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `/course-groups` | `CourseGroupListComponent` |
| `/course-groups/new` | `CourseGroupFormComponent` (add) |
| `/course-groups/:id` | `CourseGroupDetailComponent` |
| `/course-groups/:id/edit` | `CourseGroupFormComponent` (edit) |

Register lazy `loadComponent` routes; **`/new` before `/:id`**. `:id` is the numeric `pkid`.

### Angular model

```ts
export interface CourseGroup {
  pkid: number;
  description: string;
}
```

Slim lookup model (`course-group-lookup.model.ts`):

```ts
export interface CourseGroupLookup {
  pkid: number;
  description: string;
}
```

### List component

- Columns: 主代碼 (`pkid`), 群組名稱 (`description`).
- Sortable/paginated `p-table`; default sort `pkid ASC`.
- Filter drawer (`p-drawer`): single keyword input.
- Session storage keys: `course-group-list-filters`, `course-group-list-sort`, `course-group-list-page`.

### Detail component

- Show all fields (`pkid`, `description`).
- **No** Primary-Foreign link buttons this pass (Course/PartnerCourseGroup not built — see above).

### Form component

- Reactive Forms; sticky `p-toolbar`.
- No lookups needed → no `forkJoin` required (keep the standard init shape).
- Fields:
  - `description` — `input` text, required, maxlength 100.
- `pkid` is IDENTITY (auto) → **not shown as an editable field**; no immutable-key handling needed
  (contrast with AppRole/PublishStatus).

### Delete Confirmation Message

```
確定要刪除主代碼 <b>${item.pkid}</b>「${item.description}」？
```

### Session Storage Keys

| Key | Contents |
|-----|----------|
| `course-group-list-filters` | Last query filter values |
| `course-group-list-sort` | `{ sortField, sortOrder }` |
| `course-group-list-page` | `{ first, rows }` |

---

## Sidebar Placement

- Nav group: **課程管理 Course** (already exists in `app.ts` `navSections`; `Partner` is the first child).
- Entry: **課程群組 CourseGroup** → `/course-groups` (icon e.g. `pi pi-sitemap` / `pi pi-tags`).
- Add as a sibling child under the existing 課程管理 Course group in the MENU section.

---

## Files to Create / Modify

### Backend (`CMS.API`)
| File | Action |
|------|--------|
| `Models/CourseGroup.cs`, `CourseGroupRequest.cs`, `CourseGroupQuery.cs` | create |
| `Models/CourseGroupLookup.cs` | create (slim lookup) |
| `Repositories/ICourseGroupRepository.cs`, `CourseGroupRepository.cs` | create |
| `Controllers/CourseGroupsController.cs` | create |
| `Repositories/ILookupRepository.cs` + `LookupRepository.cs` + `Controllers/LookupsController.cs` | modify — add `GET course-groups` |
| `Program.cs` | modify — register `ICourseGroupRepository` |

### Frontend (`CMS.NG`)
| File | Action |
|------|--------|
| `core/models/course-group.model.ts`, `course-group-lookup.model.ts` | create |
| `core/services/course-group.service.ts` | create |
| `core/services/lookup.service.ts` | modify — add `getCourseGroups()` |
| `features/course-groups/course-group-list/`, `course-group-detail/`, `course-group-form/` | create |
| `app.routes.ts` | modify — add lazy routes |
| `app.ts` | modify — sidebar entry under 課程管理 Course |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/CourseGroupsControllerTests.cs` + `Fakes/FakeCourseGroupRepository.cs` + `CmsApiFactory.cs` registration | create/modify |
| `course-group.service.spec.ts` | create — URL/verb per method |
| `course-group-list/detail/form.spec.ts` | create — render + required-field enforcement |
