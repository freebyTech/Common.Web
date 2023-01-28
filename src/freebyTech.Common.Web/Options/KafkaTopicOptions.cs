namespace freebyTech.Common.Web.Options;

/// <summary>
/// This configuration options for Kafka topics.
/// </summary>
public class KafkaTopicOptions
{
    /// <summary>
    /// The name of the topic
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The C# type that represents the messages coming from the topic.
    /// </summary>
    public string MessageType { get; set; }
}
