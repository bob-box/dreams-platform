namespace Borderland.Dreams.Extensions;

public static class MappingExtensions
{
    public static List<TDest> MapList<TSource, TDest>(
        this IEnumerable<TSource>? source,
        Func<TSource, TDest> map)
    {
        return source?.Select(map).ToList() ?? [];
    }
}