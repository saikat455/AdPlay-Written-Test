using System.Linq.Expressions;
using AdPlay.Api.Common;
using AdPlay.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AdPlay.Api.Services;

// Q8: the caller (our own code) builds the spec with typed expressions.
// Clients only send plain values, never expressions.
public class QuerySpec<T> where T : class
{
    public QuerySpec(IEnumerable<string> sortableFields)
    {
        SortableFields = new HashSet<string>(sortableFields, StringComparer.OrdinalIgnoreCase);
    }

    public List<Expression<Func<T, bool>>> Filters { get; } = new();

    // e.g. spec.Includes.Add(q => q.Include(p => p.Category))
    public List<Func<IQueryable<T>, IQueryable<T>>> Includes { get; } = new();

    public HashSet<string> SortableFields { get; }   // allowlist for client sort
    public string? SortBy { get; set; }              // from the client, e.g. "price desc"
    public string DefaultSort { get; set; } = "Id";
    public string TieBreaker { get; set; } = "Id";   // keeps paging stable
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class QueryService
{
    private const int MaxPageSize = 100;
    private const int MaxPage = 100000;   // also protects Skip() from overflow

    private readonly AppDbContext _db;

    public QueryService(AppDbContext db) => _db = db;

    // Projection version: selects only the columns the DTO needs (no overfetching)
    public async Task<PagedResult<TDto>> GetPagedAsync<TEntity, TDto>(
        QuerySpec<TEntity> spec, Expression<Func<TEntity, TDto>> select, CancellationToken ct)
        where TEntity : class
    {
        var filtered = Filter(spec);

        // count has no sorting, includes or joins
        var total = await filtered.CountAsync(ct);
        var (page, size) = Normalize(spec);
        if (total == 0) return new PagedResult<TDto>(new List<TDto>(), page, size, 0);

        var items = await Sort(filtered, spec)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(select)
            .ToListAsync(ct);

        return new PagedResult<TDto>(items, page, size, total);
    }

    // Entity version: use when the full entity (with includes) is really needed
    public async Task<PagedResult<TEntity>> GetPagedEntitiesAsync<TEntity>(
        QuerySpec<TEntity> spec, CancellationToken ct)
        where TEntity : class
    {
        var filtered = Filter(spec);

        var total = await filtered.CountAsync(ct);
        var (page, size) = Normalize(spec);
        if (total == 0) return new PagedResult<TEntity>(new List<TEntity>(), page, size, 0);

        IQueryable<TEntity> query = filtered;
        foreach (var include in spec.Includes)
            query = include(query);

        var items = await Sort(query, spec)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<TEntity>(items, page, size, total);
    }

    private IQueryable<T> Filter<T>(QuerySpec<T> spec) where T : class
    {
        IQueryable<T> query = _db.Set<T>().AsNoTracking();
        foreach (var filter in spec.Filters)
            query = query.Where(filter);
        return query;
    }

    private static IOrderedQueryable<T> Sort<T>(IQueryable<T> query, QuerySpec<T> spec) where T : class
    {
        var fromClient = !string.IsNullOrWhiteSpace(spec.SortBy);
        var sort = fromClient ? spec.SortBy! : spec.DefaultSort;
        var field = sort.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        if (fromClient && !spec.SortableFields.Contains(field))
            throw new ArgumentException($"Sorting by '{field}' is not allowed");

        var ordered = query.OrderBy(sort);
        if (!field.Equals(spec.TieBreaker, StringComparison.OrdinalIgnoreCase))
            ordered = ordered.ThenBy(spec.TieBreaker);

        return ordered;
    }

    private static (int page, int size) Normalize<T>(QuerySpec<T> spec) where T : class =>
        (Math.Clamp(spec.Page, 1, MaxPage), Math.Clamp(spec.PageSize, 1, MaxPageSize));
}
