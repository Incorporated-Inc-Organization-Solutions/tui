using Microsoft.EntityFrameworkCore;
using tui.Models;

namespace tui.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<TestRecord> TestRecords => Set<TestRecord>();

    public DbSet<Recipient> Recipients => Set<Recipient>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Recipient>(entity =>
        {
            entity.Property(recipient => recipient.Name).HasMaxLength(200);
            entity.HasData(
                new Recipient { Id = 1, Name = "Voorbeeldontvanger Noord" },
                new Recipient { Id = 2, Name = "Voorbeeldontvanger Zuid" });
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(invoice => invoice.InternalReference).IsUnique();
            entity.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
            entity.Property(invoice => invoice.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(invoice => invoice.Recipient)
                .WithMany(recipient => recipient.Invoices)
                .HasForeignKey(invoice => invoice.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
