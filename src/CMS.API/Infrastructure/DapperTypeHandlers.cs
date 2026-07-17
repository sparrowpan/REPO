using System.Data;
using Dapper;

namespace CMS.API.Infrastructure;

/// <summary>Maps SQL Server <c>date</c> columns to <see cref="DateOnly"/>.</summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    /// <remarks>
    /// SQL Server hands back a <see cref="DateTime"/>. SQLite has no date type and returns the stored
    /// text, so the string branch is what lets a real repository run against the in-memory SQLite used
    /// by the repository-level tests. Writing is unaffected — <see cref="SetValue"/> still sends
    /// <see cref="DbType.Date"/>.
    /// </remarks>
    public override DateOnly Parse(object value) => value switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        DateOnly d => d,
        string s => DateOnly.Parse(s, System.Globalization.CultureInfo.InvariantCulture),
        _ => DateOnly.FromDateTime(Convert.ToDateTime(value, System.Globalization.CultureInfo.InvariantCulture)),
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}

/// <summary>Maps SQL Server <c>time</c> columns to <see cref="TimeOnly"/>.</summary>
public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override TimeOnly Parse(object value) => TimeOnly.FromTimeSpan((TimeSpan)value);

    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }
}
