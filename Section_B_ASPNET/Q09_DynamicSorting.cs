using System.Linq.Expressions;
using System.Reflection;

// Q9: Safe property-based sorting. Supply an endpoint allowlist for API input.
public static class DynamicSorting
{
    public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string sort,
        IReadOnlySet<string>? allowedProperties = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(sort)) throw new ArgumentException("Sort cannot be empty.", nameof(sort));
        var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2) throw new ArgumentException("Use Property or Property asc/desc.");
        var direction = parts.Length == 1 ? "asc" : parts[1].ToLowerInvariant();
        if (direction is not ("asc" or "desc")) throw new ArgumentException("Invalid sort direction.");
        var property = typeof(T).GetProperty(parts[0], BindingFlags.Public | BindingFlags.Instance);
        if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
            throw new ArgumentException("Unknown readable property.");
        if (allowedProperties is not null && !allowedProperties.Contains(property.Name))
            throw new ArgumentException("Property is not permitted for sorting.");
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (!(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
              || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid)))
            throw new ArgumentException("Only scalar properties can be sorted.");
        var parameter = Expression.Parameter(typeof(T), "x");
        var selector = Expression.Lambda(Expression.Property(parameter, property), parameter);
        var call = Expression.Call(typeof(Queryable), direction == "desc" ? "OrderByDescending" : "OrderBy",
            [typeof(T), property.PropertyType], source.Expression, Expression.Quote(selector));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }
}
