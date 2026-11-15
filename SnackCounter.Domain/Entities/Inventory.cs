namespace SnackCounter.Domain.Entities;

public class Inventory
{
    public Guid Id { get; private set; }

    public Guid OfficeId { get; private set; }

    public Office Office { get; private set; } = null!;

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public int CurrentQuantity { get; private set; }

    public int MinimumQuantity { get; private set; }

    public DateTime LastUpdatedAtUtc { get; private set; }

    private Inventory() { } // EF Core

    public Inventory(
        Guid officeId,
        Guid productId,
        int currentQuantity,
        int minimumQuantity)
    {
        if (officeId == Guid.Empty)
            throw new ArgumentException("Office is required.");

        if (productId == Guid.Empty)
            throw new ArgumentException("Product is required.");

        if (currentQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        if (minimumQuantity < 0)
            throw new ArgumentException("Minimum quantity cannot be negative.");

        Id = Guid.NewGuid();
        OfficeId = officeId;
        ProductId = productId;
        CurrentQuantity = currentQuantity;
        MinimumQuantity = minimumQuantity;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsLowStock()
    {
        return CurrentQuantity <= MinimumQuantity;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity cannot be negative.");

        CurrentQuantity = quantity;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Added quantity must be greater than zero.");

        CurrentQuantity += quantity;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }
}