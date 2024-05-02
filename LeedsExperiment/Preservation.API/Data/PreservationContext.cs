using Microsoft.EntityFrameworkCore;
using Preservation.API.Data.Entities;

namespace Preservation.API.Data;

public class PreservationContext(DbContextOptions<PreservationContext> options) : DbContext(options)
{
    public DbSet<DepositEntity> Deposits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepositEntity>(builder =>
        {
            builder
                .Property(su => su.Created)
                .HasDefaultValueSql("now()");
        });
    }
}