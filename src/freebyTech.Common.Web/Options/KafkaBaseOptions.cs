using Confluent.Kafka;

namespace freebyTech.Common.Web.Options;

/// <summary>
/// This configuration options abstract base class class that contains the configuration items
// common to both producers and consumers.
public class KafkaBaseOptions
{
  /// <summary>
  /// The list of brokers as a comma seperated list of broker host or host:port.
  /// ex. [::1]:9092
  /// </summary>
  public string BootstrapServers { get; set; }

  /// <summary>
  /// The list of schema registry URLs as a comma seperated list or null if no schema registry is used.
  /// </summary>
  public string? SchemaRegistryUrls { get; set; }

  /// <summary>
  /// The broker security protocol used.
  /// </summary>
  public SecurityProtocol? SecurityProtocol { get; set; }

  /// <summary>
  /// The auto offset reset configuration, defines how a consumer should behave when consuming from a topic
  /// partition when there is no initial offset.
  /// </summary>
  /// <remarks>
  /// This is most typically of interest when a new consumer group has been defined and is listening to a topic for the first time.
  /// This configuration will tell the consumers in the group whether to read from the beginning or end of the partition.
  ///
  /// Defaults to AutoOffsetReset.Earliest.
  /// </remarks>
  public AutoOffsetReset? AutoOffsetReset { get; set; }

  /// <summary>
  /// The file locaiton of the SSL CA Root certificates.
  /// </summary>
  public string? SslCALocation { get; set; }

  /// <summary>
  /// The file locaiton of the client SSL Certificate.
  /// </summary>
  public string? SslCertificateLocation { get; set; }

  /// <summary>
  /// The file locaiton of the client SSL key.
  /// </summary>
  public string? SslKeyLocation { get; set; }

  /// <summary>
  /// The topics available on the broker for production and consumptions of messages.
  /// </summary>

  public List<KafkaTopicOptions> Topics { get; set; }
}
