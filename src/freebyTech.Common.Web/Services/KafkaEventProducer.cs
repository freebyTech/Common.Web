using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using freebyTech.Common.Web.Services.Interfaces;
using freebyTech.Common.Web.Options;
using freebyTech.Common.ExtensionMethods;

namespace freebyTech.Common.Web.Services;

public class KafkaEventProducer : KafkaMessengerBase, IKafkaEventProducer, IDisposable
{
  private static readonly TimeSpan FLUSH_TIME = TimeSpan.FromMilliseconds(250);

  private bool _disposed;

  public KafkaEventProducer(IServiceProvider services, IOptions<KafkaProducerOptions> producerOptions) : base(services, producerOptions, typeof(KafkaEventProducer).ToString()) { }

  /// <inheritdoc />
  public Task ProduceMessageAsync<T>(T message)
  {
    if (message == null)
    {
      throw new ArgumentNullException(nameof(message));
    }

    var producer = _services.GetRequiredService<IProducer<string, T>>();

    return producer
      .ProduceAsync(GetTopicName<T>(), new Message<string, T> { Key = Guid.NewGuid().ToString(), Value = message })
      .ContinueWith(task =>
      {
        if (task.IsFaulted)
        {
          Log.Error("Type: {type} Message: {errorMessage}", typeof(T).ToString(), task?.Exception?.InnerException?.Message ?? task?.Exception?.Message);
        }
      });
  }

  /// <inheritdoc />
  public void ProduceMessages<T>(IEnumerable<T> messages)
  {
    var topic = GetTopicName<T>();

    foreach (var evt in messages)
    {
      ProduceMessage(evt, topic);
    }

    Flush<T>();
  }

  /// <inheritdoc />
  public void ProduceMessage<T>(T message, string? previouslyFoundTopicName = null)
  {
    if (message == null)
    {
      throw new ArgumentNullException(nameof(message));
    }

    var producer = _services.GetRequiredService<IProducer<string, T>>();

    var retries = 10;
    var sent = false;

    while (!sent)
    {
      try
      {
        producer.Produce(previouslyFoundTopicName ?? GetTopicName<T>(), new Message<string, T> { Key = Guid.NewGuid().ToString(), Value = message }, HandleDeliveryReport);
        sent = true;
      }
      catch (Exception ex)
      {
        if (ex.Message == "Local: Queue full" && retries-- > 0)
        {
          producer.Flush(FLUSH_TIME);
        }
        else
        {
          throw;
        }
      }
    }
  }

  /// <inheritdoc />
  public void Flush<T>()
  {
    var producer = _services.GetRequiredService<IProducer<string, T>>();
    producer.Flush();
  }

  /// <summary>
  /// Disposes managed resources for this <see cref="KafkaEventProducer"/>.
  /// </summary>
  /// <param name="disposing"></param>
  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      _disposed = true;
    }
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// Processes a <see cref="DeliveryReport{TKey, TValue}"/> received from a produce request.
  /// </summary>
  /// <param name="deliveryReport">A <see cref="DeliveryReport{TKey, TValue}"/> to process.
  /// </param>
  private static void HandleDeliveryReport<T>(DeliveryReport<string, T> deliveryReport)
  {
    if (deliveryReport.Error.Code != ErrorCode.NoError)
    {
      Log.Error("Message: {message} Error: {errorMessage}", "Failed To Deliver", deliveryReport.Error.Reason);
    }
  }
}
