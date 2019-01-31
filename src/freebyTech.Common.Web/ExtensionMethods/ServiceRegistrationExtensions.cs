using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Logging.Interfaces;
using freebyTech.Common.Web.Logging;

namespace freebyTech.Common.Web.ExtensionMethods
{
    /// <summary>
    /// Adds web based logging types to dependency injection, the specific ILogFrameworkAgent must be defined before this
    /// as that agent needs to be passed on to these logger types defined.
    /// </summary>
    public static class ServiceRegistrationExtensions
    {
        public static void AddApiLoggingServices(this IServiceCollection services, Assembly parentApplication, string applicationLogginId)
        {
            var serviceProvider = services.BuildServiceProvider();

            services.AddScoped<IApiRequestLogger, ApiRequestLogger>((ctx) =>
            {
                return new ApiRequestLogger(parentApplication, applicationLogginId, serviceProvider.GetService<ILogFrameworkAgent>());
            });
        }
    }
}
