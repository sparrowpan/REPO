# Build Spec for Course
- database schema: `.\database\course.sql`

---

## Summary

`Course` (課程) is the central content entity of the course sub-system. It carries identifying codes
(`CourseId`, `ProdCourseId`, `FriendlyUrl`), display/marketing text, scheduling dates, pricing, and
three foreign keys (`Partner`, `CourseGroup`, `PublishStatus`). It participates in two **N-N**
relationships via pure junction tables (`CourseJobCategories` → `JobCategory`, `CourseInCertification`
→ `Certification`). Its `pkid` is a standard `int IDENTITY` surrogate key.

| Item | Detail |
|------|--------|
| Primary Key | `pkid` **int IDENTITY** (auto-generated surrogate; C# `int`) |
| Foreign Keys | `Partner_pkid` → `Partner` (NOT NULL, `short`), `CourseGroup_pkid` → `CourseGroup` (**nullable**, `short?`), `PublishStatus_pkid` → `PublishStatus` (NOT NULL, `byte`) |
| Required Fields | `Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`, `DisplayOrder`, `Partner_pkid`, `PublishStatus_pkid`, `ScheduleOn`, `ScheduleOff`, `Hour`, `ListPrice`, `LearningCredit`, `CanRepeat` |
| N-N Relationships | `CourseJobCategories` (↔ `JobCategory`), `CourseInCertification` (↔ `Certification`) |
| Primary-Foreign Links | `CourseFAQ`, `CourseRelatedLink`, `HotCourse` reference `Course.pkid` (features not built — documented, no buttons this pass) |
| Query Filters | keyword (`Title`, `CourseId`, `ProdCourseId`, `FriendlyUrl`, `OfficialTitle`); FK dropdowns (Partner, CourseGroup, PublishStatus); `CanRepeat` tri-state; `ScheduleOn` / `ScheduleOff` date ranges |
| Default Sort | `DisplayOrder ASC, pkid DESC` |

---

## Localization

### Chinese Table Name

- Course: 課程
- Description: 課程主資料（含代碼、排程、定價與分類）

### Chinese Column Names

- pkid: 主代碼
- Title: 課程名稱
- OfficialTitle: 正式名稱
- CourseId: 簡介代碼
- ProdCourseId: 科目代碼
- FriendlyUrl: 網址代稱
- DisplayOrder: 顯示順序
- Partner_pkid (Partner.Name): 原廠
- CourseGroup_pkid (CourseGroup.Description): 課程群組
- PublishStatus_pkid (PublishStatus.Description): 上架狀態
- ScheduleOn: 上架日期
- ScheduleOff: 下架日期
- Hour: 時數
- ListPrice: 定價
- LearningCredit: 點數
- Material: 教材
- Objective: 課程目標
- Target: 適合對象
- Prerequisites: 先修條件
- Outline: 課程大綱
- TowardCertOrExam: 對應認證/考試
- Note: 備註
- OtherInfo: 其他資訊
- CanRepeat: 允許重聽
- (N-N) JobCategories: 職務類別
- (N-N) Certifications: 對應認證

---

## Required Fields

Required (NOT NULL):
- `Title` — nvarchar(200)
- `CourseId` — varchar(50)
- `ProdCourseId` — varchar(50)
- `FriendlyUrl` — nvarchar(100)
- `DisplayOrder` — int
- `Partner_pkid` — smallint (FK)
- `PublishStatus_pkid` — tinyint (FK)
- `ScheduleOn` — date
- `ScheduleOff` — date
- `Hour` — smallint (DB default 0)
- `ListPrice` — decimal(9,0) (DB default 0)
- `LearningCredit` — decimal(9,1) (DB default 0)
- `CanRepeat` — bit (DB default 0)

Optional (nullable):
- `OfficialTitle` — nvarchar(300)
- `CourseGroup_pkid` — smallint (FK, nullable)
- `Material` — nvarchar(500)
- `Objective` — nvarchar(4000)
- `Target` — nvarchar(500)
- `Prerequisites` — nvarchar(4000)
- `Outline` — nvarchar(max)
- `TowardCertOrExam` — nvarchar(max)
- `Note` — nvarchar(4000)
- `OtherInfo` — nvarchar(4000)

---

## Foreign Keys

- **`Partner_pkid`** → `Partner.pkid` (NOT NULL). Alias `Partner_pkid AS PartnerPkid`. Nav object
  `partner { pkid, name }`. Option label = `Partner.Name`, order by `Partner.DisplayOrder ASC`.
  Lookup: `GET /api/lookups/partners` (**exists**).
- **`CourseGroup_pkid`** → `CourseGroup.pkid` (**nullable** — allow null / 無 option). Alias
  `CourseGroup_pkid AS CourseGroupPkid`. Nav object `courseGroup { pkid, description }`. Option label =
  `CourseGroup.Description`, order by `CourseGroup.Description ASC`. Lookup:
  `GET /api/lookups/course-groups` (**exists**).
- **`PublishStatus_pkid`** → `PublishStatus.pkid` (NOT NULL). Alias `PublishStatus_pkid AS PublishStatusPkid`.
  Nav object `publishStatus { pkid, description }`. Option label = `PublishStatus.Description`, order by
  `PublishStatus.pkid ASC`. Lookup: `GET /api/lookups/publish-statuses` (**exists**).

All three FK `_pkid` column names contain an underscore and must be aliased to their PascalCase C#
property names in every SELECT (Dapper multi-map splits on the nav-object key columns).

---

## Foreign-Primary Links

Outbound navigation from `Course` to the referenced primary detail pages (shown in list/detail):

- `Partner_pkid` → `/partners/{partnerPkid}` (always present — NOT NULL)
- `CourseGroup_pkid` → `/course-groups/{courseGroupPkid}` (only when not null)
- `PublishStatus_pkid` → `/publish-statuses/{publishStatusPkid}` (always present)

All three target features (`partners`, `course-groups`, `publish-statuses`) **are built**, so these
outbound links are safe to render. In the list the FK columns display the nav-object label as plain
text; the detail page may render them as `routerLink` chips to the target detail pages.

---

## Primary-Foreign Links

The following tables reference `Course.pkid` as an FK target:

- **CourseFAQ** (`CourseFAQ.Course_pkid`) — 對應常見問題, `/course-faqs?coursePkid={pkid}`
- **CourseRelatedLink** (`CourseRelatedLink.Course_pkid`) — 對應相關連結, `/course-related-links?coursePkid={pkid}`
- **HotCourse** (`HotCourse.Course_pkid`) — 對應熱門課程, `/hot-courses?coursePkid={pkid}`

**Build note:** none of `CourseFAQ`, `CourseRelatedLink`, or `HotCourse` features exist in `src/` yet.
**Do not add these navigation buttons now** — they would link to routes that 404. Documented here so
they can be wired up when those features are built. (`CourseInCertification` / `CourseJobCategories`
are junction tables handled as N-N below, not Primary-Foreign links.)

---

## N-N Relationships

### 1. Course ↔ JobCategory (職務類別)

- Junction: **`CourseJobCategories`** (`Course_pkid`, `JobCategory_pkid`) — a pure two-FK junction
  (composite PK, `ON UPDATE/DELETE CASCADE` from Course).
- Related entity B: **`JobCategory`** (`pkid` smallint, `Description` nvarchar(70)).
- Lookup endpoint: `GET /api/lookups/job-categories` (**new** — see Lookup Endpoints).
- **List**: show a `jobCategoryCount` subquery (count of associated categories), not full labels.
- **Detail**: list the associated `JobCategory.Description` labels (chips).
- **Form**: `p-multiselect` of job categories (`[maxSelectedLabels]="9999"`).
- Request field: `JobCategoryPkids: List<short>`.
- Read on GET-by-id: second query on the same connection joining `CourseJobCategories → JobCategory`.

### 2. Course ↔ Certification (對應認證)

- Junction: **`CourseInCertification`** (`Course_pkid`, `Certification_pkid`) — pure two-FK junction
  (composite PK, `ON UPDATE/DELETE CASCADE` from Course).
- Related entity B: **`Certification`** (`pkid` int, `Title` **nchar(100)** — needs `RTRIM()`).
- Lookup endpoint: `GET /api/lookups/certifications` (**new** — see Lookup Endpoints).
- **List**: show a `certificationCount` subquery.
- **Detail**: list the associated `Certification.Title` labels (chips).
- **Form**: `p-multiselect` of certifications (`[maxSelectedLabels]="9999"`).
- Request field: `CertificationPkids: List<int>`.
- Read on GET-by-id: second query on the same connection joining `CourseInCertification → Certification`
  (RTRIM `Certification.Title`).

Sync pattern on save (both, inside the same transaction as the Course INSERT/UPDATE):
```sql
DELETE FROM CourseJobCategories   WHERE Course_pkid = @Pkid;   -- then bulk INSERT from JobCategoryPkids
DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid;   -- then bulk INSERT from CertificationPkids
```

---

## Query Filters

`POST /api/courses/query` accepts:

- **keyword**: string — LIKE on `Title`, `OfficialTitle`, `CourseId`, `ProdCourseId`, `FriendlyUrl`.
  (Large text columns `Objective`, `Prerequisites`, `Outline`, `TowardCertOrExam`, `Note`, `OtherInfo`,
  `Material`, `Target` are excluded — slow and rarely useful.)
- **partnerPkid** (`short?`): exact match on `Partner_pkid`. Dropdown from `GET /api/lookups/partners`.
- **courseGroupPkid** (`short?`): exact match on `CourseGroup_pkid`. Dropdown from `GET /api/lookups/course-groups`.
- **publishStatusPkid** (`byte?`): exact match on `PublishStatus_pkid`. Dropdown from `GET /api/lookups/publish-statuses`.
- **canRepeat** (`bool?`): tri-state — null = no filter, true = only 允許重聽, false = only 不可重聽.
- **scheduleOnFrom / scheduleOnTo** (`DateOnly?`): inclusive range on `ScheduleOn`.
- **scheduleOffFrom / scheduleOffTo** (`DateOnly?`): inclusive range on `ScheduleOff`.

Incoming cross-entity nav param: `partnerPkid` (from Partner detail's Primary-Foreign link) pre-fills
the Partner filter — accepted as a query param on the list route.

---

## Lookup Endpoints Required

| Route | Status | Returns |
|-------|--------|---------|
| `GET /api/lookups/partners` | **Exists** | `{ pkid, name }[]` ordered `DisplayOrder ASC` |
| `GET /api/lookups/course-groups` | **Exists** | `{ pkid, description }[]` ordered `Description ASC` |
| `GET /api/lookups/publish-statuses` | **Exists** | `{ pkid, description }[]` ordered `pkid ASC` |
| `GET /api/lookups/job-categories` | **New** | `{ pkid, description }[]` from `JobCategory` ordered `Description ASC` |
| `GET /api/lookups/certifications` | **New** | `{ pkid, title }[]` from `Certification` (`RTRIM(Title)`) ordered `pkid ASC` |

New lookup models: `Models/JobCategoryLookup.cs` (`{ short Pkid, string Description }`),
`Models/CertificationLookup.cs` (`{ int Pkid, string Title }`). Add both to `ILookupRepository` /
`LookupRepository` / `LookupsController` and to the Angular `LookupService`.

---

## API Endpoints

| Method | Route | Notes |
|--------|-------|-------|
| `GET` | `/api/courses` | List all (with FK nav labels + n-n counts) |
| `POST` | `/api/courses/query` | Filtered query (body: `CourseQuery`) |
| `GET` | `/api/courses/{id}` | Get by pkid (int) — includes FK nav objects + n-n pkid lists |
| `POST` | `/api/courses` | Create (pkid auto-generated) |
| `PUT` | `/api/courses` | Update (pkid from body) |
| `DELETE` | `/api/courses/{id}` | Delete (junction rows cascade in DB; also cleared in txn) |

No auth exceptions; same policy as the other controllers. `pkid` is IDENTITY (auto), so **no**
duplicate-business-key 409 check on create (contrast with `AppRole.RoleId` / `PublishStatus.pkid`).

---

## Backend Notes

### Models

```csharp
// Models/Course.cs — response (list + detail)
public class Course
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }

    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }

    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    // FK nav objects (multi-map)
    public PartnerLookup? Partner { get; set; }
    public CourseGroupLookup? CourseGroup { get; set; }
    public PublishStatusLookup? PublishStatus { get; set; }

    // N-N (populated on GetById; counts on list/query)
    public int JobCategoryCount { get; set; }
    public int CertificationCount { get; set; }
    public List<short> JobCategoryPkids { get; set; } = new();
    public List<int> CertificationPkids { get; set; } = new();
    public List<JobCategoryLookup> JobCategories { get; set; } = new();
    public List<CertificationLookup> Certifications { get; set; } = new();
}

// Models/CourseRequest.cs — write DTO
public class CourseRequest
{
    public int Pkid { get; set; }  // present on update, ignored on insert (IDENTITY)

    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(300)] public string? OfficialTitle { get; set; }
    [Required, MaxLength(50)]  public string CourseId { get; set; } = string.Empty;
    [Required, MaxLength(50)]  public string ProdCourseId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }

    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }

    [MaxLength(500)]  public string? Material { get; set; }
    [MaxLength(4000)] public string? Objective { get; set; }
    [MaxLength(500)]  public string? Target { get; set; }
    [MaxLength(4000)] public string? Prerequisites { get; set; }
    public string? Outline { get; set; }            // nvarchar(max)
    public string? TowardCertOrExam { get; set; }   // nvarchar(max)
    [MaxLength(4000)] public string? Note { get; set; }
    [MaxLength(4000)] public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    public List<short> JobCategoryPkids { get; set; } = new();
    public List<int> CertificationPkids { get; set; } = new();
}

// Models/CourseQuery.cs — search DTO
public class CourseQuery
{
    public string? Keyword { get; set; }
    public short? PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte? PublishStatusPkid { get; set; }
    public bool? CanRepeat { get; set; }
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public DateOnly? ScheduleOffFrom { get; set; }
    public DateOnly? ScheduleOffTo { get; set; }
}
```

### SQL — SELECT (list / query)

Multi-map with three nav objects + two count subqueries. `splitOn: "PartnerPkid,CourseGroupPkid,PublishStatusPkid"`
(the leading alias of each nav block). No `nchar` columns on `Course` → no RTRIM on Course itself.

```sql
SELECT c.pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl, c.DisplayOrder,
       c.Partner_pkid AS PartnerPkid, c.CourseGroup_pkid AS CourseGroupPkid, c.PublishStatus_pkid AS PublishStatusPkid,
       c.ScheduleOn, c.ScheduleOff, c.Hour, c.ListPrice, c.LearningCredit, c.CanRepeat,
       (SELECT COUNT(*) FROM CourseJobCategories cj  WHERE cj.Course_pkid = c.pkid) AS JobCategoryCount,
       (SELECT COUNT(*) FROM CourseInCertification ci WHERE ci.Course_pkid = c.pkid) AS CertificationCount,
       -- nav: Partner
       p.pkid  AS PartnerPkid,  p.Name        AS Name,
       -- nav: CourseGroup
       g.pkid  AS CourseGroupPkid, g.Description AS Description,
       -- nav: PublishStatus
       s.pkid  AS PublishStatusPkid, s.Description AS Description
FROM Course c
JOIN Partner p        ON p.pkid = c.Partner_pkid
LEFT JOIN CourseGroup g ON g.pkid = c.CourseGroup_pkid
JOIN PublishStatus s  ON s.pkid = c.PublishStatus_pkid
-- GetAll:  ORDER BY c.DisplayOrder ASC, c.pkid DESC
-- GetById: WHERE c.pkid = @Pkid  (then two extra n-n queries below)
```

> Multi-map splitOn note: give each nav block a distinct leading column so Dapper can split cleanly.
> Follow the `AppRoleRepository` multi-map precedent for the exact `Query<Course, PartnerLookup,
> CourseGroupLookup, PublishStatus, Course>(...)` signature and `splitOn` string. Text columns not
> shown on the list (Objective, Outline, etc.) are still returned for GetById; the list/query path may
> select the trimmed column set for performance, but returning all is acceptable for this pass.

Query WHERE:
```sql
WHERE (@Keyword IS NULL
       OR c.Title        LIKE '%' + @Keyword + '%'
       OR c.OfficialTitle LIKE '%' + @Keyword + '%'
       OR c.CourseId     LIKE '%' + @Keyword + '%'
       OR c.ProdCourseId LIKE '%' + @Keyword + '%'
       OR c.FriendlyUrl  LIKE '%' + @Keyword + '%')
  AND (@PartnerPkid IS NULL       OR c.Partner_pkid = @PartnerPkid)
  AND (@CourseGroupPkid IS NULL   OR c.CourseGroup_pkid = @CourseGroupPkid)
  AND (@PublishStatusPkid IS NULL OR c.PublishStatus_pkid = @PublishStatusPkid)
  AND (@CanRepeat IS NULL         OR c.CanRepeat = @CanRepeat)
  AND (@ScheduleOnFrom IS NULL    OR c.ScheduleOn >= @ScheduleOnFrom)
  AND (@ScheduleOnTo IS NULL      OR c.ScheduleOn <= @ScheduleOnTo)
  AND (@ScheduleOffFrom IS NULL   OR c.ScheduleOff >= @ScheduleOffFrom)
  AND (@ScheduleOffTo IS NULL     OR c.ScheduleOff <= @ScheduleOffTo)
ORDER BY c.DisplayOrder ASC, c.pkid DESC
```

### N-N read (GetById), same connection

```sql
SELECT jc.JobCategory_pkid FROM CourseJobCategories jc WHERE jc.Course_pkid = @Pkid;              -- JobCategoryPkids
SELECT ci.Certification_pkid FROM CourseInCertification ci WHERE ci.Course_pkid = @Pkid;          -- CertificationPkids
-- label lists for detail chips:
SELECT j.pkid AS Pkid, j.Description FROM CourseJobCategories jc JOIN JobCategory j ON j.pkid = jc.JobCategory_pkid WHERE jc.Course_pkid = @Pkid;
SELECT ct.pkid AS Pkid, RTRIM(ct.Title) AS Title FROM CourseInCertification ci JOIN Certification ct ON ct.pkid = ci.Certification_pkid WHERE ci.Course_pkid = @Pkid;
```

### SQL — INSERT

Exclude IDENTITY `pkid`. Wrap INSERT + junction sync in one transaction.

```sql
INSERT INTO Course (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
    Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, Hour, ListPrice,
    LearningCredit, Material, Objective, Target, Prerequisites, Outline, TowardCertOrExam, Note,
    OtherInfo, CanRepeat)
VALUES (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
    @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff, @Hour, @ListPrice,
    @LearningCredit, @Material, @Objective, @Target, @Prerequisites, @Outline, @TowardCertOrExam, @Note,
    @OtherInfo, @CanRepeat);
SELECT CAST(SCOPE_IDENTITY() AS int);
-- then: bulk INSERT CourseJobCategories / CourseInCertification from the pkid lists
```

### SQL — UPDATE

```sql
UPDATE Course SET
    Title=@Title, OfficialTitle=@OfficialTitle, CourseId=@CourseId, ProdCourseId=@ProdCourseId,
    FriendlyUrl=@FriendlyUrl, DisplayOrder=@DisplayOrder, Partner_pkid=@PartnerPkid,
    CourseGroup_pkid=@CourseGroupPkid, PublishStatus_pkid=@PublishStatusPkid, ScheduleOn=@ScheduleOn,
    ScheduleOff=@ScheduleOff, Hour=@Hour, ListPrice=@ListPrice, LearningCredit=@LearningCredit,
    Material=@Material, Objective=@Objective, Target=@Target, Prerequisites=@Prerequisites,
    Outline=@Outline, TowardCertOrExam=@TowardCertOrExam, Note=@Note, OtherInfo=@OtherInfo,
    CanRepeat=@CanRepeat
WHERE pkid=@Pkid;
-- then: DELETE + re-INSERT both junction tables
```

### SQL — DELETE

```sql
DELETE FROM CourseJobCategories   WHERE Course_pkid = @Pkid;
DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid;
DELETE FROM Course WHERE pkid = @Pkid;
```
(The DB FKs cascade, but explicit deletes keep the repo self-consistent and match the txn pattern.)

### Special Column Notes

- `ScheduleOn` / `ScheduleOff` are `date` → C# `DateOnly` (handlers already registered in `Program.cs`).
- `ListPrice` decimal(9,0), `LearningCredit` decimal(9,1) → C# `decimal`.
- `Hour` smallint → `short`; `PublishStatus_pkid` tinyint → `byte`; `Partner_pkid`/`CourseGroup_pkid`
  smallint → `short`/`short?`.
- FK columns have underscores (`Partner_pkid`, etc.) — **alias to PascalCase** in every SELECT.
- `Course` itself has **no** `nchar` columns. The `certifications` lookup reads `Certification.Title`
  which **is** `nchar(100)` → `RTRIM()`.
- **RowAudit**: reference features do not use RowAudit; do not add it here.

---

## Frontend Notes

### Route table

| Path | Component |
|------|-----------|
| `/courses` | `CourseListComponent` |
| `/courses/new` | `CourseFormComponent` (add) |
| `/courses/:id` | `CourseDetailComponent` |
| `/courses/:id/edit` | `CourseFormComponent` (edit) |

Register lazy `loadComponent` routes; **`/new` before `/:id`**. `:id` is the numeric `pkid`.

### Angular model

```ts
export interface Course {
  pkid: number;
  title: string;
  officialTitle: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid: number | null;
  publishStatusPkid: number;
  scheduleOn: string;   // ISO date
  scheduleOff: string;  // ISO date
  hour: number;
  listPrice: number;
  learningCredit: number;
  material: string | null;
  objective: string | null;
  target: string | null;
  prerequisites: string | null;
  outline: string | null;
  towardCertOrExam: string | null;
  note: string | null;
  otherInfo: string | null;
  canRepeat: boolean;
  partner: PartnerLookup | null;
  courseGroup: CourseGroupLookup | null;
  publishStatus: PublishStatusLookup | null;
  jobCategoryCount: number;
  certificationCount: number;
  jobCategoryPkids: number[];
  certificationPkids: number[];
  jobCategories: JobCategoryLookup[];
  certifications: CertificationLookup[];
}
```

### List component

- Columns (from the /crud request): 主代碼 `pkid`, 顯示順序 `displayOrder`, 簡介代碼 `courseId`,
  科目代碼 `prodCourseId`, 課程名稱 `title`, 原廠 `partner.name`, 課程群組 `courseGroup.description`,
  上架狀態 `publishStatus.description`, 上架日期 `scheduleOn`, 下架日期 `scheduleOff`, 時數 `hour`,
  定價 `listPrice`, 點數 `learningCredit`, 允許重聽 `canRepeat` (boolean → `是/否` or tag).
- Sortable/paginated `p-table`; default sort `displayOrder ASC` (secondary `pkid DESC` server-side).
- Filter drawer (`p-drawer`): keyword input; three `p-select` (Partner, CourseGroup, PublishStatus,
  `appendTo="body"`, `[filter]="true"`); `CanRepeat` tri-state select; two date-range pairs
  (`p-datepicker`) for ScheduleOn / ScheduleOff.
- Load lookups via `forkJoin` on init; restore saved filters after lookups load.
- Session storage keys: `course-list-filters`, `course-list-sort`, `course-list-page`.
- Accept incoming `partnerPkid` query param → pre-fill Partner filter (overrides saved state).

### Detail component

- Show all fields; FK columns as labels (optionally `routerLink` to target detail pages — all three
  targets are built). N-N shown as chip lists (`jobCategories`, `certifications`).
- **No** Primary-Foreign link buttons this pass (CourseFAQ / CourseRelatedLink / HotCourse not built).

### Form component

- Reactive Forms; sticky `p-toolbar`; `forkJoin` for parallel lookups (partners, course-groups,
  publish-statuses, job-categories, certifications).
- Controls: text inputs (`title`, `officialTitle`, `courseId`, `prodCourseId`, `friendlyUrl`);
  `p-inputnumber` (`displayOrder`, `hour`, `listPrice`, `learningCredit` — `learningCredit` with
  `[minFractionDigits]="1"`); `p-select` for the three FKs (CourseGroup allows null);
  `p-datepicker` for `scheduleOn` / `scheduleOff`; `p-multiselect` for `jobCategoryPkids` /
  `certificationPkids` (`[maxSelectedLabels]="9999"`, chips wrapped via `::ng-deep`);
  `p-checkbox`/`p-toggleswitch` for `canRepeat`; `textarea` for the long text fields
  (`material`, `objective`, `target`, `prerequisites`, `outline`, `towardCertOrExam`, `note`,
  `otherInfo`).
- Required validators on: `title`, `courseId`, `prodCourseId`, `friendlyUrl`, `displayOrder`,
  `partnerPkid`, `publishStatusPkid`, `scheduleOn`, `scheduleOff`, `hour`, `listPrice`,
  `learningCredit`.
- Date serialization uses local components via `core/utils/date.util.ts` `toIso()` (never
  `toISOString()`), and convert ISO ↔ `Date` on load/save.
- `pkid` is IDENTITY (auto) → not shown as editable; no immutable-key handling.

### Delete Confirmation Message

```
確定要刪除主代碼 <b>${item.pkid}</b>「${item.title}」？
```

### Session Storage Keys

| Key | Contents |
|-----|----------|
| `course-list-filters` | Last query filter values |
| `course-list-sort` | `{ sortField, sortOrder }` |
| `course-list-page` | `{ first, rows }` |

### Sub-panels (edit mode only)

**N/A** for this pass. (`CourseFAQ` / `CourseRelatedLink` would be candidates once those features and
inline-edit infra exist — deferred.)

---

## Sidebar Placement

- Nav group: **課程管理 Course** (exists in `app.ts` `navSections`, already has Partner + CourseGroup).
- Entry: **課程 Course** → `/courses` (icon e.g. `pi pi-book`). Add as a child alongside the existing two.

---

## Files to Create / Modify

### Backend (`CMS.API`)
| File | Action |
|------|--------|
| `Models/Course.cs`, `CourseRequest.cs`, `CourseQuery.cs` | create |
| `Models/JobCategoryLookup.cs`, `CertificationLookup.cs` | create (new lookups) |
| `Repositories/ICourseRepository.cs`, `CourseRepository.cs` | create |
| `Controllers/CoursesController.cs` | create |
| `Repositories/ILookupRepository.cs` + `LookupRepository.cs` + `Controllers/LookupsController.cs` | modify — add `job-categories`, `certifications` |
| `Program.cs` | modify — register `ICourseRepository` |

### Frontend (`CMS.NG`)
| File | Action |
|------|--------|
| `core/models/course.model.ts` | create |
| `core/models/job-category-lookup.model.ts`, `certification-lookup.model.ts` | create |
| `core/services/course.service.ts` | create |
| `core/services/lookup.service.ts` | modify — add `getJobCategories()`, `getCertifications()` |
| `features/courses/course-list/`, `course-detail/`, `course-form/` | create |
| `app.routes.ts` | modify — add lazy routes (`/new` before `/:id`) |
| `app.ts` | modify — sidebar entry under 課程管理 Course |

### Tests
| File | Action |
|------|--------|
| `CMS.API.Tests/CoursesControllerTests.cs` + `Fakes/FakeCourseRepository.cs` + `CmsApiFactory.cs` registration | create/modify |
| `course.service.spec.ts` | create — URL/verb per method |
| `course-list/detail/form.spec.ts` | create — render + required-field enforcement |
</content>
</invoke>
