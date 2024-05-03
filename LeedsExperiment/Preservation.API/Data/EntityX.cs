using Microsoft.EntityFrameworkCore;
using Preservation.API.Data.Entities;

namespace Preservation.API.Data;

/// <summary>
/// Commonly used queries/helpers for working with entities
/// </summary>
public static class EntityX
{
    public static ValueTask<DepositEntity?> GetDeposit(this DbSet<DepositEntity> deposits, string id,
        CancellationToken cancellationToken = default) =>
        deposits.FindAsync([id], cancellationToken);

    public static DepositEntity SetModified(this DepositEntity entity, string lastModified = "leedsadmin")
    {
        entity.LastModifiedBy = lastModified;
        entity.LastModified = DateTime.UtcNow;
        return entity;
    }
}