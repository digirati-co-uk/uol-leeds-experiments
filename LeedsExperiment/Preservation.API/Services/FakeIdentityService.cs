using Microsoft.EntityFrameworkCore;
using Preservation.API.Data;
using Preservation.API.Models;

namespace Preservation.API.Services;

public class FakeIdentityService(PreservationContext dbContext) : IIdentityService
{
    public async Task<string> MintNewIdentity(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var identityCandidate = Identifiable.Generate();
            if (!await dbContext.Deposits.AnyAsync(d => d.Id == identityCandidate, cancellationToken))
            {
                return identityCandidate;
            }
        }
    }
}

public interface IIdentityService
{
    Task<string> MintNewIdentity(CancellationToken cancellationToken = default);
}