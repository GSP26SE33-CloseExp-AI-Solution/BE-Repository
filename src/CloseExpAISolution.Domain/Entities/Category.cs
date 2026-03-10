namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Category entity per ER diagram. Products belong to a category.
/// </summary>
public class Category
{
    public Guid CategoryId { get; set; }
    public Guid? ParentCatId { get; set; }
    public bool IsFreshFood { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CatIconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
