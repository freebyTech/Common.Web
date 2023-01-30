using Microsoft.Extensions.Options;
using Serilog;
using freebyTech.Common.ExtensionMethods;
using freebyTech.Common.Web.Options;

namespace freebyTech.Common.Web.Services;

public class KafkaMessengerBase
{
  protected readonly List<KafkaTopicOptions> _topicOptions;
  protected readonly IServiceProvider _services;
  protected readonly string _className;

  public KafkaMessengerBase(IServiceProvider services, IOptions<KafkaBaseOptions> options, string className)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
    var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
    _topicOptions = opts.Topics;
    _className = className;
  }

  public string GetTopicName<T>()
  {
    var messageType = typeof(T).ToString();
    Log.Debug("{className}: Searching for Topic type {messageType}", _className, messageType);
    var topic = _topicOptions.FirstOrDefault(topic => topic.MessageType.CompareNoCase(messageType));
    if (topic == null)
    {
      Log.Error("{className}: No Topic for {messageType} found!", _className, messageType);
      throw new ArgumentException(messageType);
    }

    Log.Debug("{className}: Found {topicName} for type {messageType}", _className, topic.Name, messageType);

    return topic.Name;
  }
}
