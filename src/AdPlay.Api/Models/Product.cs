namespace AdPlay.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
}
