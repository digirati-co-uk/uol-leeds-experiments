using Microsoft.EntityFrameworkCore;
using Storage.API.Data.Entities;

namespace Storage.API.Data;

/// <summary>
/// Commonly used queries/helpers for working with entities
/// </summary>
public static class EntityX
{
    public static ValueTask<DepositEntity?> GetDeposit(this DbSet<DepositEntity> deposits, string id,
        CancellationToken cancellationToken = default) =>
        deposits.FindAsync([id], cancellationToken);

    public static ValueTask<ImportJobEntity?> GetImportJob(this DbSet<ImportJobEntity> importJobs, string id,
        CancellationToken cancellationToken = default) =>
        importJobs.FindAsync([id], cancellationToken);

    public static DepositEntity SetModified(this DepositEntity entity, string lastModified = "leedsadmin")
    {
        entity.LastModifiedBy = lastModified;
        entity.LastModified = DateTime.UtcNow;
        return entity;
    }
}