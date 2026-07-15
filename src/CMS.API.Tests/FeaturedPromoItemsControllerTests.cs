using System.Net;
using System.Net.Http.Json;
using CMS.API.Controllers;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the FeaturedPromoItem board endpoints (weekly filter, center tab,
/// PromoCode lookup, CRUD, slot move) against an in-memory fake repository.
/// </summary>
public class FeaturedPromoItemsControllerTests
{
    private static readonly DateOnly WeekStart = new(2026, 3, 16); // Monday
    private static readonly DateOnly WeekEnd = new(2026, 3, 22);   // Sunday

    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient());
    }

    private static FeaturedPromoItemRequest NewRequest(DateOnly on, short center, byte slot, int promotionPkid = 101) => new()
    {
        ScheduleOn = on,
        TrainingCenterPkid = center,
        Slot = slot,
        PromotionPkid = promotionPkid,
        Topic = "測試標題",
        Description = "測試描述",
    };

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededItems()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var items = await client.GetFromJsonAsync<List<FeaturedPromoItem>>("/api/featured-promo-items");

        Assert.NotNull(items);
        Assert.Equal(6, items!.Count);
    }

    [Fact]
    public async Task Query_OneWeekScheduleOnFilter_ExcludesOtherWeeks()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var query = new FeaturedPromoItemQuery { ScheduleOnFrom = WeekStart, ScheduleOnTo = WeekEnd };
        var response = await client.PostAsJsonAsync("/api/featured-promo-items/query", query);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();

        // 5 items fall in 3/16–3/22; the 3/23 item (next week) is excluded.
        Assert.Equal(5, items!.Count);
        Assert.All(items!, i => Assert.InRange(i.ScheduleOn, WeekStart, WeekEnd));
    }

    [Fact]
    public async Task Query_TrainingCenterFilter_ReturnsOnlyThatCenter()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var query = new FeaturedPromoItemQuery
        {
            TrainingCenterPkid = 1,
            ScheduleOnFrom = WeekStart,
            ScheduleOnTo = WeekEnd,
        };
        var response = await client.PostAsJsonAsync("/api/featured-promo-items/query", query);
        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();

        // Center 1 has 4 items in the week; center 2's single item is excluded.
        Assert.Equal(4, items!.Count);
        Assert.All(items!, i => Assert.Equal((short)1, i.TrainingCenterPkid));
    }

    [Fact]
    public async Task Query_JoinsPromoCodeFromPromotion()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var query = new FeaturedPromoItemQuery { TrainingCenterPkid = 1, ScheduleOnFrom = WeekStart, ScheduleOnTo = WeekEnd };
        var response = await client.PostAsJsonAsync("/api/featured-promo-items/query", query);
        var items = await response.Content.ReadFromJsonAsync<List<FeaturedPromoItem>>();

        var slot1 = items!.Single(i => i.ScheduleOn == WeekStart && i.Slot == 1);
        Assert.Equal("20251204_SkillTrainAI", slot1.PromoCode);
    }

    // --- PromoCode lookup ---------------------------------------------------

    [Fact]
    public async Task Lookup_Promotions_ReturnsPromoCodesForResolution()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var promotions = await client.GetFromJsonAsync<List<PromotionLookup>>("/api/lookups/promotions");

        Assert.NotNull(promotions);
        var match = promotions!.Single(p => p.PromoCode == "20251204_SkillTrainAI");
        Assert.Equal(101, match.Pkid);
        Assert.Equal("成為能AI協作的程式設計師", match.Topic);
    }

    [Fact]
    public async Task Lookup_TrainingCenters_DrivesTheBoardTabs()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var centers = await client.GetFromJsonAsync<List<TrainingCenterLookup>>("/api/lookups/training-centers");

        Assert.NotNull(centers);
        Assert.Equal(5, centers!.Count);
        Assert.Equal("台北", centers![0].Name);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingItem_ReturnsItem()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var item = await client.GetFromJsonAsync<FeaturedPromoItem>("/api/featured-promo-items/1");

        Assert.NotNull(item);
        Assert.Equal(1, item!.Pkid);
        Assert.Equal((byte)1, item.Slot);
    }

    [Fact]
    public async Task GetById_UnknownItem_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/featured-promo-items/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewItem_Returns201AndEchoesPromoCode()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // A free cell: Wednesday 3/18, center 1, slot 1.
        var request = NewRequest(new DateOnly(2026, 3, 18), 1, 1, promotionPkid: 102);

        var response = await client.PostAsJsonAsync("/api/featured-promo-items", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<FeaturedPromoItem>();
        Assert.True(created!.Pkid > 0);
        Assert.Equal("251211_GoogleAI", created.PromoCode);

        var fetched = await client.GetFromJsonAsync<FeaturedPromoItem>($"/api/featured-promo-items/{created.Pkid}");
        Assert.Equal("測試標題", fetched!.Topic);
    }

    [Fact]
    public async Task Create_SlotOutOfRange_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Slot is [Range(1, 3)] — 4 is invalid.
        var request = NewRequest(new DateOnly(2026, 3, 18), 1, slot: 4);

        var response = await client.PostAsJsonAsync("/api/featured-promo-items", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingItem_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = NewRequest(WeekStart, 1, 1, promotionPkid: 103);
        request.Pkid = 1;
        request.Topic = "已更新標題";

        var response = await client.PutAsJsonAsync("/api/featured-promo-items", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<FeaturedPromoItem>("/api/featured-promo-items/1");
        Assert.Equal("已更新標題", fetched!.Topic);
        Assert.Equal("20251215_n8n", fetched.PromoCode);
    }

    [Fact]
    public async Task Update_UnknownItem_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = NewRequest(WeekStart, 1, 1);
        request.Pkid = 888;

        var response = await client.PutAsJsonAsync("/api/featured-promo-items", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Move (slot + / −) --------------------------------------------------

    [Fact]
    public async Task Move_SwapsWithOccupantOfTargetSlot()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Item 1 is (3/16, center 1, slot 1); item 2 is the same cell at slot 2. Move 1 -> slot 2.
        var response = await client.PostAsJsonAsync("/api/featured-promo-items/move",
            new MoveSlotRequest { Pkid = 1, TargetSlot = 2 });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var one = await client.GetFromJsonAsync<FeaturedPromoItem>("/api/featured-promo-items/1");
        var two = await client.GetFromJsonAsync<FeaturedPromoItem>("/api/featured-promo-items/2");
        Assert.Equal((byte)2, one!.Slot);
        Assert.Equal((byte)1, two!.Slot); // occupant pushed back to the vacated slot
    }

    [Fact]
    public async Task Move_UnknownItem_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/featured-promo-items/move",
            new MoveSlotRequest { Pkid = 777, TargetSlot = 2 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingItem_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/featured-promo-items/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/featured-promo-items/1");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownItem_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/featured-promo-items/654");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
