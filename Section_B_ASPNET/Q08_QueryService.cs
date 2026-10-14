using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

// Q8: Specification objects are constructed by trusted server code.
public sealed record QuerySpec<T, TResult>(
    Expression<Func<T, TResult>> Projection,
    Func<IQueryable<T>, IOrderedQueryable<T>> Order,
    Expression<Func<T, bool>>? Filter = null,
    int Page = 1, int PageSize = 20) where T : class;

public sealed record PageResult<T>(IReadOnlyList<T> Items, long TotalCount);

public sealed class QueryService<T>(DbContext db) where T : class
{
    public async Task<PageResult<TResult>> PageAsync<TResult>(
        QuerySpec<T, TResult> spec, CancellationToken ct)
    {
        var skip = GetOffset(spec.Page, spec.PageSize);
        IQueryable<T> query = db.Set<T>().AsNoTracking();
        if (spec.Filter is not null) query = query.Where(spec.Filter);

        var count = await query.LongCountAsync(ct);
        // The order must end in a unique key. Projection avoids over-fetching.
        var items = await spec.Order(query).Skip(skip).Take(spec.PageSize)
            .Select(spec.Projection).ToListAsync(ct);
        return new(items, count);
    }

    public async Task<IReadOnlyList<T>> EntitiesAsync(
        Expression<Func<T, bool>> filter,
        Func<IQueryable<T>, IOrderedQueryable<T>> order,
        int page, int pageSize, CancellationToken ct,
        params Expression<Func<T, object>>[] includes)
    {
        var skip = GetOffset(page, pageSize);
        IQueryable<T> query = db.Set<T>().AsNoTracking().Where(filter);
        foreach (var include in includes) query = query.Include(include);
        return await order(query.AsSplitQuery()).Skip(skip).Take(pageSize).ToListAsync(ct);
    }

    private static int GetOffset(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new ArgumentException("Invalid page or page size.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > 100_000) throw new ArgumentException("Use cursor pagination for deep pages.");
        return (int)offset;
    }
}
