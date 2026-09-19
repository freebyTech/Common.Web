namespace freebyTech.Common.Web.Metering;

/// <summary>The caller's identity + resolved tier, produced by an <see cref="IUsageContextResolver"/>.</summary>
public sealed record UsageContext(Guid UserId, string Tier);

/// <summary>A usage event to record: one call by default, plus optional token/cost amounts.</summary>
public sealed record UsageRecord(string Bucket, long Calls = 1, long Tokens = 0, long CostCents = 0);

/// <summary>Accumulated counters for a (user, bucket, period).</summary>
public sealed record BucketCounters(long Calls, long Tokens, long CostCents)
{
  public static readonly BucketCounters Zero = new(0, 0, 0);
}

/// <summary>
/// The effective, resolved limits for one (tier/override × bucket), after merging global defaults,
/// the tier's bucket entry, and any per-user override. A null dimension = unlimited on that dimension.
/// </summary>
public sealed class BucketLimits
{
  public bool Unlimited { get; init; }
  public UsageEnforcementMode Enforcement { get; init; }
  public UsageFailMode FailMode { get; init; }
  public int WarnAtPercent { get; init; }
  public int CriticalAtPercent { get; init; }
  public long? Calls { get; init; }
  public long? Tokens { get; init; }
  public long? CostCents { get; init; }
}

/// <summary>Per-dimension view for the check result / usage meter.</summary>
public sealed record DimensionUsage(string Name, long Used, long? Limit, int Percent, UsageState State);

/// <summary>The outcome of a <see cref="IUsageMeter.Check"/> — the gate decision plus the numbers the UI shows.</summary>
public sealed class UsageCheckResult
{
  public required bool Allowed { get; init; }
  public required UsageState State { get; init; }
  public required string Bucket { get; init; }
  public required string Tier { get; init; }
  public required UsageEnforcementMode Enforcement { get; init; }
  public required DateTime ResetAtUtc { get; init; }
  public IReadOnlyList<DimensionUsage> Dimensions { get; init; } = Array.Empty<DimensionUsage>();
}
