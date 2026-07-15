using System.Security.Claims;
using CMS.API.Services;
using Microsoft.AspNetCore.Http;

namespace CMS.API.Tests;

/// <summary>
/// Unit tests for <see cref="RowAuditWriter"/>'s reflection logic — building an audit row from an
/// arbitrary entity without touching the database (the build/resolve helpers take no connection).
/// </summary>
public class RowAuditWriterTests
{
    // A representative entity: int pkid, then Title as the first string property.
    private sealed class Sample
    {
        public int Pkid { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int DisplayOrder { get; set; }
    }

    // An entity whose pkid is not the first property and has no string before it.
    private sealed class NumbersFirst
    {
        public int DisplayOrder { get; set; }
        public int Pkid { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static RowAuditWriter CreateWriter(IHttpContextAccessor accessor)
        => new(accessor);

    private static IHttpContextAccessor AnonymousAccessor()
        => new HttpContextAccessor { HttpContext = null };

    private static IHttpContextAccessor AccessorWithUser(string userName)
    {
        var identity = new ClaimsIdentity([new Claim("userName", userName)], "TestAuth");
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    // --- ActionDesc: first string property (Insert / Delete) ----------------

    [Fact]
    public void BuildInsertEntry_UsesFirstStringProperty_AsActionDesc()
    {
        var writer = CreateWriter(AnonymousAccessor());

        var entry = writer.BuildInsertEntry("Course", new Sample { Pkid = 7, Title = "Intro", Code = "C1" });

        Assert.Equal("Insert", entry.ActionType);
        Assert.Equal("Course", entry.TableName);
        Assert.Equal("Intro", entry.ActionDesc); // Title (first string), not Code
    }

    [Fact]
    public void BuildDeleteEntry_UsesFirstStringProperty_AsActionDesc()
    {
        var writer = CreateWriter(AnonymousAccessor());

        var entry = writer.BuildDeleteEntry("Course", new Sample { Pkid = 9, Title = "Goodbye", Code = "C2" });

        Assert.Equal("Delete", entry.ActionType);
        Assert.Equal("Goodbye", entry.ActionDesc);
    }

    [Fact]
    public void FirstStringProperty_RespectsDeclarationOrder_EvenWhenNotFirstMember()
    {
        var writer = CreateWriter(AnonymousAccessor());

        var entry = writer.BuildInsertEntry("Numbers", new NumbersFirst { Pkid = 1, Name = "First string here" });

        Assert.Equal("First string here", entry.ActionDesc);
    }

    // --- PrimaryKeyValues reads pkid ----------------------------------------

    [Fact]
    public void PrimaryKeyValue_ReadsPkid_AsString()
    {
        Assert.Equal("42", RowAuditWriter.PrimaryKeyValue(new Sample { Pkid = 42 }));
    }

    [Fact]
    public void BuildInsertEntry_SetsPrimaryKeyValues_FromPkid()
    {
        var writer = CreateWriter(AnonymousAccessor());

        var entry = writer.BuildInsertEntry("Course", new Sample { Pkid = 123, Title = "X" });

        Assert.Equal("123", entry.PrimaryKeyValues);
    }

    // --- ActionDesc: changed property names (Update) ------------------------

    [Fact]
    public void ChangedPropertyNames_ListsExactlyTheChangedProperties()
    {
        var before = new Sample { Pkid = 1, Title = "Old", Code = "C1", DisplayOrder = 5 };
        var after = new Sample { Pkid = 1, Title = "New", Code = "C1", DisplayOrder = 9 };

        // Title and DisplayOrder changed; Pkid and Code did not.
        Assert.Equal("Title, DisplayOrder", RowAuditWriter.ChangedPropertyNames(before, after));
    }

    [Fact]
    public void ChangedPropertyNames_ReturnsEmpty_WhenNothingChanged()
    {
        var before = new Sample { Pkid = 1, Title = "Same", Code = "C1", DisplayOrder = 5 };
        var after = new Sample { Pkid = 1, Title = "Same", Code = "C1", DisplayOrder = 5 };

        Assert.Equal(string.Empty, RowAuditWriter.ChangedPropertyNames(before, after));
    }

    [Fact]
    public void BuildUpdateEntry_ActionDescIsChangedNames()
    {
        var writer = CreateWriter(AnonymousAccessor());
        var before = new Sample { Pkid = 1, Title = "Old", Code = "C1" };
        var after = new Sample { Pkid = 1, Title = "New", Code = "C1" };

        var entry = writer.BuildUpdateEntry("Course", before, after);

        Assert.NotNull(entry);
        Assert.Equal("Update", entry!.ActionType);
        Assert.Equal("Title", entry.ActionDesc);
        Assert.Equal("1", entry.PrimaryKeyValues);
    }

    [Fact]
    public void BuildUpdateEntry_ReturnsNull_WhenNothingChanged()
    {
        var writer = CreateWriter(AnonymousAccessor());
        var same = new Sample { Pkid = 1, Title = "Same", Code = "C1", DisplayOrder = 5 };

        var entry = writer.BuildUpdateEntry("Course", same, new Sample { Pkid = 1, Title = "Same", Code = "C1", DisplayOrder = 5 });

        Assert.Null(entry); // no row is written
    }

    // --- UserName from JWT, "system" fallback -------------------------------

    [Fact]
    public void ResolveUserName_FallsBackToSystem_WhenUnauthenticated()
    {
        var writer = CreateWriter(AnonymousAccessor());

        var entry = writer.BuildInsertEntry("Course", new Sample { Pkid = 1, Title = "X" });

        Assert.Equal("system", entry.UserName);
    }

    [Fact]
    public void ResolveUserName_ReadsUserNameClaim_WhenAuthenticated()
    {
        var writer = CreateWriter(AccessorWithUser("alice"));

        var entry = writer.BuildInsertEntry("Course", new Sample { Pkid = 1, Title = "X" });

        Assert.Equal("alice", entry.UserName);
    }

    // --- ActionDesc truncation ----------------------------------------------

    [Fact]
    public void BuildInsertEntry_TruncatesActionDesc_At1000Chars()
    {
        var writer = CreateWriter(AnonymousAccessor());
        var longTitle = new string('a', 1500);

        var entry = writer.BuildInsertEntry("Course", new Sample { Pkid = 1, Title = longTitle });

        Assert.Equal(RowAuditWriter.ActionDescMaxLength, entry.ActionDesc!.Length);
        Assert.Equal(new string('a', 1000), entry.ActionDesc);
    }
}
