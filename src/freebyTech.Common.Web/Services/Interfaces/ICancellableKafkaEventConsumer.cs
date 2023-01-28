namespace freebyTech.Common.Web.Services.Interfaces;

/// <summary>
/// The Interface for a generic Helper Service for Kafka topic Event Consumption with cancellation.
/// </summary>
public interface ICancellableKafkaEventConsumer
{
    /// <summary>
    /// A cancellable Kafka event consumer for a given generic type T.
    /// </summary>
    /// <param name="consumerHandler">The action to handle the event of messag type T.</param>
    /// <param name="cancellationToken">The optional cancellation token that can stop topic consumption.</param>
    Task Consume<T>(Action<T> consumerHandler, CancellationToken cancellationToken);
}
