using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfServer.Data.Entities;

namespace PdfServer.Data;

public class AdventureWorksContext : DbContext
{
    public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options) : base(options) { }

    public DbSet<SalesOrderHeader> SalesOrderHeaders => Set<SalesOrderHeader>();
    public DbSet<SalesOrderDetail> SalesOrderDetails => Set<SalesOrderDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrderHeader>(e =>
        {
            e.ToTable("SalesOrderHeader", "Sales");
            e.HasKey(h => h.SalesOrderID);
        });

        modelBuilder.Entity<SalesOrderDetail>(SalesOrderDetailConfig.Configure);
    }
}

internal static class SalesOrderDetailConfig
{
    public static void Configure(EntityTypeBuilder<SalesOrderDetail> e)
    {
        e.ToTable("SalesOrderDetail", "Sales");
        e.HasKey(d => new { d.SalesOrderID, d.SalesOrderDetailID });

        e.HasOne(d => d.Header!)
         .WithMany(h => h.Details)
         .HasForeignKey(d => d.SalesOrderID);
    }
}
