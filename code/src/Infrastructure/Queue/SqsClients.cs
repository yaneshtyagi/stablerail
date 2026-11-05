using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Queue;

public static class SqsClients
{
    public static IAmazonSQS Create(QueueOptions options)
    {
        var config = new AmazonSQSConfig
        {
            AuthenticationRegion = options.Region,
            Timeout = TimeSpan.FromSeconds(2),
            MaxErrorRetry = 0,
        };
        if (!string.IsNullOrWhiteSpace(options.Region))
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            config.ServiceURL = options.ServiceUrl;

        if (!string.IsNullOrWhiteSpace(options.AccessKey))
            return new AmazonSQSClient(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);

        return new AmazonSQSClient(config);
    }
}
