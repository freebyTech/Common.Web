using System.Collections.Concurrent;

namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Default <see cref="IUsageStore"/> — an in-process, thread-safe counter keyed by (user, period, bucket).
/// Ships as the self-testable default and a dev fallback. Durable/shared storage (SQL, Redis) is a host
/// adapter registered in its place. Not persistent: counts reset on process restart.
/// </summary>
public sealed class InMemoryUsageStore : IUsageStore
{
  private readonly ConcurrentDictionary<string, BucketCounters> _counters = new();

  private static string Key(Guid userId, string bucket, DateTime periodStartUtc)
    => $"{userId:N}|{periodStartUtc:yyyyMM}|{bucket}";

  public BucketCounters GetCurrent(Guid userId, string bucket, DateTime periodStartUtc)
    => _counters.TryGetValue(Key(userId, bucket, periodStartUtc), out var c) ? c : BucketCounters.Zero;

  public BucketCounters Increment(Guid userId, UsageRecord record, DateTime periodStartUtc)
    => _counters.AddOrUpdate(
        Key(userId, record.Bucket, periodStartUtc),
        _ => new BucketCounters(record.Calls, record.Tokens, record.CostCents),
        (_, existing) => new BucketCounters(
          existing.Calls + record.Calls,
          existing.Tokens + record.Tokens,
          existing.CostCents + record.CostCents));
}

/// <summary>Default <see cref="IUsageOverrideStore"/> — no overrides. The host supplies a real one.</summary>
public sealed class NoOpUsageOverrideStore : IUsageOverrideStore
{
  public BucketLimitOption? GetOverride(Guid userId, string bucket) => null;
}
