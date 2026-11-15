
namespace SnackCounter.Domain.Entities;

public class Employee
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public Guid OfficeId { get; private set; }

    public Office Office { get; private set; } = null!;

    public bool IsVolunteer { get; private set; }

    public bool IsAvailable { get; private set; }

    private Employee() { } // EF Core

    public Employee(
        string name,
        string email,
        Guid officeId,
        bool isVolunteer = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Employee name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Employee email is required.");

        if (officeId == Guid.Empty)
            throw new ArgumentException("Office is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        OfficeId = officeId;
        IsVolunteer = isVolunteer;
        IsAvailable = isVolunteer;
    }

    public void BecomeVolunteer()
    {
        IsVolunteer = true;
        IsAvailable = true;
    }

    public void UpdateAvailability(bool isAvailable)
    {
        if (!IsVolunteer)
            throw new InvalidOperationException(
                "Only volunteers can update volunteer availability.");

        IsAvailable = isAvailable;
    }
}
