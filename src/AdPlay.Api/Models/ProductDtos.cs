using System.ComponentModel.DataAnnotations;

namespace AdPlay.Api.Models;

public class ProductSearchRequest
{
    [StringLength(100)]
    public string? Keyword { get; set; }

    [Range(0, 100000000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 100000000)]
    public decimal? MaxPrice { get; set; }

    // ?categoryIds=1&categoryIds=2
    [MaxLength(50)]
    public List<int> CategoryIds { get; set; } = new();

    [MaxLength(50)]
    public List<int> BrandIds { get; set; } = new();

    // e.g. "price desc", "name"
    [StringLength(30)]
    public string? Sort { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public record ProductDto(int Id, string Name, decimal Price, string Category, string Brand, DateTime CreatedAt);
