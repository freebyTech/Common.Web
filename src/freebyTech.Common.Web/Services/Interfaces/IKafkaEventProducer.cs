namespace freebyTech.Common.Web.Services.Interfaces;

public interface IKafkaEventProducer
{
  public string GetTopicName<T>();

  /// Adds a message to producer's queue, flushing the queue if it is full.
  /// </summary>
  /// <param name="key">The key to post</param>
  /// <param name="message">The message to post</param>
  /// <param name="previouslyFoundTopicName">The previously found topic if already know,
  /// otherwise it is determined from configuration based on type.</param>
  /// <typeparam name="K"></typeparam>
  /// <typeparam name="T">The message type</typeparam>
  void ProduceMessage<K, T>(K key, T message, string? previouslyFoundTopicName = null);

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
  /// Adds a series of messages to producer's queue, flushing the queue if it is full.
  /// </summary>
  /// <param name="message">The message to post</param>
  /// <typeparam name="K"></typeparam>
  /// <typeparam name="T">The message type</typeparam>
  void ProduceMessages<K, T>(Dictionary<K, T> messages);

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

  /// <summary>
  /// Produce a message asynchronously.
  /// </summary>
  /// <param name="key">The key to post</param>
  /// <param name="message">The message to post</param>
  /// <typeparam name="K"></typeparam>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  Task ProduceMessageAsync<K, T>(K key, T message);
}
