using freebyTech.Common.Web.Logging.Interfaces;
using freebyTech.Common.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace freebyTech.Common.Web.ExtensionMethods
{
    public static class MiddlewareExtensions
    {
        public static void EnableRequestBuffering(this IApplicationBuilder app)
        {
            app.Use(async (context, next) => {
                context.Request.EnableBuffering();
                await next();
            });
        }
        /// <summary>
        /// Enables request-body buffering only when the scoped <see cref="IApiRequestLogger"/> will actually
        /// capture bodies (<see cref="IApiRequestLogger.CapturesBodies"/>), so production verbosities do not
        /// pay for buffering nobody reads.
        /// </summary>
        public static void EnableRequestBufferingWhenBodiesAreLogged(this IApplicationBuilder app)
        {
            app.Use(async (context, next) => {
                var logger = context.RequestServices.GetService<IApiRequestLogger>();
                if (logger?.CapturesBodies == true)
                {
                    context.Request.EnableBuffering();
                }
                await next();
            });
        }

        public static void UseApiExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiExceptionMiddleware>();
        }

        public static void UseApiRequestLoggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiRequestLoggingMiddleware>();
        }

        public static void UseStandardApiMiddleware(this IApplicationBuilder app)
        {
            app.EnableRequestBufferingWhenBodiesAreLogged();
            app.UseMiddleware<ApiRequestLoggingMiddleware>();
            // This needs to catch and log exceptions for the api middleware above so it should run after it.
            app.UseMiddleware<ApiExceptionMiddleware>();
        }
    }
}
