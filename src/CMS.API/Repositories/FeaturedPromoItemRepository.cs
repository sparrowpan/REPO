using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="FeaturedPromoItem"/> (no EF).</summary>
public sealed class FeaturedPromoItemRepository(IDbConnectionFactory connectionFactory) : IFeaturedPromoItemRepository
{
    // PromoCode is joined from Promotion2 for display; all other columns live on FeaturedPromoItem.
    private const string SelectColumns = """
        SELECT f.pkid AS Pkid, f.ScheduleOn, f.TrainingCenter_pkid AS TrainingCenterPkid,
               f.Slot, f.Promotion_pkid AS PromotionPkid, p.PromoCode AS PromoCode,
               f.Topic, f.Description
        FROM FeaturedPromoItem f
        JOIN Promotion2 p ON p.pkid = f.Promotion_pkid
        """;

    public async Task<IReadOnlyList<FeaturedPromoItem>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY f.ScheduleOn, f.TrainingCenter_pkid, f.Slot";
        var rows = await conn.QueryAsync<FeaturedPromoItem>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@TrainingCenterPkid IS NULL OR f.TrainingCenter_pkid = @TrainingCenterPkid)
              AND (@ScheduleOnFrom IS NULL OR f.ScheduleOn >= @ScheduleOnFrom)
              AND (@ScheduleOnTo IS NULL OR f.ScheduleOn <= @ScheduleOnTo)
              AND (@Slot IS NULL OR f.Slot = @Slot)
            ORDER BY f.ScheduleOn, f.Slot
            """;
        var rows = await conn.QueryAsync<FeaturedPromoItem>(new CommandDefinition(sql, new
        {
            query.TrainingCenterPkid,
            query.ScheduleOnFrom,
            query.ScheduleOnTo,
            query.Slot,
        }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<FeaturedPromoItem?> GetByPkidAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE f.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<FeaturedPromoItem>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var pkid = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO FeaturedPromoItem (ScheduleOn, TrainingCenter_pkid, Slot, Promotion_pkid, Topic, Description)
            VALUES (@ScheduleOn, @TrainingCenterPkid, @Slot, @PromotionPkid, @Topic, @Description);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request, cancellationToken: ct));

        return (await GetByPkidAsync(pkid, ct))!;
    }

    public async Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE FeaturedPromoItem SET
                ScheduleOn = @ScheduleOn, TrainingCenter_pkid = @TrainingCenterPkid, Slot = @Slot,
                Promotion_pkid = @PromotionPkid, Topic = @Topic, Description = @Description
            WHERE pkid = @Pkid;
            """, request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM FeaturedPromoItem WHERE pkid = @Pkid", new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> MoveToSlotAsync(int pkid, byte targetSlot, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var item = await conn.QuerySingleOrDefaultAsync<FeaturedPromoItem>(new CommandDefinition(
            "SELECT pkid AS Pkid, ScheduleOn, TrainingCenter_pkid AS TrainingCenterPkid, Slot FROM FeaturedPromoItem WHERE pkid = @Pkid",
            new { Pkid = pkid }, tx, cancellationToken: ct));
        if (item is null || item.Slot == targetSlot)
        {
            tx.Rollback();
            return false;
        }

        // Occupant currently sitting in the target slot on the same day / center (if any).
        var occupantPkid = await conn.ExecuteScalarAsync<int?>(new CommandDefinition("""
            SELECT pkid FROM FeaturedPromoItem
            WHERE ScheduleOn = @ScheduleOn AND TrainingCenter_pkid = @TrainingCenterPkid AND Slot = @Slot
            """, new { item.ScheduleOn, item.TrainingCenterPkid, Slot = targetSlot }, tx, cancellationToken: ct));

        if (occupantPkid is int occupant)
        {
            // Swap via a temporary slot (0) so the unique (date, center, slot) index never collides.
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE FeaturedPromoItem SET Slot = 0 WHERE pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE FeaturedPromoItem SET Slot = @Slot WHERE pkid = @Pkid", new { Slot = item.Slot, Pkid = occupant }, tx, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE FeaturedPromoItem SET Slot = @Slot WHERE pkid = @Pkid", new { Slot = targetSlot, Pkid = pkid }, tx, cancellationToken: ct));
        }
        else
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE FeaturedPromoItem SET Slot = @Slot WHERE pkid = @Pkid", new { Slot = targetSlot, Pkid = pkid }, tx, cancellationToken: ct));
        }

        tx.Commit();
        return true;
    }
}
