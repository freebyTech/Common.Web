namespace freebyTech.Common.Web.Options;

/// <summary>
/// This configuration options necessary for Kafka Topic production.
/// It works in conjuction with Kubernetes environment secrets and app
/// settings files to populate these settings.
/// </summary>
/// <example>
/// This class can be used in configuration startup like this:
/// <code>
/// services.Configure<KafkaProducerOptions>(Configuration.GetSection("kafka"));
/// </code>
/// and used via dependency injection like this:
/// <code>
/// public ClassName(IOptionsSnapshot<KafkaProducerOptions> kafkaProducerOptions) { }
/// </code>
/// </example>
public class KafkaProducerOptions : KafkaBaseOptions
{
    /// <summary>
    /// Delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches (MessageSets) to transmit to brokers. A higher value allows larger and more effective (less overhead, improved compression) batches of messages to accumulate at the expense of increased message delivery latency.
    /// </summary>
    /// <remarks>
    /// default: 5 importance: high
    /// </remarks>
    public double? LingerMs { get; set; }
}
