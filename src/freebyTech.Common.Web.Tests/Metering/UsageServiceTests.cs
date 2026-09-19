using freebyTech.Common.Web.Metering;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace freebyTech.Common.Web.Tests.Metering;

/// <summary>
/// Engine tests for <see cref="UsageService"/> against the in-memory store + fake identity ports —
/// exercises the whole check/record path (enforce blocks, monitor never blocks, unlimited bypass,
/// per-user override, and the master enable switch) with no ASP.NET or DB.
/// </summary>
public class UsageServiceTests
{
  private static readonly Guid User = Guid.NewGuid();

  private sealed class FakePrivileges : IPrivilegeChecker
  {
    private readonly HashSet<string> _held;
    public FakePrivileges(params string[] held) => _held = new HashSet<string>(held, StringComparer.OrdinalIgnoreCase);
    public bool Has(string privilege) => _held.Contains(privilege);
  }

  private sealed class FixedUser : ICurrentUserAccessor
  {
    public bool IsAuthenticated => true;
    public Guid UserId => User;
  }

  private sealed class FakeResolver : IUsageContextResolver
  {
    private readonly string _tier;
    public FakeResolver(string tier) => _tier = tier;
    public UsageContext Resolve() => new(User, _tier);
  }

  private sealed class FakeOverrides : IUsageOverrideStore
  {
    private readonly BucketLimitOption? _ovr;
    public FakeOverrides(BucketLimitOption? ovr = null) => _ovr = ovr;
    public BucketLimitOption? GetOverride(Guid userId, string bucket) => _ovr;
  }

  private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
  {
    public StaticOptionsMonitor(T value) => CurrentValue = value;
    public T CurrentValue { get; }
    public T Get(string? name) => CurrentValue;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
  }

  private static UsageMeteringOptions BaseOptions(UsageEnforcementMode agentMode = UsageEnforcementMode.Enforce) => new()
  {
    Enabled = true,
    DefaultEnforcement = UsageEnforcementMode.Enforce,
    WarnAtPercent = 80,
    CriticalAtPercent = 95,
    DefaultTier = "free",
    Tiers =
    {
      ["free"] = new TierOption { Buckets = { ["agent"] = new BucketLimitOption { Enforcement = agentMode, CallsPerMonth = 2 } } },
      ["admin"] = new TierOption { Unlimited = true }
    }
  };

  private static UsageService Build(UsageMeteringOptions options, string tier, IUsageStore store, IUsageOverrideStore? overrides = null)
    => new(new FakeResolver(tier), store, overrides ?? new FakeOverrides(),
           new StaticOptionsMonitor<UsageMeteringOptions>(options), NullLogger<UsageService>.Instance);

  [Fact]
  public void Enforce_blocks_once_the_limit_is_reached()
  {
    var store = new InMemoryUsageStore();
    var svc = Build(BaseOptions(), "free", store);

    Assert.True(svc.Check("agent").Allowed);          // 0 used
    svc.Record(new UsageRecord("agent"));
    svc.Record(new UsageRecord("agent"));             // now at the limit of 2

    var check = svc.Check("agent");
    Assert.False(check.Allowed);
    Assert.Equal(UsageState.Blocked, check.State);
  }

  [Fact]
  public void Monitor_never_blocks_but_reports_critical()
  {
    var store = new InMemoryUsageStore();
    var svc = Build(BaseOptions(UsageEnforcementMode.Monitor), "free", store);

    svc.Record(new UsageRecord("agent"));
    svc.Record(new UsageRecord("agent"));
    svc.Record(new UsageRecord("agent"));             // over the limit

    var check = svc.Check("agent");
    Assert.True(check.Allowed);                        // Monitor: allowed despite being over
    Assert.Equal(UsageState.Critical, check.State);    // ...but flagged
  }

  [Fact]
  public void Unlimited_tier_is_never_blocked()
  {
    var store = new InMemoryUsageStore();
    var svc = Build(BaseOptions(), "admin", store);

    for (var i = 0; i < 100; i++) svc.Record(new UsageRecord("agent"));

    Assert.True(svc.Check("agent").Allowed);
  }

  [Fact]
  public void Per_user_override_raises_the_limit()
  {
    var store = new InMemoryUsageStore();
    var overrides = new FakeOverrides(new BucketLimitOption { CallsPerMonth = 100 });
    var svc = Build(BaseOptions(), "free", store, overrides);

    for (var i = 0; i < 5; i++) svc.Record(new UsageRecord("agent")); // past the tier's 2, under override 100

    Assert.True(svc.Check("agent").Allowed);
  }

  [Fact]
  public void Disabled_switch_makes_everything_pass()
  {
    var options = BaseOptions();
    options.Enabled = false;
    var store = new InMemoryUsageStore();
    var svc = Build(options, "free", store);

    for (var i = 0; i < 10; i++) svc.Record(new UsageRecord("agent"));

    var check = svc.Check("agent");
    Assert.True(check.Allowed);
    Assert.Equal(UsageState.Ok, check.State);
  }

  [Fact]
  public void Approaching_state_surfaces_before_the_block()
  {
    // limit 10, warn 80% → at 8 calls we expect Approaching, still allowed.
    var options = BaseOptions();
    options.Tiers["free"].Buckets["agent"].CallsPerMonth = 10;
    var store = new InMemoryUsageStore();
    var svc = Build(options, "free", store);

    for (var i = 0; i < 8; i++) svc.Record(new UsageRecord("agent"));

    var check = svc.Check("agent");
    Assert.True(check.Allowed);
    Assert.Equal(UsageState.Approaching, check.State);
  }
}
