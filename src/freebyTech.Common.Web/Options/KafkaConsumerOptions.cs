namespace freebyTech.Common.Web.Options;

/// <summary>
/// This configuration options necessary for Kafka Topic consumption.
/// It works in conjuction with Kubernetes environment secrets and app
/// settings files to populate these settings.
/// </summary>
/// <example>
/// This class can be used in configuration startup like this:
/// <code>
/// services.Configure<KafkaConsumerOptions>(Configuration.GetSection("kafka"));
/// </code>
/// and used via dependency injection like this:
/// <code>
/// public ClassName(IOptionsSnapshot<KafkaConsumerOptions> kafkaConsumerOptions) { }
/// </code>
/// </example>
public class KafkaConsumerOptions : KafkaBaseOptions
{
    /// <summary>
    /// The consumer Group ID.
    /// </summary>
    public string GroupId { get; set; }
}
