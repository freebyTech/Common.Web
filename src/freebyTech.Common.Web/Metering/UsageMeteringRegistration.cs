using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace freebyTech.Common.Web.Metering;

/// <summary>
/// DI + pipeline registration for the metering component. The host still MUST register the two identity
/// ports (<see cref="IPrivilegeChecker"/> and <see cref="ICurrentUserAccessor"/>) — there is no sensible
/// generic default — and typically replaces <see cref="IUsageStore"/> / <see cref="IUsageOverrideStore"/>
/// with durable adapters.
/// </summary>
public static class UsageMeteringRegistration
{
  /// <summary>Register the engine, binding options from the "UsageMetering" config section.</summary>
  public static IServiceCollection AddUsageMetering(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<UsageMeteringOptions>(configuration.GetSection(UsageMeteringOptions.SectionName));
    return services.AddUsageMeteringCore();
  }

  /// <summary>Register the engine with options configured in code (handy for tests / non-config hosts).</summary>
  public static IServiceCollection AddUsageMetering(this IServiceCollection services, Action<UsageMeteringOptions> configure)
  {
    services.Configure(configure);
    return services.AddUsageMeteringCore();
  }

  private static IServiceCollection AddUsageMeteringCore(this IServiceCollection services)
  {
    // Defaults registered only if the host hasn't supplied its own (TryAdd*).
    services.TryAddScoped<IUsageContextResolver, TierMappingContextResolver>();
    services.TryAddSingleton<IUsageStore, InMemoryUsageStore>();
    services.TryAddSingleton<IUsageOverrideStore, NoOpUsageOverrideStore>();
    services.TryAddScoped<IUsageMeter, UsageService>();
    return services;
  }

  /// <summary>Add the metering middleware to the request pipeline (records + enforces per bucket).</summary>
  public static IApplicationBuilder UseUsageMetering(this IApplicationBuilder app)
    => app.UseMiddleware<UsageMeteringMiddleware>();
}
