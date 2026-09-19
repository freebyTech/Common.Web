namespace freebyTech.Common.Web.Metering;

/// <summary>
/// How a bucket's quota is enforced. Resolved per (tier × bucket), falling back to the global default.
/// </summary>
public enum UsageEnforcementMode
{
  /// <summary>Not metered at all — no counting, no checking.</summary>
  Off,

  /// <summary>Counted and evaluated, but never blocked — over-limit is logged as a would-be-block.</summary>
  Monitor,

  /// <summary>Counted, evaluated, and blocked once a limit is reached.</summary>
  Enforce
}

/// <summary>What happens when the metering backend itself is unavailable for a bucket.</summary>
public enum UsageFailMode
{
  /// <summary>Allow the request through (never block real work on a meter outage). Default for CRUD.</summary>
  Open,

  /// <summary>Deny the request (cost-safe). Used for the Agent bucket.</summary>
  Closed
}

/// <summary>
/// The user-facing usage state for a bucket. Ordered so <c>Max()</c> across dimensions yields the worst.
/// </summary>
public enum UsageState
{
  Ok = 0,
  Approaching = 1,   // ≥ WarnAtPercent
  Critical = 2,      // ≥ CriticalAtPercent (or ≥100% while only monitoring)
  Blocked = 3        // ≥100% and enforcing
}
