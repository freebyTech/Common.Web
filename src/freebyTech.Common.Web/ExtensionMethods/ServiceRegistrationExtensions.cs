using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using freebyTech.Common.Web.Logging.Interfaces;
using freebyTech.Common.Web.Logging;
using System.Reflection;

namespace freebyTech.Common.Web.ExtensionMethods
{
    public static class ServiceRegistrationExtensions
    {
        public static void AddApiLoggingServices(this IServiceCollection services, Assembly parentApplication, string applicationLogginId)
        {
            services.AddScoped<IApiRequestLogger, ApiRequestLogger>((ctx) =>
            {
                return new ApiRequestLogger(parentApplication, applicationLogginId);
            });
        }
    }
}
