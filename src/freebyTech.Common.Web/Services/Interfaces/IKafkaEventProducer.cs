namespace freebyTech.Common.Web.Services.Interfaces;

public interface IKafkaEventProducer
{
  /// Adds a message to producer's queue, flushing the queue if it is full.
  /// </summary>
  /// <param name="message">The message to post</param>
  /// <param name="previouslyFoundTopicName">The previously found topic if already know,
  /// otherwise it is determined from configuration based on type.</param>
  /// <typeparam name="T">The message type</typeparam>
  void ProduceMessage<T>(T message, string? previouslyFoundTopicName = null);

  /// <summary>
  /// Adds a series of messages to producer's queue, flushing the queue if it is full.
  /// </summary>
  /// <param name="message">The message to post</param>
  /// <typeparam name="T">The message type</typeparam>
  void ProduceMessages<T>(IEnumerable<T> messages);

  /// <summary>
  /// Blocks until producer has pushed all of its messages to the broker.
  /// </summary>
  /// <typeparam name="T">The message type</typeparam>
  void Flush<T>();

  /// <summary>
  /// Produce a message asynchronously.
  /// </summary>
  /// <param name="message">The message to post</param>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  Task ProduceMessageAsync<T>(T message);
}
