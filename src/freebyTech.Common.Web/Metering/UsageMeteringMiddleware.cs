using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Meters requests that carry a <see cref="UsageBucketAttribute"/>: checks the caller's quota before the
/// endpoint runs (returning 402 when a quota is enforced and exhausted) and records one call after.
/// Requests without a bucket, or from unauthenticated callers, pass straight through.
/// </summary>
public sealed class UsageMeteringMiddleware
{
  private readonly RequestDelegate _next;

  public UsageMeteringMiddleware(RequestDelegate next) => _next = next;

  public async Task Invoke(
    HttpContext context,
    IUsageMeter meter,
    ICurrentUserAccessor user,
    IOptionsMonitor<UsageMeteringOptions> options)
  {
    var bucket = context.GetEndpoint()?.Metadata.GetMetadata<UsageBucketAttribute>()?.Bucket;

    if (!options.CurrentValue.Enabled || string.IsNullOrEmpty(bucket) || !user.IsAuthenticated)
    {
      await _next(context);
      return;
    }

    var check = meter.Check(bucket);
    if (!check.Allowed)
    {
      context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
      context.Response.Headers["X-Usage-Reset"] = check.ResetAtUtc.ToString("O");
      await context.Response.WriteAsJsonAsync(new
      {
        error = "usage_quota_exceeded",
        bucket = check.Bucket,
        tier = check.Tier,
        state = check.State.ToString(),
        resetAt = check.ResetAtUtc
      });
      return;
    }

    await _next(context);

    // Meter-everything: record one call for this bucket once the request has been served.
    meter.Record(new UsageRecord(bucket));
  }
}
