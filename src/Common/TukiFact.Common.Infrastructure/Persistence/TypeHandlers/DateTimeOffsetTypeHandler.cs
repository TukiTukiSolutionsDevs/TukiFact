using System.Data;
using Dapper;

namespace TukiFact.Common.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Npgsql materializes <c>timestamptz</c> as a UTC <see cref="DateTime"/> through <c>GetValue</c>, so Dapper cannot
/// bind it to positional records with <see cref="DateTimeOffset"/> parameters without a handler.
/// A non-UTC value means a <c>timestamp without time zone</c> column, which the conventions do not use.
/// </summary>
internal sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime { Kind: DateTimeKind.Utc } dateTime => new DateTimeOffset(dateTime),
        _ => throw new DataException($"Cannot convert {value} to {nameof(DateTimeOffset)}; map DateTimeOffset to timestamptz."),
    };

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.Value = value;
    }
}
