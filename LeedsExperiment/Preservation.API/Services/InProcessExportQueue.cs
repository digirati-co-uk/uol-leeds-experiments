using System.Threading.Channels;
using Preservation.API.Controllers;
using Preservation.API.Data.Entities;

namespace Preservation.API.Services;

public interface IExportQueue
{
    ValueTask QueueRequest(DepositEntity deposit, ExportDeposit exportDeposit, CancellationToken cancellationToken);
    ValueTask<ExportRequest> DequeueRequest(CancellationToken cancellationToken);
}

/// <summary>
/// Basic implementation of export service using bounded queue for managing / processing
/// </summary>
/// <remarks>This is purely for demo purposes - this would likely use SQS </remarks>
public class InProcessExportQueue : IExportQueue
{
    private readonly Channel<ExportRequest> queue;
    
    public InProcessExportQueue()
    {
        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        queue = Channel.CreateBounded<ExportRequest>(options);
    }

    public ValueTask QueueRequest(DepositEntity deposit, ExportDeposit exportDeposit,
        CancellationToken cancellationToken) =>
        queue.Writer.WriteAsync(new ExportRequest(deposit.Id, exportDeposit.Version), cancellationToken);

    public ValueTask<ExportRequest> DequeueRequest(CancellationToken cancellationToken)
        => queue.Reader.ReadAsync(cancellationToken);
}

public record ExportRequest(string DepositId, string? Version);