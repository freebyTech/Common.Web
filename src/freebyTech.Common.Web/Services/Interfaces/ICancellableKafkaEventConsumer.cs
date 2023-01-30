namespace freebyTech.Common.Web.Services.Interfaces;

/// <summary>
/// The Interface for a generic Helper Service for Kafka topic Event Consumption with cancellation.
/// </summary>
public interface ICancellableKafkaEventConsumer
{
  /// <summary>
  /// Listens for events to be dispatched on the topic for MessageType T.
  /// </summary>
  /// <param name="consumerHandler">Action to handle event of MessageType T</param>
  /// <param name="cancellationToken">Token to signal cancellation and shutdown.</param>
  /// <typeparam name="T">The MessageType being listened for.</typeparam>
  /// <returns></returns>
  Task Consume<T>(Action<T> consumerHandler, CancellationToken cancellationToken);
}
