﻿using System.Threading.Channels;
using Preservation.API.Data.Entities;

namespace Preservation.API.Services.ImportJobs;

public interface IImportJobQueue
{
    ValueTask QueueRequest(ImportJobEntity importJob, CancellationToken cancellationToken);
    ValueTask<string> DequeueRequest(CancellationToken cancellationToken);
}

/// <summary>
/// Basic implementation of import job running service using bounded queue for managing / processing
/// </summary>
/// <remarks>This is purely for demo purposes - this would likely use SQS </remarks>
public class InProcessImportJobQueue : IImportJobQueue
{
    private readonly Channel<string> queue;
    
    public InProcessImportJobQueue()
    {
        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        queue = Channel.CreateBounded<string>(options);
    }
    
    public ValueTask QueueRequest(ImportJobEntity importJob, CancellationToken cancellationToken)
        => queue.Writer.WriteAsync(importJob.Id, cancellationToken);

    public ValueTask<string> DequeueRequest(CancellationToken cancellationToken)
        => queue.Reader.ReadAsync(cancellationToken);
}
