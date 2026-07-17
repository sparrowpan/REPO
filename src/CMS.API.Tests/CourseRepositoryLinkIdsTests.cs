using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Repositories;
using CMS.API.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// Regression: ISSUE-004 — Course list rows arrived with empty n-n id lists, and inline edit PUT them
/// straight back, silently wiping every JobCategory / Certification link on the edited course.
/// Found by /qa on 2026-07-17.
/// Report: qa/qa-report-localhost-4200-2026-07-17.md
///
/// <c>SelectColumns</c> carried only <c>JobCategoryCount</c> / <c>CertificationCount</c>, never the id
/// arrays — only <c>GetByPkidAsync</c> ran the extra SELECTs. So list rows had no ids, and
/// <c>course-list.ts toRequest()</c> coerced the missing arrays to <c>[]</c> and sent them; the repository's
/// sync is delete-then-reinsert, so it deleted every link and reinserted nothing. The toast said 已更新.
///
/// The API suite could never catch this: <see cref="Fakes.FakeCourseRepository"/> keeps the ids in memory
/// and hands them back from list *and* detail, so the fake is more generous than the real SQL. These run the
/// REAL repository against SQLite — the same approach <see cref="PublishStatusRepositoryAuditTests"/> uses,
/// and the only place the query's shape is actually asserted.
///
/// Scope: <c>GetAllAsync</c> only. <c>QueryAsync</c> shares the same <c>SelectColumns</c> and helper but its
/// WHERE uses SQL Server's <c>'%' + @Keyword + '%'</c> concatenation, which SQLite does not parse.
/// </summary>
public sealed class CourseRepositoryLinkIdsTests : IDisposable
{
    private readonly string _connectionString = $"Data Source=CourseLinks-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly CourseRepository _repo;

    static CourseRepositoryLinkIdsTests()
    {
        // Program.cs registers these when the WebApplicationFactory suites boot; this class never boots the
        // app, so register them here too. Dapper's handler table is global and idempotent for a given type.
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    public CourseRepositoryLinkIdsTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        CreateSchema(_keepAlive);
        SeedCourses(_keepAlive);

        var audit = new RowAuditWriter(new HttpContextAccessor { HttpContext = null });
        _repo = new CourseRepository(new SqliteConnectionFactory(_connectionString), audit);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task GetAll_CarriesTheJobCategoryIds_NotJustTheCount()
    {
        var courses = await _repo.GetAllAsync(CancellationToken.None);

        var linked = courses.Single(c => c.Pkid == 1);
        // Pre-fix this was [] while the count still said 3 — the shape that fed the wipe.
        Assert.Equal([10, 20, 30], linked.JobCategoryPkids);
        Assert.Equal(3, linked.JobCategoryCount);
    }

    [Fact]
    public async Task GetAll_CarriesTheCertificationIds_NotJustTheCount()
    {
        var courses = await _repo.GetAllAsync(CancellationToken.None);

        var linked = courses.Single(c => c.Pkid == 1);
        Assert.Equal([40, 50], linked.CertificationPkids);
        Assert.Equal(2, linked.CertificationCount);
    }

    [Fact]
    public async Task GetAll_GivesEachCourseOnlyItsOwnLinks()
    {
        var courses = await _repo.GetAllAsync(CancellationToken.None);

        // Course 2 has its own single link — the batched fetch must not smear course 1's ids across rows.
        Assert.Equal([20], courses.Single(c => c.Pkid == 2).JobCategoryPkids);
        Assert.Empty(courses.Single(c => c.Pkid == 2).CertificationPkids);
    }

    [Fact]
    public async Task GetAll_LeavesAnUnlinkedCourseEmpty()
    {
        var courses = await _repo.GetAllAsync(CancellationToken.None);

        var unlinked = courses.Single(c => c.Pkid == 3);
        Assert.Empty(unlinked.JobCategoryPkids);
        Assert.Empty(unlinked.CertificationPkids);
    }

    private static void CreateSchema(IDbConnection conn)
    {
        conn.Execute("""
            CREATE TABLE Partner (pkid INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE CourseGroup (pkid INTEGER PRIMARY KEY, Description TEXT NOT NULL);
            CREATE TABLE PublishStatus (pkid INTEGER PRIMARY KEY, Description TEXT NOT NULL);

            CREATE TABLE Course (
                pkid              INTEGER PRIMARY KEY AUTOINCREMENT,
                Title             TEXT    NOT NULL,
                OfficialTitle     TEXT    NULL,
                CourseId          TEXT    NOT NULL,
                ProdCourseId      TEXT    NULL,
                FriendlyUrl       TEXT    NULL,
                DisplayOrder      INTEGER NOT NULL,
                Partner_pkid      INTEGER NOT NULL,
                CourseGroup_pkid  INTEGER NULL,
                PublishStatus_pkid INTEGER NOT NULL,
                ScheduleOn        TEXT    NOT NULL,
                ScheduleOff       TEXT    NOT NULL,
                [Hour]            INTEGER NULL,
                ListPrice         INTEGER NULL,
                LearningCredit    REAL    NULL,
                Material          TEXT    NULL,
                Objective         TEXT    NULL,
                Target            TEXT    NULL,
                Prerequisites     TEXT    NULL,
                Outline           TEXT    NULL,
                TowardCertOrExam  TEXT    NULL,
                Note              TEXT    NULL,
                OtherInfo         TEXT    NULL,
                CanRepeat         INTEGER NOT NULL
            );

            CREATE TABLE CourseJobCategories (Course_pkid INTEGER NOT NULL, JobCategory_pkid INTEGER NOT NULL);
            CREATE TABLE CourseInCertification (Course_pkid INTEGER NOT NULL, Certification_pkid INTEGER NOT NULL);

            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL
            );
            """);
    }

    private static void SeedCourses(IDbConnection conn)
    {
        conn.Execute("""
            INSERT INTO Partner (pkid, Name) VALUES (1, '微軟');
            INSERT INTO CourseGroup (pkid, Description) VALUES (1, '雲端系列');
            INSERT INTO PublishStatus (pkid, Description) VALUES (1, '草稿');

            INSERT INTO Course (pkid, Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
                Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, [Hour], ListPrice,
                LearningCredit, Material, Objective, Target, Prerequisites, Outline, TowardCertOrExam, Note,
                OtherInfo, CanRepeat)
            VALUES
                (1, '多重關聯課程', '多重關聯課程', 'C1', 'P1', 'c1', 0, 1, 1, 1, '2026-01-01', '2026-12-31',
                 10, 100, 1.0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1),
                (2, '單一關聯課程', '單一關聯課程', 'C2', 'P2', 'c2', 0, 1, 1, 1, '2026-01-01', '2026-12-31',
                 10, 100, 1.0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1),
                (3, '無關聯課程', '無關聯課程', 'C3', 'P3', 'c3', 0, 1, 1, 1, '2026-01-01', '2026-12-31',
                 10, 100, 1.0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1);

            INSERT INTO CourseJobCategories (Course_pkid, JobCategory_pkid) VALUES (1, 10), (1, 20), (1, 30), (2, 20);
            INSERT INTO CourseInCertification (Course_pkid, Certification_pkid) VALUES (1, 40), (1, 50);
            """);
    }

    /// <summary>Hands the repository fresh connections to the shared in-memory SQLite database.</summary>
    private sealed class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
