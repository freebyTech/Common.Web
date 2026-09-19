namespace freebyTech.Common.Web.Metering;

/// <summary>
/// The full metering configuration surface (bound from the "UsageMetering" config section, hot-reloadable
/// via <c>IOptionsMonitor</c>). Generic: tiers are opaque string keys, not a fixed enum — add as many as you
/// like in config with no code change.
/// </summary>
public sealed class UsageMeteringOptions
{
  public const string SectionName = "UsageMetering";

  /// <summary>Master kill-switch for the whole subsystem.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>Fallback enforcement when a bucket doesn't override it.</summary>
  public UsageEnforcementMode DefaultEnforcement { get; set; } = UsageEnforcementMode.Enforce;

  /// <summary>Percent at which state → <see cref="UsageState.Approaching"/>.</summary>
  public int WarnAtPercent { get; set; } = 80;

  /// <summary>Percent at which state → <see cref="UsageState.Critical"/>.</summary>
  public int CriticalAtPercent { get; set; } = 95;

  /// <summary>Quota window. Only "CalendarMonth" is implemented today.</summary>
  public string ResetCadence { get; set; } = "CalendarMonth";

  /// <summary>Reset boundary timezone. Locked to "UTC" for now; per-user-local deferred.</summary>
  public string ResetTimeZone { get; set; } = "UTC";

  /// <summary>Counter backend selector ("Sql" | "Redis"). Informational in Phase A (in-memory store).</summary>
  public string Store { get; set; } = "Sql";

  /// <summary>How long a pod caches a user's tally before re-reading the store.</summary>
  public int CounterCacheTtlSeconds { get; set; } = 20;

  /// <summary>How often buffered increments flush to the store.</summary>
  public int CounterFlushSeconds { get; set; } = 15;

  /// <summary>Whether to write the per-event detail log (adapter concern).</summary>
  public bool DetailLogEnabled { get; set; } = true;

  /// <summary>Prune window for the detail log.</summary>
  public int DetailLogRetentionDays { get; set; } = 90;

  /// <summary>Behavior if the meter is unavailable, unless a bucket overrides it.</summary>
  public UsageFailMode DefaultFailMode { get; set; } = UsageFailMode.Open;

  /// <summary>Tier assigned when no <see cref="TierMappings"/> entry matches the caller.</summary>
  public string DefaultTier { get; set; } = "free";

  /// <summary>Ordered privilege→tier rules; the highest-<see cref="TierMappingOption.Priority"/> match wins.</summary>
  public List<TierMappingOption> TierMappings { get; set; } = new();

  /// <summary>Per-tier limits, keyed by the opaque tier name.</summary>
  public Dictionary<string, TierOption> Tiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Maps a privilege the caller may hold to a tier, with a tie-breaking priority.</summary>
public sealed class TierMappingOption
{
  public string Privilege { get; set; } = string.Empty;
  public string Tier { get; set; } = string.Empty;
  public int Priority { get; set; }
}

/// <summary>A tier's limits. <see cref="Unlimited"/> bypasses every bucket (e.g. an admin tier).</summary>
public sealed class TierOption
{
  public bool Unlimited { get; set; }
  public Dictionary<string, BucketLimitOption> Buckets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Per-bucket limit config. Any dimension left null is unlimited (not gated). Enforcement/warn/fail-mode
/// fall back to the global defaults when null. Also the shape a per-user override supplies.
/// </summary>
public sealed class BucketLimitOption
{
  public UsageEnforcementMode? Enforcement { get; set; }
  public long? CallsPerMonth { get; set; }
  public long? TokensPerMonth { get; set; }
  public long? CostCentsPerMonth { get; set; }
  public int? WarnAtPercent { get; set; }
  public UsageFailMode? FailMode { get; set; }
}
