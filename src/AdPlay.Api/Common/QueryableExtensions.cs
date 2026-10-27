using System.Linq.Expressions;
using System.Reflection;

namespace AdPlay.Api.Common;

// Q9: query.OrderBy("Name"), query.OrderBy("Salary desc")
// Bad input throws ArgumentException, the API layer turns that into a 400.
public static class QueryableExtensions
{
    public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, string sort)
    {
        var (prop, desc) = Parse<T>(sort);
        return Apply(query, prop, desc ? "OrderByDescending" : "OrderBy");
    }

    public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> query, string sort)
    {
        var (prop, desc) = Parse<T>(sort);
        return Apply(query, prop, desc ? "ThenByDescending" : "ThenBy");
    }

    private static (PropertyInfo prop, bool desc) Parse<T>(string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            throw new ArgumentException("Sort value is empty");

        var parts = sort.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 2)
            throw new ArgumentException($"Invalid sort '{sort}'");

        var desc = false;
        if (parts.Length == 2)
        {
            if (parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)) desc = true;
            else if (!parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Sort direction must be asc or desc");
        }

        // only real public properties of T can be used, nothing the client types is compiled
        var prop = typeof(T).GetProperty(parts[0],
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (prop is null || !IsSimple(prop.PropertyType))
            throw new ArgumentException($"Cannot sort by '{parts[0]}'");

        return (prop, desc);
    }

    // no navigation properties or collections
    private static bool IsSimple(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal)
            || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid);
    }

    // builds: query.OrderBy(x => x.Prop)
    private static IOrderedQueryable<T> Apply<T>(IQueryable<T> query, PropertyInfo prop, string method)
    {
        var x = Expression.Parameter(typeof(T), "x");
        var keySelector = Expression.Lambda(Expression.Property(x, prop), x);

        var call = Expression.Call(
            typeof(Queryable), method,
            new[] { typeof(T), prop.PropertyType },
            query.Expression, Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(call);
    }
}
