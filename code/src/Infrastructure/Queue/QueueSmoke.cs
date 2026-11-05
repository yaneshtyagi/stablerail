using Amazon.SQS.Model;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Queue;

public static class QueueSmoke
{
    public static async Task RoundTripAsync(QueueOptions options, CancellationToken cancellationToken = default)
    {
        using var client = SqsClients.Create(options);
        var queue = await client.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = options.Name }, cancellationToken);
        var deadLetter = await client.GetQueueUrlAsync(
            new GetQueueUrlRequest { QueueName = options.DeadLetterName },
            cancellationToken);
        if (string.IsNullOrWhiteSpace(queue.QueueUrl) || string.IsNullOrWhiteSpace(deadLetter.QueueUrl))
            throw new InvalidOperationException("The emulator did not return a queue URL.");

        var body = $"presence-{Guid.NewGuid():N}";
        await client.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queue.QueueUrl,
            MessageBody = body,
        }, cancellationToken);
        var received = await client.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queue.QueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 2,
        }, cancellationToken);
        var match = received.Messages.FirstOrDefault(message => message.Body == body)
            ?? throw new InvalidOperationException("The sent queue message was not received.");
        await client.DeleteMessageAsync(queue.QueueUrl, match.ReceiptHandle, cancellationToken);
    }
}
