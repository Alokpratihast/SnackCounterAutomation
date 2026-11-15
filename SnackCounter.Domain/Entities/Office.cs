
namespace SnackCounter.Domain.Entities;

public class Office
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;

    public ICollection<Employee> Employees { get; private set; }
        = new List<Employee>();

    private Office() { } // EF Core

    public Office(string name, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Office name is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }
}
