using Confluent.Kafka;
using Microsoft.Extensions.Options;
using freebyTech.Common.ExtensionMethods;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Confluent.Kafka.SyncOverAsync;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using freebyTech.Common.Web.Options;
using freebyTech.Common.Web.Services.Interfaces;
using freebyTech.Common.Web.Services;

namespace freebyTech.Common.Web.ExtensionMethods;

/// <summary>
/// Extension methods to simplify Kafka consumer and producer setup.
/// </summary>
/// <remarks>
/// https://github.com/confluentinc/confluent-kafka-dotnet/blob/master/examples/AvroSpecific/Program.cs
/// </remarks>
public static class KafkaExtensions
{
  public static IServiceCollection AddCancellableKafkaEventConsumer(this IServiceCollection serviceCollection, IOptions<KafkaConsumerOptions> consumerOptions)
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (consumerOptions == null || consumerOptions.Value == null)
      throw new ArgumentNullException(nameof(consumerOptions));

    return serviceCollection.AddSingleton<ICancellableKafkaEventConsumer, CancellableKafkaEventConsumer>();
  }

  public static IServiceCollection AddAvroConsumerBuilderForType<K, T>(this IServiceCollection serviceCollection, IOptions<KafkaConsumerOptions> consumerOptions)
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (consumerOptions == null || consumerOptions.Value == null)
      throw new ArgumentNullException(nameof(consumerOptions));

    var cf = consumerOptions.Value;
    if (cf == null || cf.SchemaRegistryUrls.IsNullOrEmpty())
      throw new ArgumentException("No SchemaRegistryUrls specified.");

    ConsumerConfig config =
      new()
      {
        BootstrapServers = cf.BootstrapServers,
        GroupId = cf.GroupId,
        AutoOffsetReset = cf.AutoOffsetReset ?? AutoOffsetReset.Earliest,
        SecurityProtocol = cf.SecurityProtocol,
        SslCaLocation = cf.SslCALocation,
        SslCertificateLocation = cf.SslCertificateLocation,
        SslKeyLocation = cf.SslKeyLocation
      };

    var consumerBuilder = new ConsumerBuilder<K, T>(config);

    SchemaRegistryConfig schemaRegistryConfig = new() { Url = cf.SchemaRegistryUrls };
    CachedSchemaRegistryClient registryClient = new(schemaRegistryConfig);
    consumerBuilder
      .SetValueDeserializer(new AvroDeserializer<T>(registryClient).AsSyncOverAsync())
      .SetErrorHandler(
        (_, e) => Log.Error("Message: {Message} MessageType: {messageType} ErrorCode: {errorCode} ErrorReason: {errorReason}", "Failed to Consume Message", typeof(T).ToString(), e.Code, e.Reason)
      );

    return serviceCollection.AddSingleton(typeof(ConsumerBuilder<K, T>), consumerBuilder);
  }

  public static IServiceCollection AddAvroConsumerBuilderForType<T>(this IServiceCollection serviceCollection, IOptions<KafkaConsumerOptions> consumerOptions)
  {
    return serviceCollection.AddAvroConsumerBuilderForType<Ignore, T>(consumerOptions);
  }

  public static IServiceCollection AddStandardConsumerBuilderForType<T>(this IServiceCollection serviceCollection, IOptions<KafkaConsumerOptions> consumerOptions)
    where T : ISerializer<T>, IDeserializer<T>, new()
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (consumerOptions == null || consumerOptions.Value == null)
      throw new ArgumentNullException(nameof(consumerOptions));

    var cf = consumerOptions.Value;

    ConsumerConfig config =
      new()
      {
        BootstrapServers = cf.BootstrapServers,
        GroupId = cf.GroupId,
        AutoOffsetReset = cf.AutoOffsetReset ?? AutoOffsetReset.Earliest,
        SecurityProtocol = cf.SecurityProtocol,
        SslCaLocation = cf.SslCALocation,
        SslCertificateLocation = cf.SslCertificateLocation,
        SslKeyLocation = cf.SslKeyLocation
      };

    var consumerBuilder = new ConsumerBuilder<Ignore, T>(config);

    consumerBuilder
      .SetValueDeserializer(new T())
      .SetErrorHandler(
        (_, e) => Log.Error("Message: {Message} MessageType: {messageType} ErrorCode: {errorCode} ErrorReason: {errorReason}", "Failed to Consume Message", typeof(T).ToString(), e.Code, e.Reason)
      );

    return serviceCollection.AddSingleton(typeof(ConsumerBuilder<Ignore, T>), consumerBuilder);
  }

  public static IServiceCollection AddAvroProducerForType<K, T>(this IServiceCollection serviceCollection, IOptions<KafkaProducerOptions> producerOptions)
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (producerOptions == null || producerOptions.Value == null)
      throw new ArgumentNullException(nameof(producerOptions));

    var cf = producerOptions.Value;
    if (cf == null || cf.SchemaRegistryUrls.IsNullOrEmpty())
      throw new ArgumentException("No SchemaRegistryUrls specified.");

    ProducerConfig config =
      new()
      {
        BootstrapServers = cf.BootstrapServers,
        LingerMs = cf.LingerMs,
        SecurityProtocol = cf.SecurityProtocol,
        SslCaLocation = cf.SslCALocation,
        SslCertificateLocation = cf.SslCertificateLocation,
        SslKeyLocation = cf.SslKeyLocation
      };

    var producerBuilder = new ProducerBuilder<K, T>(config);
    SchemaRegistryConfig schemaRegistryConfig = new() { Url = cf.SchemaRegistryUrls };
    CachedSchemaRegistryClient registryClient = new(schemaRegistryConfig);
    var avroSerializerConfig = new AvroSerializerConfig();

    producerBuilder
      .SetKeySerializer(new AvroSerializer<K>(registryClient, avroSerializerConfig).AsSyncOverAsync())
      .SetValueSerializer(new AvroSerializer<T>(registryClient, avroSerializerConfig).AsSyncOverAsync());

    var producer = producerBuilder.Build();

    return serviceCollection.AddSingleton(typeof(IProducer<K, T>), producer);
  }

  public static IServiceCollection AddAvroProducerForType<T>(this IServiceCollection serviceCollection, IOptions<KafkaProducerOptions> producerOptions)
  {
    return serviceCollection.AddAvroProducerForType<Null, T>(producerOptions);
  }

  public static IServiceCollection AddStandardProducerForType<T>(this IServiceCollection serviceCollection, IOptions<KafkaProducerOptions> producerOptions)
    where T : ISerializer<T>, IDeserializer<T>, new()
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (producerOptions == null || producerOptions.Value == null)
      throw new ArgumentNullException(nameof(producerOptions));

    var cf = producerOptions.Value;

    ProducerConfig config =
      new()
      {
        BootstrapServers = cf.BootstrapServers,
        LingerMs = cf.LingerMs,
        SecurityProtocol = cf.SecurityProtocol,
        SslCaLocation = cf.SslCALocation,
        SslCertificateLocation = cf.SslCertificateLocation,
        SslKeyLocation = cf.SslKeyLocation
      };

    var producerBuilder = new ProducerBuilder<Null, T>(config);

    producerBuilder
      .SetValueSerializer(new T())
      .SetErrorHandler(
        (_, e) => Log.Error("Message: {Message} MessageType: {messageType} ErrorCode: {errorCode} ErrorReason: {errorReason}", "Failed to Produce Message", typeof(T).ToString(), e.Code, e.Reason)
      );

    var producer = producerBuilder.Build();

    return serviceCollection.AddSingleton(typeof(IProducer<Null, T>), producer);
  }

  public static IServiceCollection AddKafkaEventProducer(this IServiceCollection serviceCollection, IOptions<KafkaProducerOptions> producerOptions)
  {
    if (serviceCollection == null)
      throw new ArgumentNullException(nameof(serviceCollection));
    if (producerOptions == null || producerOptions.Value == null)
      throw new ArgumentNullException(nameof(producerOptions));

    return serviceCollection.AddSingleton<IKafkaEventProducer, KafkaEventProducer>();
  }
}
