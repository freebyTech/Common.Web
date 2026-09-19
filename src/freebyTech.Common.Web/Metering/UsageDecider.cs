namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Pure, dependency-free metering decisions — tier resolution, limit resolution, and the over-limit /
/// state evaluation. Kept side-effect-free so it can be unit-tested without DI, HTTP, or a store
/// (mirrors the OwnerGuard pattern).
/// </summary>
public static class UsageDecider
{
  /// <summary>
  /// Resolve the caller's tier by walking <see cref="UsageMeteringOptions.TierMappings"/> — the
  /// highest-<see cref="TierMappingOption.Priority"/> mapping whose privilege the caller holds wins —
  /// falling back to <see cref="UsageMeteringOptions.DefaultTier"/>.
  /// </summary>
  public static string ResolveTier(UsageMeteringOptions options, IPrivilegeChecker privileges)
  {
    string? best = null;
    var bestPriority = int.MinValue;
    foreach (var mapping in options.TierMappings)
    {
      if (mapping.Priority > bestPriority
          && !string.IsNullOrEmpty(mapping.Privilege)
          && privileges.Has(mapping.Privilege))
      {
        best = mapping.Tier;
        bestPriority = mapping.Priority;
      }
    }
    return string.IsNullOrEmpty(best) ? options.DefaultTier : best!;
  }

  /// <summary>
  /// Merge global defaults, the tier's bucket entry, and any per-user override into the effective limits.
  /// Precedence per field: override → tier-bucket → global default. A tier flagged
  /// <see cref="TierOption.Unlimited"/> short-circuits to unlimited.
  /// </summary>
  public static BucketLimits ResolveLimits(UsageMeteringOptions options, string tier, string bucket, BucketLimitOption? ovr)
  {
    options.Tiers.TryGetValue(tier, out var tierOption);

    if (tierOption is { Unlimited: true })
    {
      return new BucketLimits
      {
        Unlimited = true,
        Enforcement = UsageEnforcementMode.Off,
        FailMode = options.DefaultFailMode,
        WarnAtPercent = options.WarnAtPercent,
        CriticalAtPercent = options.CriticalAtPercent
      };
    }

    BucketLimitOption? bucketOption = null;
    tierOption?.Buckets.TryGetValue(bucket, out bucketOption);

    return new BucketLimits
    {
      Unlimited = false,
      Enforcement = ovr?.Enforcement ?? bucketOption?.Enforcement ?? options.DefaultEnforcement,
      FailMode = ovr?.FailMode ?? bucketOption?.FailMode ?? options.DefaultFailMode,
      WarnAtPercent = ovr?.WarnAtPercent ?? bucketOption?.WarnAtPercent ?? options.WarnAtPercent,
      CriticalAtPercent = options.CriticalAtPercent,
      Calls = ovr?.CallsPerMonth ?? bucketOption?.CallsPerMonth,
      Tokens = ovr?.TokensPerMonth ?? bucketOption?.TokensPerMonth,
      CostCents = ovr?.CostCentsPerMonth ?? bucketOption?.CostCentsPerMonth
    };
  }

  /// <summary>Percent of a limit used (may exceed 100). Returns 0 for an unlimited dimension.</summary>
  public static int Percent(long used, long? limit)
  {
    if (limit is null) return 0;
    if (limit.Value <= 0) return used > 0 ? int.MaxValue : 0; // a 0 limit = no allowance
    return (int)Math.Min(int.MaxValue, used * 100L / limit.Value);
  }

  /// <summary>
  /// The state of one dimension, independent of enforcement (so Monitor and Enforce share the math).
  /// A dimension at/over 100% is <see cref="UsageState.Critical"/> here; the service promotes it to
  /// <see cref="UsageState.Blocked"/> only when enforcing.
  /// </summary>
  public static UsageState DimensionState(long used, long? limit, int warnAtPercent, int criticalAtPercent)
  {
    if (limit is null) return UsageState.Ok;
    var pct = Percent(used, limit);
    if (pct >= 100) return UsageState.Critical;
    if (pct >= criticalAtPercent) return UsageState.Critical;
    if (pct >= warnAtPercent) return UsageState.Approaching;
    return UsageState.Ok;
  }

  /// <summary>True when any limited dimension has reached/exceeded its limit.</summary>
  public static bool IsOverLimit(BucketCounters used, BucketLimits limits)
  {
    if (limits.Unlimited || limits.Enforcement == UsageEnforcementMode.Off) return false;
    return AtOrOver(used.Calls, limits.Calls)
        || AtOrOver(used.Tokens, limits.Tokens)
        || AtOrOver(used.CostCents, limits.CostCents);
  }

  private static bool AtOrOver(long used, long? limit) => limit is not null && Percent(used, limit) >= 100;

  /// <summary>
  /// Evaluate a bucket: the worst dimension state (ignoring enforcement) and whether any limit is over.
  /// The service turns this into the final state + allow/deny using the enforcement mode.
  /// </summary>
  public static (bool overLimit, UsageState softState) Evaluate(BucketCounters used, BucketLimits limits)
  {
    if (limits.Unlimited || limits.Enforcement == UsageEnforcementMode.Off)
      return (false, UsageState.Ok);

    var worst = UsageState.Ok;
    worst = Worst(worst, DimensionState(used.Calls, limits.Calls, limits.WarnAtPercent, limits.CriticalAtPercent));
    worst = Worst(worst, DimensionState(used.Tokens, limits.Tokens, limits.WarnAtPercent, limits.CriticalAtPercent));
    worst = Worst(worst, DimensionState(used.CostCents, limits.CostCents, limits.WarnAtPercent, limits.CriticalAtPercent));

    return (IsOverLimit(used, limits), worst);
  }

  private static UsageState Worst(UsageState a, UsageState b) => a >= b ? a : b;
}
