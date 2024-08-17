using Microsoft.EntityFrameworkCore;
using Storage.API.Data.Entities;

namespace Storage.API.Data;

public class PreservationContext(DbContextOptions<PreservationContext> options) : DbContext(options)
{
    public DbSet<DepositEntity> Deposits { get; set; }
    
    public DbSet<ImportJobEntity> ImportJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepositEntity>(builder =>
        {
            builder
                .Property(su => su.Created)
                .HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<ImportJobEntity>(builder =>
        {
            builder
                .Property(ij => ij.DateSubmitted)
                .HasDefaultValueSql("now()");

            builder.Property(ij => ij.Errors).HasColumnType("jsonb");
            builder.Property(ij => ij.ContainersAdded).HasColumnType("jsonb");
            builder.Property(ij => ij.ContainersDeleted).HasColumnType("jsonb");
            builder.Property(ij => ij.BinariesAdded).HasColumnType("jsonb");
            builder.Property(ij => ij.BinariesPatched).HasColumnType("jsonb");
            builder.Property(ij => ij.BinariesDeleted).HasColumnType("jsonb");
            builder.Property(ij => ij.ImportJobJson).HasColumnType("jsonb");
        });
    }
}