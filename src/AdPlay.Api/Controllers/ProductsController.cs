using AdPlay.Api.Models;
using AdPlay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdPlay.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private static readonly string[] SortFields = { "Name", "Price", "CreatedAt" };

    private readonly QueryService _query;

    public ProductsController(QueryService query) => _query = query;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequest r, CancellationToken ct)
    {
        if (r.MinPrice > r.MaxPrice)
            return BadRequest(new { error = "invalid_price_range", message = "minPrice cannot be greater than maxPrice" });

        var spec = new QuerySpec<Product>(SortFields)
        {
            SortBy = r.Sort ?? "CreatedAt desc",
            Page = r.Page,
            PageSize = r.PageSize
        };

        if (!string.IsNullOrWhiteSpace(r.Keyword))
        {
            // Contains -> LIKE '%x%' with a parameter (safe from injection).
            // On big data a FULLTEXT index would be better.
            var keyword = r.Keyword.Trim();
            spec.Filters.Add(p => p.Name.Contains(keyword));
        }

        if (r.MinPrice is decimal min) spec.Filters.Add(p => p.Price >= min);
        if (r.MaxPrice is decimal max) spec.Filters.Add(p => p.Price <= max);

        if (r.CategoryIds.Count > 0)
        {
            var ids = r.CategoryIds;
            spec.Filters.Add(p => ids.Contains(p.CategoryId));
        }

        if (r.BrandIds.Count > 0)
        {
            var ids = r.BrandIds;
            spec.Filters.Add(p => ids.Contains(p.BrandId));
        }

        try
        {
            var result = await _query.GetPagedAsync(spec,
                p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name, p.Brand.Name, p.CreatedAt),
                ct);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_sort", message = ex.Message });
        }
    }
}
