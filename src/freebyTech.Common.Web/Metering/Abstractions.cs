namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Port: "does the current caller hold this privilege?" The host implements it (e.g. wrapping the app's
/// own privilege/claims check). Common.Web never learns the app's user model or privilege vocabulary.
/// </summary>
public interface IPrivilegeChecker
{
  bool Has(string privilege);
}

/// <summary>Port: the current caller's id. Host-supplied (from the authenticated principal).</summary>
public interface ICurrentUserAccessor
{
  bool IsAuthenticated { get; }
  Guid UserId { get; }
}

/// <summary>Resolves the caller's <see cref="UsageContext"/> (id + tier). A default data-driven
/// implementation ships in Common.Web; the host may replace it.</summary>
public interface IUsageContextResolver
{
  UsageContext Resolve();
}

/// <summary>
/// Port: the counter store. Reads the current period's counters and applies an increment. The default
/// in-memory implementation ships in Common.Web (self-testable); the host supplies a durable adapter.
/// </summary>
public interface IUsageStore
{
  BucketCounters GetCurrent(Guid userId, string bucket, DateTime periodStartUtc);
  BucketCounters Increment(Guid userId, UsageRecord record, DateTime periodStartUtc);
}

/// <summary>Port: per-user override limits for a bucket, or null if none. Host-supplied; default is no-op.</summary>
public interface IUsageOverrideStore
{
  BucketLimitOption? GetOverride(Guid userId, string bucket);
}

/// <summary>The metering facade the app uses: check a bucket before serving, record usage after.</summary>
public interface IUsageMeter
{
  UsageCheckResult Check(string bucket);
  void Record(UsageRecord record);
}
