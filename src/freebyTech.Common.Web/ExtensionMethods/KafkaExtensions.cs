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
    public static IServiceCollection AddCancellableKafkaEventConsumer(
        this IServiceCollection serviceCollection,
        IOptions<KafkaConsumerOptions> consumerOptions
    )
    {
        if (serviceCollection == null)
            throw new ArgumentNullException(nameof(serviceCollection));
        if (consumerOptions == null || consumerOptions.Value == null)
            throw new ArgumentNullException(nameof(consumerOptions));

        return serviceCollection.AddSingleton<
            ICancellableKafkaEventConsumer,
            CancellableKafkaEventConsumer
        >();
    }

    public static IServiceCollection AddRegularOrAvroConsumerBuilderForType<T>(
        this IServiceCollection serviceCollection,
        IOptions<KafkaConsumerOptions> consumerOptions
    )
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

        if (!cf.SchemaRegistryUrls.IsNullOrEmpty())
        {
            SchemaRegistryConfig schemaRegistryConfig = new() { Url = cf.SchemaRegistryUrls };
            CachedSchemaRegistryClient registryClient = new(schemaRegistryConfig);
            consumerBuilder
                .SetValueDeserializer(new AvroDeserializer<T>(registryClient).AsSyncOverAsync())
                .SetErrorHandler(
                    (_, e) =>
                        Log.Error(
                            "Message: {Message} MessageType: {messageType} ErrorCode: {errorCode} ErrorReason: {errorReason}",
                            "Failed to Consume Message",
                            typeof(T).ToString(),
                            e.Code,
                            e.Reason
                        )
                );
        }

        return serviceCollection.AddSingleton(typeof(ConsumerBuilder<Ignore, T>), consumerBuilder);
    }
}
