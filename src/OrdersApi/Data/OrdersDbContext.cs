using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;

namespace OrdersApi.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.CustomerEmail)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(e => e.OrderDate)
                .IsRequired();
                
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
                
            entity.Property(e => e.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
                
            entity.Property(e => e.CancellationReason)
                .HasMaxLength(500);
                
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure OrderItem entity
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.ProductSku)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.Quantity)
                .IsRequired();
                
            entity.Property(e => e.UnitPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
                
            entity.HasOne(e => e.Order)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.OrderId);
        });
    }
}