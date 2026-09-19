using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace freebyTech.Common.Web.Metering;

/// <summary>
/// The metering engine (<see cref="IUsageMeter"/>). Composes the resolver, override store, config, and
/// counter store into a check/record facade. Contains no app-domain knowledge — it works entirely in
/// opaque tiers + buckets.
/// </summary>
public sealed class UsageService : IUsageMeter
{
  private readonly IUsageContextResolver _resolver;
  private readonly IUsageStore _store;
  private readonly IUsageOverrideStore _overrides;
  private readonly IOptionsMonitor<UsageMeteringOptions> _options;
  private readonly ILogger<UsageService> _logger;

  public UsageService(
    IUsageContextResolver resolver,
    IUsageStore store,
    IUsageOverrideStore overrides,
    IOptionsMonitor<UsageMeteringOptions> options,
    ILogger<UsageService> logger)
  {
    _resolver = resolver;
    _store = store;
    _overrides = overrides;
    _options = options;
    _logger = logger;
  }

  public UsageCheckResult Check(string bucket)
  {
    var options = _options.CurrentValue;
    var periodStart = PeriodStartUtc(options);
    var resetAt = periodStart.AddMonths(1);

    if (!options.Enabled)
      return Unmetered(bucket, tier: string.Empty, resetAt);

    var context = _resolver.Resolve();
    var ovr = _overrides.GetOverride(context.UserId, bucket);
    var limits = UsageDecider.ResolveLimits(options, context.Tier, bucket, ovr);

    if (limits.Unlimited || limits.Enforcement == UsageEnforcementMode.Off)
      return Unmetered(bucket, context.Tier, resetAt, limits.Enforcement);

    var used = _store.GetCurrent(context.UserId, bucket, periodStart);
    var (overLimit, softState) = UsageDecider.Evaluate(used, limits);

    var enforcing = limits.Enforcement == UsageEnforcementMode.Enforce;
    var blocked = overLimit && enforcing;
    var state = blocked ? UsageState.Blocked : softState;
    var allowed = !blocked;

    if (overLimit && !enforcing)
    {
      // Monitor mode: surface the would-be-block so limits can be calibrated before enforcing.
      _logger.LogWarning(
        "Usage would-be-block (Monitor): user {UserId} tier {Tier} bucket {Bucket} over limit (calls {Calls}, tokens {Tokens}).",
        context.UserId, context.Tier, bucket, used.Calls, used.Tokens);
    }

    return new UsageCheckResult
    {
      Allowed = allowed,
      State = state,
      Bucket = bucket,
      Tier = context.Tier,
      Enforcement = limits.Enforcement,
      ResetAtUtc = resetAt,
      Dimensions = BuildDimensions(used, limits)
    };
  }

  public void Record(UsageRecord record)
  {
    var options = _options.CurrentValue;
    if (!options.Enabled) return;

    var context = _resolver.Resolve();
    _store.Increment(context.UserId, record, PeriodStartUtc(options));
  }

  private static UsageCheckResult Unmetered(string bucket, string tier, DateTime resetAt,
    UsageEnforcementMode enforcement = UsageEnforcementMode.Off) => new()
  {
    Allowed = true,
    State = UsageState.Ok,
    Bucket = bucket,
    Tier = tier,
    Enforcement = enforcement,
    ResetAtUtc = resetAt,
    Dimensions = Array.Empty<DimensionUsage>()
  };

  private static IReadOnlyList<DimensionUsage> BuildDimensions(BucketCounters used, BucketLimits limits)
  {
    var list = new List<DimensionUsage>(3);
    AddDimension(list, "calls", used.Calls, limits.Calls, limits);
    AddDimension(list, "tokens", used.Tokens, limits.Tokens, limits);
    AddDimension(list, "costCents", used.CostCents, limits.CostCents, limits);
    return list;
  }

  private static void AddDimension(List<DimensionUsage> list, string name, long used, long? limit, BucketLimits limits)
  {
    if (limit is null) return; // unlimited dimension — omit from the meter
    list.Add(new DimensionUsage(
      name, used, limit,
      UsageDecider.Percent(used, limit),
      UsageDecider.DimensionState(used, limit, limits.WarnAtPercent, limits.CriticalAtPercent)));
  }

  private static DateTime PeriodStartUtc(UsageMeteringOptions options)
  {
    // CalendarMonth in UTC (the only cadence implemented). ResetTimeZone is UTC by design.
    var now = DateTime.UtcNow;
    return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
  }
}
