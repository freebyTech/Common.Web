namespace freebyTech.Common.Web.Metering;

/// <summary>
/// Tags a controller or action with the usage bucket it counts against (e.g. "agent", "report",
/// "bulk-save", "other"). The metering middleware reads it from endpoint metadata to check + record.
/// A plain attribute — no ASP.NET MVC dependency — so it can sit on minimal-API endpoints too.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UsageBucketAttribute : Attribute
{
  public string Bucket { get; }

  public UsageBucketAttribute(string bucket) => Bucket = bucket;
}
