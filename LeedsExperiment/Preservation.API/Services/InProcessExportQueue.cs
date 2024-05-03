using System.Threading.Channels;
using Preservation.API.Data.Entities;

namespace Preservation.API.Services;

public interface IExportQueue
{
    ValueTask QueueRequest(DepositEntity deposit, CancellationToken cancellationToken);
    ValueTask<DepositEntity> DequeueRequest(CancellationToken cancellationToken);
}

/// <summary>
/// Basic implementation of export service using bounded queue for managing / processing
/// </summary>
/// <remarks>This is purely for demo purposes - this would likely use SQS </remarks>
public class InProcessExportQueue : IExportQueue
{
    private readonly Channel<DepositEntity> queue;
    
    public InProcessExportQueue()
    {
        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        queue = Channel.CreateBounded<DepositEntity>(options);
    }

    public ValueTask QueueRequest(DepositEntity deposit, CancellationToken cancellationToken) =>
        queue.Writer.WriteAsync(deposit, cancellationToken);

    public ValueTask<DepositEntity> DequeueRequest(CancellationToken cancellationToken)
        => queue.Reader.ReadAsync(cancellationToken);
}