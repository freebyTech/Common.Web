using freebyTech.Common.Web.Metering;
using Xunit;

namespace freebyTech.Common.Web.Tests.Metering;

/// <summary>
/// Pure tests for the metering decision core (tier resolution, limit resolution, percent/state math,
/// over-limit evaluation) — no DI, no HTTP, no store. Mirrors the OwnerGuardTests approach.
/// </summary>
public class UsageDeciderTests
{
  private sealed class FakePrivileges : IPrivilegeChecker
  {
    private readonly HashSet<string> _held;
    public FakePrivileges(params string[] held) => _held = new HashSet<string>(held, StringComparer.OrdinalIgnoreCase);
    public bool Has(string privilege) => _held.Contains(privilege);
  }

  private static UsageMeteringOptions OptionsWithTiers() => new()
  {
    DefaultTier = "free",
    DefaultEnforcement = UsageEnforcementMode.Enforce,
    WarnAtPercent = 80,
    CriticalAtPercent = 95,
    TierMappings =
    {
      new TierMappingOption { Privilege = "admin-user", Tier = "admin", Priority = 100 },
      new TierMappingOption { Privilege = "pro-user",   Tier = "pro",   Priority = 30 },
      new TierMappingOption { Privilege = "paid-user",  Tier = "paid",  Priority = 20 }
    },
    Tiers =
    {
      ["free"] = new TierOption { Buckets = { ["agent"] = new BucketLimitOption { CallsPerMonth = 50 } } },
      ["paid"] = new TierOption { Buckets = { ["agent"] = new BucketLimitOption { CallsPerMonth = 1000 } } },
      ["admin"] = new TierOption { Unlimited = true }
    }
  };

  // ── Tier resolution ───────────────────────────────────────────────────────────
  [Fact]
  public void ResolveTier_no_matching_privilege_falls_back_to_default()
    => Assert.Equal("free", UsageDecider.ResolveTier(OptionsWithTiers(), new FakePrivileges()));

  [Fact]
  public void ResolveTier_single_match_wins()
    => Assert.Equal("paid", UsageDecider.ResolveTier(OptionsWithTiers(), new FakePrivileges("paid-user")));

  [Fact]
  public void ResolveTier_highest_priority_wins_when_multiple_held()
    => Assert.Equal("admin", UsageDecider.ResolveTier(OptionsWithTiers(), new FakePrivileges("paid-user", "admin-user", "pro-user")));

  // ── Limit resolution (override → tier-bucket → global) ──────────────────────────
  [Fact]
  public void ResolveLimits_unlimited_tier_short_circuits()
  {
    var limits = UsageDecider.ResolveLimits(OptionsWithTiers(), "admin", "agent", null);
    Assert.True(limits.Unlimited);
  }

  [Fact]
  public void ResolveLimits_uses_tier_bucket_value_and_global_enforcement_fallback()
  {
    var limits = UsageDecider.ResolveLimits(OptionsWithTiers(), "free", "agent", null);
    Assert.Equal(50, limits.Calls);
    Assert.Equal(UsageEnforcementMode.Enforce, limits.Enforcement); // fell back to global default
  }

  [Fact]
  public void ResolveLimits_override_wins_over_tier()
  {
    var ovr = new BucketLimitOption { CallsPerMonth = 999, Enforcement = UsageEnforcementMode.Monitor };
    var limits = UsageDecider.ResolveLimits(OptionsWithTiers(), "free", "agent", ovr);
    Assert.Equal(999, limits.Calls);
    Assert.Equal(UsageEnforcementMode.Monitor, limits.Enforcement);
  }

  [Fact]
  public void ResolveLimits_unknown_tier_leaves_dimensions_null_unlimited()
  {
    var limits = UsageDecider.ResolveLimits(OptionsWithTiers(), "does-not-exist", "agent", null);
    Assert.Null(limits.Calls);
    Assert.False(limits.Unlimited);
  }

  // ── Percent / dimension state ───────────────────────────────────────────────────
  [Theory]
  [InlineData(0, 10, 0)]
  [InlineData(8, 10, 80)]
  [InlineData(10, 10, 100)]
  [InlineData(12, 10, 120)]
  public void Percent_computes_ratio(long used, long limit, int expected)
    => Assert.Equal(expected, UsageDecider.Percent(used, limit));

  [Fact]
  public void Percent_null_limit_is_zero() => Assert.Equal(0, UsageDecider.Percent(5, null));

  [Fact]
  public void Percent_zero_limit_is_maxed_when_used() => Assert.Equal(int.MaxValue, UsageDecider.Percent(1, 0));

  [Theory]
  [InlineData(70, 100, UsageState.Ok)]
  [InlineData(80, 100, UsageState.Approaching)]
  [InlineData(95, 100, UsageState.Critical)]
  [InlineData(100, 100, UsageState.Critical)] // enforcement promotes to Blocked in the service, not here
  public void DimensionState_thresholds(long used, long limit, UsageState expected)
    => Assert.Equal(expected, UsageDecider.DimensionState(used, limit, warnAtPercent: 80, criticalAtPercent: 95));

  [Fact]
  public void DimensionState_null_limit_is_ok()
    => Assert.Equal(UsageState.Ok, UsageDecider.DimensionState(9999, null, 80, 95));

  // ── Evaluate ────────────────────────────────────────────────────────────────────
  [Fact]
  public void Evaluate_over_limit_flags_and_worst_state()
  {
    var limits = new BucketLimits { Enforcement = UsageEnforcementMode.Enforce, WarnAtPercent = 80, CriticalAtPercent = 95, Calls = 2 };
    var (over, soft) = UsageDecider.Evaluate(new BucketCounters(2, 0, 0), limits);
    Assert.True(over);
    Assert.Equal(UsageState.Critical, soft);
  }

  [Fact]
  public void Evaluate_under_limit_is_ok()
  {
    var limits = new BucketLimits { Enforcement = UsageEnforcementMode.Enforce, WarnAtPercent = 80, CriticalAtPercent = 95, Calls = 10 };
    var (over, soft) = UsageDecider.Evaluate(new BucketCounters(1, 0, 0), limits);
    Assert.False(over);
    Assert.Equal(UsageState.Ok, soft);
  }

  [Fact]
  public void Evaluate_unlimited_never_over()
  {
    var limits = new BucketLimits { Unlimited = true };
    var (over, soft) = UsageDecider.Evaluate(new BucketCounters(long.MaxValue, long.MaxValue, long.MaxValue), limits);
    Assert.False(over);
    Assert.Equal(UsageState.Ok, soft);
  }

  [Fact]
  public void Evaluate_gates_on_tokens_too()
  {
    var limits = new BucketLimits { Enforcement = UsageEnforcementMode.Enforce, WarnAtPercent = 80, CriticalAtPercent = 95, Calls = 1000, Tokens = 100 };
    var (over, _) = UsageDecider.Evaluate(new BucketCounters(1, 100, 0), limits);
    Assert.True(over); // tokens hit even though calls are fine
  }
}
