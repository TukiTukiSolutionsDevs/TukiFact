using Dapper;

namespace TukiFact.Common.Infrastructure.Persistence.TypeHandlers;

/// <summary>Registers the Dapper handlers the query side needs. Dapper keeps them globally; re-registering replaces them.</summary>
internal static class DapperTypeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }
}
