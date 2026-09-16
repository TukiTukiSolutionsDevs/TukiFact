using System.Data;
using Dapper;

namespace TukiFact.Common.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Npgsql materializes <c>date</c> as <see cref="DateTime"/> through <c>GetValue</c>, so Dapper cannot bind it to
/// positional records with <see cref="DateOnly"/> parameters without a handler.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to {nameof(DateOnly)}."),
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.Value = value;
    }
}
