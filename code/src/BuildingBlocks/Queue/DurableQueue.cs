namespace StableRail.BuildingBlocks.Queue;

/// <summary>
/// The one durable queue for the control plane. Amazon SQS is the product.
/// ElasticMQ is the local SQS-compatible emulator. RabbitMQ is not used.
/// The transactional outbox remains the authority; slice 5 owns publish, retry and dead-letter behavior.
/// </summary>
public static class DurableQueue
{
    public const string Product = "AmazonSqs";
    public const string Type = "Standard";
    public const string Emulator = "ElasticMQ";
    public const string EmulatorImage = "softwaremill/elasticmq-native:1.7.1";
    public const string QueueName = "stablerail-control-plane";
    public const string DeadLetterQueueName = "stablerail-control-plane-dlq";
    public const string Region = "us-east-1";
    public const string AccountId = "000000000000";
    public const string LocalServiceUrl = "http://localhost:9324";
    public const string LocalAccessKey = "local";
    public const string LocalSecretKey = "local";
    public const int LocalApiPort = 9324;
    public const int LocalUiPort = 9325;
    public const int VisibilityTimeoutSeconds = 30;
    public const int ReceiveWaitTimeSeconds = 20;
    public const int MaxReceiveCount = 5;
}
