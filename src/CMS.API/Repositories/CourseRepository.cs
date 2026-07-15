using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Services;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="Course"/> (no EF).</summary>
public sealed class CourseRepository(IDbConnectionFactory connectionFactory, RowAuditWriter audit) : ICourseRepository
{
    private const string TableName = "Course";

    // Own-table scalar columns only (no FK joins / n-n count subqueries) — used for audit before/after
    // snapshots so the changed-column list stays limited to real Course columns.
    private const string AuditSelectColumns = """
        SELECT c.pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl, c.DisplayOrder,
               c.Partner_pkid AS PartnerPkid, c.CourseGroup_pkid AS CourseGroupPkid, c.PublishStatus_pkid AS PublishStatusPkid,
               c.ScheduleOn, c.ScheduleOff, c.[Hour], c.ListPrice, c.LearningCredit,
               c.Material, c.Objective, c.Target, c.Prerequisites, c.Outline, c.TowardCertOrExam, c.Note, c.OtherInfo, c.CanRepeat
        FROM Course c
        """;

    // Course columns first, then the three FK nav blocks (each starting with a `Pkid` split marker).
    // splitOn "Pkid,Pkid,Pkid" boundaries the Partner / CourseGroup / PublishStatus objects.
    private const string SelectColumns = """
        SELECT c.pkid, c.Title, c.OfficialTitle, c.CourseId, c.ProdCourseId, c.FriendlyUrl, c.DisplayOrder,
               c.Partner_pkid AS PartnerPkid, c.CourseGroup_pkid AS CourseGroupPkid, c.PublishStatus_pkid AS PublishStatusPkid,
               c.ScheduleOn, c.ScheduleOff, c.[Hour], c.ListPrice, c.LearningCredit,
               c.Material, c.Objective, c.Target, c.Prerequisites, c.Outline, c.TowardCertOrExam, c.Note, c.OtherInfo, c.CanRepeat,
               (SELECT COUNT(*) FROM CourseJobCategories cj WHERE cj.Course_pkid = c.pkid) AS JobCategoryCount,
               (SELECT COUNT(*) FROM CourseInCertification ci WHERE ci.Course_pkid = c.pkid) AS CertificationCount,
               p.pkid AS Pkid, p.Name AS Name,
               g.pkid AS Pkid, g.Description AS Description,
               s.pkid AS Pkid, s.Description AS Description
        FROM Course c
        JOIN Partner p ON p.pkid = c.Partner_pkid
        LEFT JOIN CourseGroup g ON g.pkid = c.CourseGroup_pkid
        JOIN PublishStatus s ON s.pkid = c.PublishStatus_pkid
        """;

    private const string SplitOn = "Pkid,Pkid,Pkid";

    /// <summary>Multi-map assembler: attach the three FK nav objects to the Course row.</summary>
    private static Course MapRow(Course course, PartnerLookup partner, CourseGroupLookup group, PublishStatusLookup status)
    {
        course.Partner = partner;
        course.CourseGroup = group; // null when the LEFT JOIN produced no CourseGroup row
        course.PublishStatus = status;
        return course;
    }

    public async Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY c.DisplayOrder ASC, c.pkid DESC";
        var rows = await conn.QueryAsync<Course, PartnerLookup, CourseGroupLookup, PublishStatusLookup, Course>(
            new CommandDefinition(sql, cancellationToken: ct), MapRow, splitOn: SplitOn);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL
                   OR c.Title LIKE '%' + @Keyword + '%'
                   OR c.OfficialTitle LIKE '%' + @Keyword + '%'
                   OR c.CourseId LIKE '%' + @Keyword + '%'
                   OR c.ProdCourseId LIKE '%' + @Keyword + '%'
                   OR c.FriendlyUrl LIKE '%' + @Keyword + '%')
              AND (@PartnerPkid IS NULL OR c.Partner_pkid = @PartnerPkid)
              AND (@CourseGroupPkid IS NULL OR c.CourseGroup_pkid = @CourseGroupPkid)
              AND (@PublishStatusPkid IS NULL OR c.PublishStatus_pkid = @PublishStatusPkid)
              AND (@CanRepeat IS NULL OR c.CanRepeat = @CanRepeat)
              AND (@ScheduleOnFrom IS NULL OR c.ScheduleOn >= @ScheduleOnFrom)
              AND (@ScheduleOnTo IS NULL OR c.ScheduleOn <= @ScheduleOnTo)
              AND (@ScheduleOffFrom IS NULL OR c.ScheduleOff >= @ScheduleOffFrom)
              AND (@ScheduleOffTo IS NULL OR c.ScheduleOff <= @ScheduleOffTo)
            ORDER BY c.DisplayOrder ASC, c.pkid DESC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
            query.PartnerPkid,
            query.CourseGroupPkid,
            query.PublishStatusPkid,
            query.CanRepeat,
            query.ScheduleOnFrom,
            query.ScheduleOnTo,
            query.ScheduleOffFrom,
            query.ScheduleOffTo,
        };
        var rows = await conn.QueryAsync<Course, PartnerLookup, CourseGroupLookup, PublishStatusLookup, Course>(
            new CommandDefinition(sql, parameters, cancellationToken: ct), MapRow, splitOn: SplitOn);
        return rows.ToList();
    }

    public async Task<Course?> GetByPkidAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);

        var rowSql = $"{SelectColumns} WHERE c.pkid = @Pkid";
        var rows = await conn.QueryAsync<Course, PartnerLookup, CourseGroupLookup, PublishStatusLookup, Course>(
            new CommandDefinition(rowSql, new { Pkid = pkid }, cancellationToken: ct), MapRow, splitOn: SplitOn);
        var course = rows.FirstOrDefault();
        if (course is null)
            return null;

        // n-n: pkid lists (for the form) + label lists (for the detail chips), same connection.
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition("""
            SELECT JobCategory_pkid FROM CourseJobCategories WHERE Course_pkid = @Pkid ORDER BY JobCategory_pkid;
            SELECT Certification_pkid FROM CourseInCertification WHERE Course_pkid = @Pkid ORDER BY Certification_pkid;
            SELECT j.pkid AS Pkid, j.Description AS Description
              FROM CourseJobCategories cj JOIN JobCategory j ON j.pkid = cj.JobCategory_pkid
             WHERE cj.Course_pkid = @Pkid ORDER BY j.Description;
            SELECT ct.pkid AS Pkid, RTRIM(ct.Title) AS Title
              FROM CourseInCertification ci JOIN Certification ct ON ct.pkid = ci.Certification_pkid
             WHERE ci.Course_pkid = @Pkid ORDER BY ct.pkid;
            """, new { Pkid = pkid }, cancellationToken: ct));

        course.JobCategoryPkids = (await multi.ReadAsync<short>()).ToList();
        course.CertificationPkids = (await multi.ReadAsync<int>()).ToList();
        course.JobCategories = (await multi.ReadAsync<JobCategoryLookup>()).ToList();
        course.Certifications = (await multi.ReadAsync<CertificationLookup>()).ToList();
        return course;
    }

    public async Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var pkid = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO Course (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
                Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff, [Hour], ListPrice,
                LearningCredit, Material, Objective, Target, Prerequisites, Outline, TowardCertOrExam, Note,
                OtherInfo, CanRepeat)
            VALUES (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
                @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff, @Hour, @ListPrice,
                @LearningCredit, @Material, @Objective, @Target, @Prerequisites, @Outline, @TowardCertOrExam, @Note,
                @OtherInfo, @CanRepeat);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request, tx, cancellationToken: ct));

        await SyncJobCategoriesAsync(conn, tx, pkid, request.JobCategoryPkids, ct);
        await SyncCertificationsAsync(conn, tx, pkid, request.CertificationPkids, ct);

        // Audit within the transaction — load the just-inserted row (not yet committed) on the same connection.
        var created = (await LoadForAuditAsync(conn, tx, pkid, ct))!;
        await audit.LogInsert(TableName, created, conn, tx, ct);
        tx.Commit();

        return (await GetByPkidAsync(pkid, ct))!;
    }

    public async Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadForAuditAsync(conn, tx, request.Pkid, ct);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE Course SET
                Title = @Title, OfficialTitle = @OfficialTitle, CourseId = @CourseId, ProdCourseId = @ProdCourseId,
                FriendlyUrl = @FriendlyUrl, DisplayOrder = @DisplayOrder, Partner_pkid = @PartnerPkid,
                CourseGroup_pkid = @CourseGroupPkid, PublishStatus_pkid = @PublishStatusPkid, ScheduleOn = @ScheduleOn,
                ScheduleOff = @ScheduleOff, [Hour] = @Hour, ListPrice = @ListPrice, LearningCredit = @LearningCredit,
                Material = @Material, Objective = @Objective, Target = @Target, Prerequisites = @Prerequisites,
                Outline = @Outline, TowardCertOrExam = @TowardCertOrExam, Note = @Note, OtherInfo = @OtherInfo,
                CanRepeat = @CanRepeat
            WHERE pkid = @Pkid;
            """, request, tx, cancellationToken: ct));

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncJobCategoriesAsync(conn, tx, request.Pkid, request.JobCategoryPkids, ct);
        await SyncCertificationsAsync(conn, tx, request.Pkid, request.CertificationPkids, ct);

        var after = await LoadForAuditAsync(conn, tx, request.Pkid, ct);
        await audit.LogUpdate(TableName, before, after!, conn, tx, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var existing = await LoadForAuditAsync(conn, tx, pkid, ct);
        if (existing is null)
        {
            tx.Rollback();
            return false;
        }

        // Remove junction rows first (DB cascades too, but keep the repo self-consistent).
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseJobCategories WHERE Course_pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseInCertification WHERE Course_pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Course WHERE pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));

        await audit.LogDelete(TableName, existing, conn, tx, ct);
        tx.Commit();
        return true;
    }

    /// <summary>Load a course's own scalar columns on the given connection/transaction, for audit snapshots.</summary>
    private static async Task<Course?> LoadForAuditAsync(IDbConnection conn, IDbTransaction tx, int pkid, CancellationToken ct)
        => await conn.QuerySingleOrDefaultAsync<Course>(
            new CommandDefinition($"{AuditSelectColumns} WHERE c.pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));

    /// <summary>n-n sync: delete-then-reinsert the CourseJobCategories rows for a course.</summary>
    private static async Task SyncJobCategoriesAsync(
        IDbConnection conn, IDbTransaction tx, int coursePkid, List<short> jobCategoryPkids, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseJobCategories WHERE Course_pkid = @CoursePkid", new { CoursePkid = coursePkid }, tx, cancellationToken: ct));

        var distinct = jobCategoryPkids.Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO CourseJobCategories (Course_pkid, JobCategory_pkid) VALUES (@CoursePkid, @JobCategoryPkid)",
            distinct.Select(id => new { CoursePkid = coursePkid, JobCategoryPkid = id }), tx, cancellationToken: ct));
    }

    /// <summary>n-n sync: delete-then-reinsert the CourseInCertification rows for a course.</summary>
    private static async Task SyncCertificationsAsync(
        IDbConnection conn, IDbTransaction tx, int coursePkid, List<int> certificationPkids, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM CourseInCertification WHERE Course_pkid = @CoursePkid", new { CoursePkid = coursePkid }, tx, cancellationToken: ct));

        var distinct = certificationPkids.Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO CourseInCertification (Course_pkid, Certification_pkid) VALUES (@CoursePkid, @CertificationPkid)",
            distinct.Select(id => new { CoursePkid = coursePkid, CertificationPkid = id }), tx, cancellationToken: ct));
    }
}
