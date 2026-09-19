using Microsoft.Extensions.Options;

namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Default <see cref="IUsageContextResolver"/>: reads the caller's id from <see cref="ICurrentUserAccessor"/>
/// and resolves the tier by walking the configured <see cref="UsageMeteringOptions.TierMappings"/> via the
/// host-supplied <see cref="IPrivilegeChecker"/>. Entirely data-driven — no tier names are hardcoded.
/// </summary>
public sealed class TierMappingContextResolver : IUsageContextResolver
{
  private readonly ICurrentUserAccessor _user;
  private readonly IPrivilegeChecker _privileges;
  private readonly IOptionsMonitor<UsageMeteringOptions> _options;

  public TierMappingContextResolver(
    ICurrentUserAccessor user,
    IPrivilegeChecker privileges,
    IOptionsMonitor<UsageMeteringOptions> options)
  {
    _user = user;
    _privileges = privileges;
    _options = options;
  }

  public UsageContext Resolve()
  {
    var tier = UsageDecider.ResolveTier(_options.CurrentValue, _privileges);
    return new UsageContext(_user.UserId, tier);
  }
}
