# Common.Web

Common Web Library for freebyTech Software written in .NET 10.0. Because it is written in .NET 10.0, this library can be run on Windows, Linux, or a Mac.

Technologies currently used are:

- .NET 10.0

## Installing Required Tools

| Tool                               | URL                                            |
| ---------------------------------- | ---------------------------------------------- |
| Git for Windows                    | https://git-scm.com/download/win               |
| Install Latest .NET 10.0 SDK       | https://www.microsoft.com/net/download/windows |
| Install Latest LTS Version of Node | https://nodejs.org/en/                         |
| Install Latest LTS Version of Node | https://nodejs.org/en/                         |

# Building this project

```
# Building the library
cd ./src/freebyTech.Common.Web
dotnet build

# Building and running unit tests
cd ./src/freebyTech.Common.Web.Tests
dotnet test

# Build via the docker build file
docker build ./src -t freebyTech/common-web
```

# Pulling this package from NuGet

The standard released version of this package can be referenced in a project by pulling it from NuGet.

```
# VS Code or command line usage
dotnet add package freebyTech.Common.Web

# Visual Studio Package Manager Console
install-package freebyTech.Common.Web
```

---

# Usage Metering & Quotas

A drop-in component for **per-user API usage metering and quota enforcement**. One set of counters drives
four things at once:

1. **Quota limiting** — block a user once they exceed their monthly allowance.
2. **Usage analytics** — how many calls / tokens / cost each user has consumed this period.
3. **The 402 decision** — the component decides *allow vs. deny* and supplies the reset time.
4. **A user-facing usage meter** — `Check` returns the used / limit / percent / state per dimension.

It is **generic**: it knows nothing about your user model, your privilege names, or your database. It works
in terms of **opaque tier strings** and a small set of **ports** your application implements. Tiers are
**data-driven** — you add `free` / `paid` / `pro` / `enterprise` / `beta-tester` / … purely in config, with
no code change.

## Mental model — ports & adapters

```
                     freebyTech.Common.Web  (this library — generic)
   ┌───────────────────────────────────────────────────────────────────────┐
   │  IUsageMeter  ── UsageService (engine) ── UsageDecider (pure decisions) │
   │        │                                                                │
   │        ├── IUsageContextResolver  ← TierMappingContextResolver (default)│
   │        ├── IUsageStore            ← InMemoryUsageStore       (default)  │
   │        └── IUsageOverrideStore    ← NoOpUsageOverrideStore   (default)  │
   └──────────────▲───────────────▲──────────────────▲──────────────────────┘
                  │ implements     │ implements        │ replaces defaults
   ┌──────────────┴───────────────┴──────────────────┴──────────────────────┐
   │  YOUR APP (the adapters)                                                │
   │   • IPrivilegeChecker   — "does the caller hold privilege X?"  (REQUIRED)│
   │   • ICurrentUserAccessor — the caller's id + authenticated?    (REQUIRED)│
   │   • IUsageStore / IUsageOverrideStore — durable persistence    (optional)│
   └───────────────────────────────────────────────────────────────────────┘
```

The dependency only ever runs **your app → this library**. The library never references your domain types.

## Core concepts

| Concept | Meaning |
| --- | --- |
| **Bucket** | A category of usage you meter and limit, e.g. `agent`, `report`, `bulk-save`, `other`. You choose the names. |
| **Tier** | An opaque string (`free`, `paid`, `admin`, …) that selects a set of limits. Resolved from the caller's privileges via config. |
| **Dimension** | What a bucket counts: `calls`, `tokens`, `costCents`. A limit may be set on any subset; exceeding **any** set dimension trips the quota. |
| **Enforcement mode** | `Off` (not metered), `Monitor` (counted + evaluated, never blocked — logs would-be-blocks), `Enforce` (blocks at 100%). |
| **State** | `Ok` → `Approaching` (≥ warn %) → `Critical` (≥ critical %, or ≥100% while only monitoring) → `Blocked` (≥100% and enforcing). |
| **Reset** | Calendar month in **UTC** (00:00 UTC on the 1st). |
| **Fail mode** | If the store is unavailable: `Open` (allow — never block real work) or `Closed` (deny — cost-safe, used for `agent`). |

## Component reference

**Provided by the library:**

| Type | What it is |
| --- | --- |
| `IUsageMeter` | The facade your code calls: `Check(bucket)` before serving, `Record(usageRecord)` after. |
| `UsageService` | The engine implementing `IUsageMeter`. Composes the resolver, stores, and options. No domain knowledge. |
| `UsageDecider` | **Pure, static** decision logic (tier resolution, limit resolution, percent/state math, over-limit test). Side-effect-free and independently unit-testable. |
| `IUsageContextResolver` / `TierMappingContextResolver` | Resolves the caller's `UsageContext` (id + tier). The default walks `TierMappings` via `IPrivilegeChecker`. |
| `IUsageStore` / `InMemoryUsageStore` | Reads + increments the period counters. The in-memory default is self-testable and a dev fallback; replace it with a durable adapter in production. |
| `IUsageOverrideStore` / `NoOpUsageOverrideStore` | Per-user limit overrides (comps, credits, throttling). Default returns none. |
| `UsageBucketAttribute` | `[UsageBucket("agent")]` — tags a controller/action with its bucket. Plain attribute (works on MVC and minimal APIs). |
| `UsageMeteringMiddleware` / `UseUsageMetering()` | Reads the bucket off endpoint metadata, checks before the endpoint (→ `402` when enforced + exhausted), records one call after. |
| `AddUsageMetering(...)` | DI registration (binds options, registers engine + defaults). |
| `UsageMeteringOptions` (+ `TierMappingOption`, `TierOption`, `BucketLimitOption`) | The full config surface. |
| `UsageContext`, `UsageRecord`, `BucketCounters`, `BucketLimits`, `UsageCheckResult`, `DimensionUsage` | The data types passed around. |

**You must provide (no sensible generic default exists):**

| Port | Responsibility |
| --- | --- |
| `IPrivilegeChecker` | `bool Has(string privilege)` — does the current caller hold this privilege? |
| `ICurrentUserAccessor` | `bool IsAuthenticated` + `Guid UserId` — the current caller. |

**You should provide in production** (the in-memory/no-op defaults are for dev + tests):

| Port | Responsibility |
| --- | --- |
| `IUsageStore` | Durable, shared counters (SQL/Redis). |
| `IUsageOverrideStore` | Per-user override lookups. |

## Wiring it up

**1. Implement the two required ports.** Both typically wrap the same per-request user, so resolve it once:

```csharp
using freebyTech.Common.Web.Metering;

// A scoped holder resolved once per request (populate from your auth pipeline).
public sealed class CurrentUser
{
    public bool IsAuthenticated { get; set; }
    public Guid Id { get; set; }
    public HashSet<string> Privileges { get; set; } = new();
}

public sealed class AppCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly CurrentUser _u;
    public AppCurrentUserAccessor(CurrentUser u) => _u = u;
    public bool IsAuthenticated => _u.IsAuthenticated;
    public Guid UserId => _u.Id;
}

public sealed class AppPrivilegeChecker : IPrivilegeChecker
{
    private readonly CurrentUser _u;
    public AppPrivilegeChecker(CurrentUser u) => _u = u;
    public bool Has(string privilege) => _u.Privileges.Contains(privilege);
}
```

> **Tip:** resolve your user entity once per request (e.g. in middleware or a scoped service) and have both
> adapters read from it — don't re-hit the database inside `Has()`, which is called during every `Check`.

**2. Register the component and your adapters:**

```csharp
using freebyTech.Common.Web.Metering;

builder.Services.AddScoped<CurrentUser>();                       // your per-request user holder
builder.Services.AddScoped<ICurrentUserAccessor, AppCurrentUserAccessor>();
builder.Services.AddScoped<IPrivilegeChecker, AppPrivilegeChecker>();

// Binds options from the "UsageMetering" config section and registers the engine + defaults.
builder.Services.AddUsageMetering(builder.Configuration);

// In production, replace the in-memory store with your durable adapter:
// builder.Services.AddSingleton<IUsageStore, SqlUsageStore>();
// builder.Services.AddScoped<IUsageOverrideStore, SqlUsageOverrideStore>();
```

**3. Add the middleware** (after authentication, so the caller is known):

```csharp
app.UseAuthentication();
app.UseUsageMetering();   // checks + records any endpoint tagged with [UsageBucket]
```

**4. Tag the endpoints you want metered:**

```csharp
[ApiController]
[Route("agent")]
[UsageBucket("agent")]                 // whole controller counts against the "agent" bucket
public class AgentController : ControllerBase
{
    [HttpPost("transform")]
    public IActionResult Transform(...) => Ok(...);

    [HttpGet("status")]
    [UsageBucket("other")]             // an action can override the bucket
    public IActionResult Status() => Ok(...);
}
```

That's it — tagged endpoints are now checked before running (returning `402 Payment Required` with an
`X-Usage-Reset` header when a bucket is enforced and exhausted) and record one call after.

## Recording richer usage (tokens / cost)

The middleware records **one call** per request. For token/cost accounting (e.g. after an LLM call), inject
`IUsageMeter` and record explicitly:

```csharp
public class AgentService
{
    private readonly IUsageMeter _meter;
    public AgentService(IUsageMeter meter) => _meter = meter;

    public async Task<Result> RunAsync(Prompt p)
    {
        // Optional explicit pre-check (the middleware already gates the endpoint):
        var check = _meter.Check("agent");
        if (!check.Allowed)
            throw new QuotaExceededException(check.Tier, check.ResetAtUtc);

        var completion = await _llm.CompleteAsync(p);

        // Record the real token + cost usage for this call:
        _meter.Record(new UsageRecord("agent", Calls: 0, Tokens: completion.TotalTokens, CostCents: completion.CostCents));
        return completion.Result;
    }
}
```

> `Calls: 0` here because the middleware already counted the call; this record adds only tokens/cost.

## Driving a "usage meter" UI

`Check` returns everything a usage display needs — expose it via an endpoint (e.g. `GET /usage/me`):

```csharp
var check = _meter.Check("agent");
// check.Tier, check.State, check.ResetAtUtc
// check.Dimensions -> [{ Name:"calls", Used:37, Limit:50, Percent:74, State:Ok }, ...]
```

## Configuration (`appsettings.json`)

```jsonc
"UsageMetering": {
  "Enabled": true,
  "DefaultEnforcement": "Enforce",   // Off | Monitor | Enforce
  "WarnAtPercent": 80,
  "CriticalAtPercent": 95,
  "ResetCadence": "CalendarMonth",
  "ResetTimeZone": "UTC",
  "DefaultFailMode": "Open",         // Open | Closed
  "DefaultTier": "free",             // used when no TierMappings entry matches

  // Privilege → tier rules. "priority" is the tie-breaker when a caller holds more than one mapped
  // privilege (e.g. both paid-user AND admin-user): the highest priority wins, so that user resolves
  // to "admin". It is compared by number, NOT by list order, so reordering these entries changes
  // nothing. Leave gaps (100/30/20) so a new tier can slot between existing ones later without
  // renumbering. No mapping matches → DefaultTier.
  "TierMappings": [
    { "privilege": "admin-user", "tier": "admin", "priority": 100 },
    { "privilege": "pro-user",   "tier": "pro",   "priority": 30  },
    { "privilege": "paid-user",  "tier": "paid",  "priority": 20  }
  ],

  // Per-tier, per-bucket limits. Any dimension left out is unlimited. Enforcement / failMode /
  // warnAtPercent fall back to the globals above when omitted.
  "Tiers": {
    "free": {
      "buckets": {
        "agent":     { "enforcement": "Enforce", "callsPerMonth": 50,   "tokensPerMonth": 200000, "failMode": "Closed" },
        "report":    { "enforcement": "Monitor", "callsPerMonth": 300 },
        "bulk-save": { "enforcement": "Monitor", "callsPerMonth": 10000 }
      }
    },
    "paid": {
      "buckets": {
        "agent":  { "callsPerMonth": 1000, "tokensPerMonth": 5000000, "failMode": "Closed" },
        "report": { "callsPerMonth": 3000 }
      }
    },
    "admin": { "unlimited": true }   // bypasses every bucket
  }
}
```

**Adding a new tier is config-only:** add a `TierMappings` row (mapping a privilege to the tier name) and a
`Tiers` block with its limits — no code change.

**Resolution precedence for a limit:** `per-user override → tier's bucket entry → global default`.

## Enforcement semantics

- **`Off`** — the bucket is not counted or checked.
- **`Monitor`** — usage is counted and evaluated; going over the limit is **logged as a would-be-block** but
  the request is **allowed**. Use this to calibrate limits against real traffic before turning on enforcement.
- **`Enforce`** — going over the limit returns `402 Payment Required` (`X-Usage-Reset` header + a JSON body
  `{ error: "usage_quota_exceeded", bucket, tier, state, resetAt }`).
- Modes and limits are **hot-reloadable** via `IOptionsMonitor` — change config and it takes effect live,
  no redeploy.

## Testing

`UsageDecider` is pure, so decisions test without any host:

```csharp
Assert.Equal("admin", UsageDecider.ResolveTier(options, new FakePrivileges("paid-user", "admin-user")));

var limits = UsageDecider.ResolveLimits(options, tier: "free", bucket: "agent", ovr: null);
var (overLimit, state) = UsageDecider.Evaluate(new BucketCounters(Calls: 50, Tokens: 0, CostCents: 0), limits);
Assert.True(overLimit);
```

The engine tests against `InMemoryUsageStore` + fake ports (see `freebyTech.Common.Web.Tests/Metering`).

## Extending

- **Durable storage** — implement `IUsageStore` (SQL/Redis) and register it; the default in-memory store
  resets on restart and is per-process only. A common production shape is an in-memory buffered counter that
  flushes to SQL periodically (checks read the buffer; the durable row is the record of truth).
- **Custom tier logic** — replace `IUsageContextResolver` if you need resolution beyond privilege→tier.
- **Cross-language services** — non-.NET services (Python, Node) meter by calling small `/usage/check` and
  `/usage/record` HTTP endpoints your host exposes over this same engine.
