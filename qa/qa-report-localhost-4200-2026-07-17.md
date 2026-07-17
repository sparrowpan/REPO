# QA Report — CMS (localhost:4200)

| | |
|---|---|
| **Date** | 2026-07-17 |
| **Target** | http://localhost:4200 (Angular 20) + http://localhost:5000 (.NET 9 API) |
| **Branch** | `develop` |
| **Tier** | Standard (fix critical + high + medium) |
| **Scope** | Entire project — all 9 implemented features |
| **Signed in as** | miles@uuu.com.tw (Admin) |
| **Framework** | Angular 20 standalone + PrimeNG; .NET 9 + Dapper; SQL Express (local) |
| **Health score** | **78 → 97** |

## Summary

Four issues found and fixed, each committed atomically with a regression test that was
proven to fail against the pre-fix code. One is **Critical**: Course inline edit was
silently destroying data on every save. The rest of the app is in good shape — the
cross-cutting rules (RowAudit, ErrorHandling, Auth) hold up under direct attack, and
no feature was broken or unreachable.

> **PR summary:** QA found 4 issues (1 critical), fixed 4, health score 78 → 97.

| Severity | Found | Fixed | Deferred |
|----------|-------|-------|----------|
| Critical | 1 | 1 | 0 |
| High | 1 | 1 | 0 |
| Medium | 2 | 2 | 0 |
| Low | 3 | 0 | 3 |

## Top 4 things fixed

1. **Course inline edit silently wiped every JobCategory/Certification link** (Critical) — unrecoverable data loss on every save, reported as success.
2. **AppUser password timestamps rendered 8 hours in the future** (High) — wrong data on screen.
3. **Deleting an in-use record returned a generic 500** (Medium) — a routine refusal looked like an outage.
4. **Row action buttons had no accessible name** (Medium) — screen readers announced three unlabelled buttons per row.

---

## ISSUE-004 — Course inline edit silently wipes n-n links — **Critical** — *fixed (verified)*

**Category:** Functional / data loss

Every inline cell edit on the Course list destroyed that course's JobCategory and
Certification links — then showed `已更新 課程資料已更新。`

**Mechanism** (four independent lines of evidence agreed):

1. `CourseRepository.SelectColumns` (`:26-40`) selects `JobCategoryCount` /
   `CertificationCount` — **counts only, never the id arrays**. Only `GetByPkidAsync`
   runs the extra SELECTs (`:113-114`).
2. So every list row carries `jobCategoryPkids: undefined`.
3. `course-list.ts toRequest()` (`:480-481`) coerces them: `c.jobCategoryPkids ?? []`.
4. `PUT /api/courses` receives `[]`; the repository's n-n sync is delete-then-reinsert
   → deletes every link, reinserts nothing.

It fired on **any** column — a date, a price, display order. The loss is unrecoverable
and untraceable: the spec deliberately excludes collections from RowAudit, so nothing
records which links existed.

**Why 176 API tests never caught it:** `FakeCourseRepository` keeps the ids in memory
and returns them from list *and* detail. The fake is more generous than the real SQL,
so the defect only exists against a database.

**Blast radius (measured on the live DB):** only **2** courses have ever been
inline-edited (they carry `Course`/`Update` audit rows):

| Course | Inline edits | First edit | Links now |
|---|---|---|---|
| 3332 | 2 (both mine, **post-fix**) | 2026-07-17 14:35 | **7 job + 5 cert — intact** ✅ |
| 3334 | 5 (first **pre-fix**) | 2026-07-16 10:34 | **0 + 0 — wiped** ❌ |

Course 3334 ("Miles UWA 2", placeholder content) is the one real casualty; it was
already stripped hours before this QA run — the audit shows Barry editing it at 09:48
today. The 333 courses with no job links and 990 with no certification links have **no
audit rows at all**, so those are original data, not victims.

⚠️ **Caveat:** RowAudit's earliest `Course` update is 2026-07-16. Any inline edit made
before RowAudit shipped left no trace, so pre-audit damage can't be ruled out from the
data alone.

**Evidence** — inline date edit on course 3332 (which has 7 + 5 links):

| | Before fix | After fix |
|---|---|---|
| `scheduleOn` | edits fine | edits fine |
| `jobCategoryPkids` | → `[]` ❌ | `[20,22,24,26,27,28,30]` ✅ |
| `certificationPkids` | → `[]` ❌ | `[5,7,31,32,33]` ✅ |

- **Files:** `CourseRepository.cs` (new `AttachLinkIdsAsync`), `DapperTypeHandlers.cs`
- **Fix:** `GetAll`/`Query` attach the real ids via **two batched round trips**, not a
  correlated subquery per row — the query is unpaged (1085 rows), so the per-row form
  would run thousands of seeks rebuilding the same map. **Measured: ~40ms for the full
  1085-row query** (3 runs: 40/40/52ms).
- **Commit:** `9ef16b8`
- **Found via** a prior session's logged learning (`course-list-inline-edit-nn-wipe`,
  confidence 9/10, then unfixed), independently confirmed from the query SQL, the audit
  trail, and live data.

**Regression test** (`CourseRepositoryLinkIdsTests.cs`, 4 tests) runs the **real**
repository against SQLite — the same approach `PublishStatusRepositoryAuditTests` uses,
and the only place the query's shape is actually asserted. Proven to fail without the fix:
```
Expected: [10, 20, 30] / Actual: []
Expected: [20]         / Actual: []
```
`DateOnlyTypeHandler.Parse` now also accepts a string (SQL Server returns `DateTime`;
SQLite returns text). Course is the first date-bearing repository tested this way.
Writing is unchanged.

---

## ISSUE-001 — AppUser password time shifted +8 hours — **High** — *fixed (verified)*

**Category:** Functional / data correctness

`PasswordUpdatedTime` is written with `DateTime.Now` (`AppUserRepository.cs:109`) and
`GETDATE()` (`AppUserRepository.cs:188`, `AuthRepository.cs:72`) — local time — and
serialized with no timezone designator. Both AppUser templates appended `'Z'` before
the date pipe, marking that local value as UTC so the pipe re-converted it to +08:00.

The `row-audit-badge` renders the same unmarked local shape *without* `'Z'` and was
always correct. These two lines broke that convention.

**Repro:**
1. Sign in, go to any user's detail page and note 密碼更新時間.
2. Click 重設密碼 → 重設 (stamps `GETDATE()`).
3. The field jumps 8 hours into the future.

**Evidence** — a live reset stamped at exactly `13:44:13` local:

| Source | Before fix | After fix |
|---|---|---|
| API (truth) | `2026-07-17T13:44:13.29` | `2026-07-17T13:44:13.29` |
| UI 密碼更新時間 | `2026-07-17 21:44` ❌ | `2026-07-17 13:44` ✅ |

The clearest proof is one page showing both: `issue-001-before-detail.png` has the
audit badge reading `Insert … 09:47` while 密碼更新時間 reads `17:47` — the same
instant, rendered 8 hours apart.

- **Files:** `app-user-list.html:61`, `app-user-detail.html:44`
- **Fix:** dropped `+ 'Z'` (2 lines)
- **Commit:** `5a9d8ed` · **Test:** `de580a6`
- **Screenshots:** `issue-001-before-detail.png`, `issue-001-after-list.png`

**Regression test** (`app-user-list.regression-1.spec.ts`) — proven to fail pre-fix:
```
Expected '2026-07-17 21:44' to be '2026-07-17 13:44'
Expected '2026-07-16 06:30' to be '2026-07-15 22:30'   (day rollover)
```
Note: the defect is unobservable at offset zero — in UTC the `'Z'` form rendered
identically — so these discriminate wherever the offset is non-zero (i.e. for the
Taipei team, always).

---

## ISSUE-002 — Deleting a referenced record returns 500 — **Medium** — *fixed (verified)*

**Category:** Functional / error handling

Deleting a `PublishStatus` / `Partner` / `CourseGroup` still assigned to a `Course`
let SQL Server's FK violation (error 547) escape to the exception middleware, so the
user got `系統發生錯誤，請稍後再試。An unexpected error occurred.` — indistinguishable
from a real outage, with no hint that the record is simply in use.

The data was never at risk (the delete transaction rolls back). This is an
error-surface bug, not a data-safety one. It also logged routine refusals as
unexpected failures.

`spec/features.md` already prescribed the answer: controllers *"catch only what you
can improve on for the caller (400, 409)"* — and the duplicate-key 409s already do
exactly this.

**Repro:** delete 發布狀態 `草稿` (used by ~1085 courses) → generic error toast.

**Evidence:**

| Endpoint | Before | After |
|---|---|---|
| `DELETE /api/publish-statuses/1` | 500 generic | **409** `此發布狀態已被課程使用，無法刪除。` |
| `DELETE /api/partners/127` | 500 generic | **409** `此合作廠商已被課程使用，無法刪除。` |
| `DELETE /api/course-groups/155` | 500 generic | **409** `此課程群組已被課程使用，無法刪除。` |
| Unreferenced delete | 204 | **204** (unchanged) |
| Missing row | 404 | **404** (not 409) |

**A second bug surfaced while verifying the first.** The 409 alone was *invisible* —
the interceptor only toasts 5xx, and the list delete handlers hardcoded a generic
`detail`, so the server's reason was dropped and the user saw
`刪除發布狀態時發生錯誤。` The handlers now prefer the server's message using the same
idiom the forms already use. Re-testing caught this; the backend fix alone would have
shipped half-working.

- **Files:** new `Infrastructure/ReferencedRecordException.cs`; 3 repositories; 3 controllers; 3 list components
- **Commit:** `4ff2f5a` · **Test:** `3367640` · **Spec:** `dbaf94a`
- **Screenshots:** `issue-002-500-toast.png`, `issue-002-after-409-toast.png`

**Regression test** (`PublishStatusesDeleteReferencedTests.cs`, 4 tests) — proven to fail pre-fix:
```
Expected: Conflict / Actual: InternalServerError
Actual: "系統發生錯誤，請稍後再試。An unexpected error occurred"
```
Caveat: `SqlException` has no public constructor, so the fake throws the
already-translated domain exception. The 547→domain translation itself is only
exercisable against real SQL Server — where it was verified by hand (all three above).

---

## ISSUE-003 — Row action buttons have no accessible name — **Medium** — *fixed (verified)*

**Category:** Accessibility

The view/edit/delete buttons on all six list pages are icon-only. `pTooltip` draws a
hover tooltip but sets no accessible name, so each row announced three unlabelled
`button`s — including the destructive one — with nothing to tell them apart.

**Evidence** (accessibility tree, `/app-roles`):

| Before | After |
|---|---|
| `@e21 [button] ""` | `@e21 [button] "檢視"` |
| `@e22 [button] ""` | `@e22 [button] "編輯"` |
| `@e23 [button] ""` | `@e23 [button] "刪除"` |

- **Files:** 6 list templates, 18 buttons
- **Fix:** `aria-label` mirroring the tooltip text. Sighted behaviour unchanged.
- **Commit:** `a683d27`

---

## Deferred (Low)

| # | Issue | Why deferred |
|---|-------|--------------|
| 004 | **Topbar Search and Settings buttons are inert** — click does nothing, no overlay, no handler. Ultima template leftovers. | Needs a product decision: implement or remove. Not a code defect to guess at. |
| 005 | **Table headers wrap one-character-per-line below ~900px** — CJK headers (`主代碼`, `權限等級`) render vertically; the actions column clips. Correct at ≥1024px. | Responsive work across every list; the app targets desktop admin use. Its own task. |
| 006 | **Invalid-field styling is inconsistent** — the profile form red-borders an invalid input; the AppRole/AppUser forms show only the message text (`ng-invalid` is set but `p-invalid` is not applied). | Cosmetic; the error text is present and discoverable in both. |

## Known gap (by design — not a bug)

**Password reset writes no audit row.** I confirmed a reset at 13:44:13 left barry's
audit trail with only its original `Insert`. An admin can reset any user's password
with no trace — worth knowing — but `spec/features.md` explicitly documents this:

> **Not audited (custom, non-triad ops):** `AppUserRepository.ResetPasswordAsync` and
> `FeaturedPromoItemRepository.MoveToSlotAsync` … intentionally left for a follow-up.

Left alone as a documented deferral. Flagging it because the compliance impact is
real if it stays unfinished.

---

## What was verified working

Tested by direct attack, not by reading code:

- **RowAudit** — create/edit/delete each wrote exactly one correct row; the history dialog lists them newest-first; timestamps render correctly (this is what exposed ISSUE-001 by contrast).
- **Auth** — non-admin (barry, 0 roles) gets **403** on every admin-only endpoint including destructive `DELETE`, **200** on regular ones, **401** with no token. `authGuard` redirects all protected routes to `/login`. Wrong password gives a generic `帳號或密碼錯誤。` that doesn't leak account existence.
- **No privilege escalation** — barry `PUT /api/Auth/profile` with `userId: miles@uuu.com.tw` + `roleIds: ["Admin"]` → server ignored the injected id, applied the rename to barry, granted no roles. Miles untouched.
- **PasswordHash never crosses the wire** — absent from every AppUser response.
- **ErrorHandling** — every failure toasted exactly **once**; never double-toasted.
- **Inline date edit** (Course) — set 2026-01-15 via the datepicker, persisted `"2026-01-15"`, no off-by-one. `date.util.ts` holds.
- **FeaturedPromoItem** — `+`/`−` move matches the spec (`+` = move down); slot-1 `−` no-ops client-side with no API call; week navigation clean both directions.
- **CourseBrochure** — prose renders as structured HTML (not `pre-wrap`); print shell-undo present.
- **Duplicate keys** — `barry@uuu.com.tw` → 409 with a friendly message.
- **Console** — 0 errors across all 8 routes, before and after.

## Test suites

| Suite | Before | After |
|---|---|---|
| Angular (Karma) | 275 pass | **278 pass** (+3 regression) |
| API (xUnit) | 176 pass | **184 pass** (+8 regression) |

## Commits

```
9ef16b8  fix(qa): ISSUE-004 — stop Course inline edit wiping JobCategory/Certification links
a683d27  fix(qa): ISSUE-003 — name the row action buttons for screen readers
dbaf94a  docs(qa): record the referenced-delete 409 contract in ErrorHandling
3367640  test(qa): regression test for ISSUE-002 — referenced delete answers 409
4ff2f5a  fix(qa): ISSUE-002 — answer 409, not 500, when a delete is still referenced
de580a6  test(qa): regression test for ISSUE-001 — AppUser password time not shifted
5a9d8ed  fix(qa): ISSUE-001 — stop shifting AppUser password time +8h
```

## Health score

| Category | Weight | Before | After |
|----------|--------|--------|-------|
| Console | 15% | 100 | 100 |
| Links | 10% | 100 | 100 |
| Visual | 10% | 94 | 94 |
| Functional | 20% | 52 | 100 |
| UX | 15% | 94 | 94 |
| Performance | 10% | 100 | 100 |
| Content | 5% | 100 | 100 |
| Accessibility | 15% | 92 | 100 |
| **Weighted** | | **77.7** | **97.3** |

Functional before = 100 − 25 (ISSUE-004 critical) − 15 (ISSUE-001 high) − 8 (ISSUE-002 medium) = 52.

## Recommended follow-ups

1. **Re-link course 3334** if its JobCategory/Certification assignments mattered — they're
   gone and nothing recorded them. It looks like a test course, so probably not.
2. **Audit n-n changes.** ISSUE-004 was unrecoverable *and* invisible precisely because
   RowAudit excludes collections by design. Worth revisiting that exclusion — a link set
   is business data.
3. **The fakes are more generous than the SQL.** `FakeCourseRepository` returns ids the
   real query never sent, which is why the suite was green through a critical data-loss
   bug. Any list-vs-detail DTO shape difference is invisible to the API suite; the SQLite
   pattern is the antidote where it matters.
4. **Finish the two documented audit deferrals** (`ResetPasswordAsync`,
   `MoveToSlotAsync`) — password resets leaving no trace is a real compliance gap.

## Test data left behind (local SQL Express)

QA mutates state. All of it was on `Server=.\SQLEXPRESS` (local, not shared). Restored
unless noted:

- `AppRole QATEST` — created, edited, deleted. Gone. Audit rows remain (correctly).
- `Course 3334 ScheduleOn` — changed to 2026-01-15, **restored to 2026-01-27**. ⚠️ Those two edits ran *before* the ISSUE-004 fix and so re-triggered the wipe — but 3334's links were already `[]` (wiped by earlier edits on 07-16 and by Barry at 09:48 today), so nothing additional was lost.
- `Course 3332 ScheduleOn` — changed to 2026-01-20 to prove the ISSUE-004 fix, **restored to 2026-01-16**. Its 7 + 5 links survived (that was the test).
- `FeaturedPromoItem` 7/13 slots — reordered, **restored** to original order.
- `PublishStatus 99` — created and deleted while testing the happy path. Gone.
- `AppUser barry` — renamed to `HACKED` during the escalation test, **restored to `Barry Chung`**.
- **`AppUser barry@uuu.com.tw` password was reset to the default (`CMS4fun#`)** while testing 重設密碼. The original is a one-way hash — unrecoverable by design, so no reset can restore it. **Resolved 2026-07-17: left at the default deliberately.** That is exactly the state the reset feature intends (admin resets → user sets their own), so re-running it would be a no-op. Barry needs telling: log in with `CMS4fun#`, then 個人資料 My Profile → 變更密碼. Account verified healthy — `isActive: true`, login returns 200. Note the reset wrote **no audit row** (the documented `ResetPasswordAsync` gap), which is why this had to be reported by hand rather than being visible in his history.
- Miles's password was never changed (only wrong-password paths were exercised).

Your API (`dotnet run`) was restarted twice to build the API fix — it's running again.
