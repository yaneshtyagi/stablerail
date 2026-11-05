using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StableRail.BuildingBlocks.Telemetry;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Queue;

/// <summary>
/// Confirms the configured queue exists. Publishing, leases, retries and dead letters belong to slice 5.
/// </summary>
public sealed class QueuePresenceService(
    IAmazonSQS sqs,
    IOptions<StableRailOptions> options,
    ILogger<QueuePresenceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = options.Value.Queue.Name;
        if (string.IsNullOrWhiteSpace(queueName))
        {
            logger.LogWarning("Durable queue name is not configured");
            StableRailTelemetry.QueuePresenceChecks.Add(1, new KeyValuePair<string, object?>("result", "unconfigured"));
        }
        else
        {
            try
            {
                _ = await sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName }, stoppingToken);
                logger.LogInformation("Durable queue {QueueName} is reachable", queueName);
                StableRailTelemetry.QueuePresenceChecks.Add(1, new KeyValuePair<string, object?>("result", "reachable"));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Durable queue {QueueName} is not reachable ({ExceptionType})",
                    queueName,
                    exception.GetType().Name);
                StableRailTelemetry.QueuePresenceChecks.Add(1, new KeyValuePair<string, object?>("result", "unreachable"));
            }
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
