namespace SnackCounter.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    private Product() { } // EF Core

    public Product(string name, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Product category is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Category = category.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}