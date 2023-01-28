using Microsoft.Extensions.Options;
using Confluent.Kafka;
using Serilog;
using System.Diagnostics;
using freebyTech.Common.ExtensionMethods;
using freebyTech.Common.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using freebyTech.Common.Web.Services.Interfaces;

namespace freebyTech.Common.Web.Services;

/// <summary>
/// The Generic Helper Service for Kafka topic Event Consumption with cancellation.
/// </summary>
/// <seealso cref="freeby.GoGet.Api.Services.Interfaces.ICancellableKafkaEventConsumer" />
public class CancellableKafkaEventConsumer : ICancellableKafkaEventConsumer
{
    private readonly List<KafkaTopicOptions> _topicOptions;
    private readonly IServiceProvider _services;
    private readonly string _className = typeof(CancellableKafkaEventConsumer).ToString().ToUpper();

    public CancellableKafkaEventConsumer(
        IServiceProvider services,
        IOptions<KafkaConsumerOptions> consumerOptions
    )
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        var opts =
            consumerOptions?.Value ?? throw new ArgumentNullException(nameof(consumerOptions));
        _topicOptions = opts.Topics;
        Log.Information("{className}: Found {count} topics", _className, opts.Topics.Count);
    }

    /// <summary>
    /// Listens for events to be dispatched on the topic for MessageType T.
    /// </summary>
    /// <param name="consumerHandler">Action to handle event of MessageType T</param>
    /// <param name="cancellationToken">Token to signal cancellation and shutdown.</param>
    /// <typeparam name="T">The MessageType being listened for.</typeparam>
    /// <returns></returns>
    public async Task Consume<T>(Action<T> consumerHandler, CancellationToken cancellationToken)
    {
        var consumerBuilder = _services.GetRequiredService<ConsumerBuilder<Ignore, T>>();
        using (var consumer = consumerBuilder.Build())
        {
            var task = Task.Run(
                () =>
                {
                    var topicName = GetTopicName<T>();
                    consumer.Subscribe(topicName);
                    Log.Information(
                        "{className}: Consuming Topic {topicName}",
                        _className,
                        topicName
                    );

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var cr = consumer.Consume(cancellationToken);
                        var evt = cr.Message.Value;

                        Stopwatch sw = new();
                        sw.Start();

                        try
                        {
                            consumerHandler(evt);
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            Log.Error(
                                "{className} Message: {message} EventType: {eventType} ResponseTime: {responseTime} Exception: {exception}",
                                _className,
                                "ConsumeFailed",
                                typeof(T),
                                sw.Elapsed,
                                ex.Message
                            );
                            throw;
                        }

                        sw.Stop();
                        Log.Debug(
                            "{className} Message: {message} EventType: {eventType} ResponseTime: {responseTime}",
                            _className,
                            "Consumed",
                            typeof(T),
                            sw.Elapsed
                        );
                    }
                },
                cancellationToken
            );

            await task;
        }
    }

    private string GetTopicName<T>()
    {
        var messageType = typeof(T).ToString();
        Log.Debug("{className}: Searching for Topic type {messageType}", _className, messageType);
        var topic = _topicOptions.FirstOrDefault(
            topic => topic.MessageType.CompareNoCase(messageType)
        );
        if (topic == null)
        {
            Log.Error("{className}: No Topic for {messageType} found!", _className, messageType);
            throw new ArgumentException(messageType);
        }

        Log.Debug(
            "{className}: Found {topicName} for type {messageType}",
            _className,
            topic.Name,
            messageType
        );

        return topic.Name;
    }
}
