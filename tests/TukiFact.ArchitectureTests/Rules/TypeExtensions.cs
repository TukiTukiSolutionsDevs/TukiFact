namespace TukiFact.ArchitectureTests.Rules;

public static class TypeExtensions
{
    public static bool IsConcreteClass(this Type type) =>
        type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false };

    public static bool IsInternal(this Type type) => !type.IsPublic && !type.IsNestedPublic;

    public static string NameWithoutArity(this Type type) => type.Name.Split('`')[0];
}
