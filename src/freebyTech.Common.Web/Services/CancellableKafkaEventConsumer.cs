using Microsoft.Extensions.Options;
using Confluent.Kafka;
using Serilog;
using System.Diagnostics;
using freebyTech.Common.Web.Options;
using Microsoft.Extensions.DependencyInjection;
using freebyTech.Common.Web.Services.Interfaces;

namespace freebyTech.Common.Web.Services;

/// <summary>
/// The Generic Helper Service for Kafka topic Event Consumption with cancellation.
/// </summary>
/// <seealso cref="freeby.GoGet.Api.Services.Interfaces.ICancellableKafkaEventConsumer" />
public class CancellableKafkaEventConsumer : KafkaMessengerBase, ICancellableKafkaEventConsumer
{
  public CancellableKafkaEventConsumer(IServiceProvider services, IOptions<KafkaConsumerOptions> consumerOptions) : base(services, consumerOptions, typeof(CancellableKafkaEventConsumer).ToString())
  { }

  /// <inheritdoc />
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
          Log.Information("{className}: Consuming Topic {topicName}", _className, topicName);

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
              Log.Error("{className} Message: {message} EventType: {eventType} ResponseTime: {responseTime} Exception: {exception}", _className, "ConsumeFailed", typeof(T), sw.Elapsed, ex.Message);
              throw;
            }

            sw.Stop();
            Log.Debug("{className} Message: {message} EventType: {eventType} ResponseTime: {responseTime}", _className, "Consumed", typeof(T), sw.Elapsed);
          }
        },
        cancellationToken
      );

      await task;
    }
  }
}
