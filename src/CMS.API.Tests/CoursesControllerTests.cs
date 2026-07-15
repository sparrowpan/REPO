using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the Course endpoints (list/filter, view, add, edit, delete)
/// running against an in-memory fake repository. Each test gets an isolated factory
/// so mutations don't leak between tests.
/// </summary>
public class CoursesControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    private static CourseRequest ValidRequest(int pkid = 0) => new()
    {
        Pkid = pkid,
        Title = "Terraform 實戰",
        CourseId = "TF101",
        ProdCourseId = "PROD-TF101",
        FriendlyUrl = "tf101",
        DisplayOrder = 5,
        PartnerPkid = 1,
        CourseGroupPkid = 1,
        PublishStatusPkid = 20,
        ScheduleOn = new DateOnly(2026, 3, 1),
        ScheduleOff = new DateOnly(2031, 3, 1),
        Hour = 16,
        ListPrice = 18000,
        LearningCredit = 4.0m,
        CanRepeat = true,
        JobCategoryPkids = [1, 3],
        CertificationPkids = [2],
    };

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededCourses()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var courses = await client.GetFromJsonAsync<List<Course>>("/api/courses");

        Assert.NotNull(courses);
        Assert.Equal(2, courses!.Count);
        Assert.Contains(courses, c => c.Title == "AZ-900 基礎課程");
    }

    [Fact]
    public async Task GetAll_ProjectsFkNavAndNnCounts()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var courses = await client.GetFromJsonAsync<List<Course>>("/api/courses");
        var az = courses!.Single(c => c.CourseId == "AZ900");

        Assert.Equal("微軟", az.Partner!.Name);
        Assert.Equal(2, az.JobCategoryCount);
        Assert.Equal(1, az.CertificationCount);
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByCourseId()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/courses/query", new CourseQuery { Keyword = "PMP" });
        response.EnsureSuccessStatusCode();
        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();

        Assert.Single(courses!);
        Assert.Equal("PMP 專案管理", courses!.Single().Title);
    }

    [Fact]
    public async Task Query_ByPartner_FiltersByFk()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/courses/query", new CourseQuery { PartnerPkid = 2 });
        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();

        Assert.Single(courses!);
        Assert.Equal("PMP", courses!.Single().CourseId);
    }

    [Fact]
    public async Task Query_ByCanRepeat_TriState()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/courses/query", new CourseQuery { CanRepeat = true });
        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();

        Assert.Single(courses!);
        Assert.True(courses!.Single().CanRepeat);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/courses/query", new CourseQuery());
        var courses = await response.Content.ReadFromJsonAsync<List<Course>>();

        Assert.Equal(2, courses!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingCourse_ReturnsCourse()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var course = await client.GetFromJsonAsync<Course>("/api/courses/1");

        Assert.NotNull(course);
        Assert.Equal(1, course!.Pkid);
        Assert.Equal("AZ-900 基礎課程", course.Title);
        Assert.Equal(new[] { (short)1, (short)2 }, course.JobCategoryPkids.ToArray());
    }

    [Fact]
    public async Task GetById_UnknownCourse_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/courses/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewCourse_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/courses", ValidRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Course>();
        Assert.Equal("Terraform 實戰", created!.Title);
        Assert.True(created.Pkid > 0);
        Assert.Equal(2, created.JobCategoryCount);

        var fetched = await client.GetFromJsonAsync<Course>($"/api/courses/{created.Pkid}");
        Assert.Equal("TF101", fetched!.CourseId);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Title / CourseId / ProdCourseId / FriendlyUrl are [Required].
        var request = new CourseRequest
        {
            Title = "",
            CourseId = "",
            ProdCourseId = "",
            FriendlyUrl = "",
            PartnerPkid = 1,
            PublishStatusPkid = 20,
        };

        var response = await client.PostAsJsonAsync("/api/courses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingCourse_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = ValidRequest(pkid: 2);
        request.Title = "PMP 專案管理（改版）";

        var response = await client.PutAsJsonAsync("/api/courses", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<Course>("/api/courses/2");
        Assert.Equal("PMP 專案管理（改版）", fetched!.Title);
    }

    [Fact]
    public async Task Update_UnknownCourse_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/courses", ValidRequest(pkid: 88));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingCourse_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/courses/2");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/courses/2");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownCourse_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/courses/77");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
