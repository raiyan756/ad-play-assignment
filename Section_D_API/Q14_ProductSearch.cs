using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Q14: Action inside an existing controller with injected DbContext _db.
// Product has Id, Name, Price, CategoryId and BrandId.
// Category and brand arrays combine with AND; values within each array use OR.

[HttpGet("/api/products")]
public async Task<IActionResult> SearchProducts(
    string? keyword = null, decimal? minPrice = null, decimal? maxPrice = null,
    [FromQuery] long[]? categories = null, [FromQuery] long[]? brands = null,
    string sortBy = "id", string sortDirection = "asc",
    int page = 1, int pageSize = 20, CancellationToken ct = default)
{
    if (page < 1 || pageSize is < 1 or > 100)
        return BadRequest("Invalid page or page size.");
    long offset = ((long)page - 1) * pageSize;
    if (offset > 100_000) return BadRequest("Use cursor pagination for deep pages.");
    if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
        return BadRequest("Invalid price range.");
    if (keyword?.Length > 100 || categories?.Length > 50 || brands?.Length > 50)
        return BadRequest("Too many filters or keyword too long.");
    if (categories?.Any(id => id <= 0) == true || brands?.Any(id => id <= 0) == true)
        return BadRequest("Filter IDs must be positive.");
    var field = sortBy?.ToLowerInvariant();
    var direction = sortDirection?.ToLowerInvariant();
    if (field is not ("id" or "name" or "price") || direction is not ("asc" or "desc"))
        return BadRequest("Invalid sorting.");

    var query = _db.Products.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        var term = keyword.Trim();
        query = query.Where(p => p.Name.Contains(term));
    }
    if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
    if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);
    if (categories is { Length: > 0 }) query = query.Where(p => categories.Contains(p.CategoryId));
    if (brands is { Length: > 0 }) query = query.Where(p => brands.Contains(p.BrandId));

    var totalCount = await query.LongCountAsync(ct);
    var ordered = (field, direction) switch
    {
        ("name", "asc") => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
        ("name", "desc") => query.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
        ("price", "asc") => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
        ("price", "desc") => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
        ("id", "desc") => query.OrderByDescending(p => p.Id),
        _ => query.OrderBy(p => p.Id)
    };
    var items = await ordered.Skip((int)offset).Take(pageSize)
        .Select(p => new { p.Id, p.Name, p.Price, p.CategoryId, p.BrandId })
        .ToListAsync(ct);

    return Ok(new { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
}
