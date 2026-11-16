
using Microsoft.EntityFrameworkCore;
using SnackCounter.Domain.Entities;

namespace SnackCounter.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Office> Offices => Set<Office>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<BudgetReservation> BudgetReservations =>
        Set<BudgetReservation>();

    public DbSet<RefillRequest> RefillRequests =>
        Set<RefillRequest>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<Reimbursement> Reimbursements =>
        Set<Reimbursement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Office>()
            .HasMany(o => o.Employees)
            .WithOne(e => e.Office)
            .HasForeignKey(e => e.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Office)
            .WithMany()
            .HasForeignKey(i => i.OfficeId);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId);

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.OfficeId, i.ProductId })
            .IsUnique();

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Office)
            .WithMany()
            .HasForeignKey(b => b.OfficeId);

        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.OfficeId, b.Year, b.Month })
            .IsUnique();

        modelBuilder.Entity<BudgetReservation>()
            .HasIndex(r => r.RefillRequestId)
            .IsUnique();

        modelBuilder.Entity<RefillRequest>()
            .HasOne<Office>()
            .WithMany()
            .HasForeignKey(r => r.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefillRequest>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefillRequest>()
            .HasOne<Employee>()
            .WithMany()
            .HasForeignKey(r => r.AssignedVolunteerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefillRequest>()
            .HasIndex(r => new { r.OfficeId, r.ProductId })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2, 3)");

        modelBuilder.Entity<Purchase>()
            .HasIndex(p => p.RefillRequestId)
            .IsUnique();

        modelBuilder.Entity<Reimbursement>()
            .HasIndex(r => r.PurchaseId)
            .IsUnique();

        modelBuilder.Entity<Budget>()
            .Property(b => b.MonthlyLimit)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Budget>()
            .Property(b => b.ActualSpent)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Budget>()
            .Property(b => b.ReservedAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Purchase>()
            .Property(p => p.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Purchase>()
            .Property(p => p.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BudgetReservation>()
            .Property(r => r.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Office>()
            .Property(o => o.RotationVersion)
            .IsConcurrencyToken();

        modelBuilder.Entity<RefillRequest>()
            .Property(r => r.ConcurrencyVersion)
            .IsConcurrencyToken();
    }
}
