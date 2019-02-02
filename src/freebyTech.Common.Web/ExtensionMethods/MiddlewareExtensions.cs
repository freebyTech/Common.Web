using freebyTech.Common.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
            app.EnableRequestBuffering();
            app.UseMiddleware<ApiRequestLoggingMiddleware>();
            // This needs to catch and log exceptions for the api middleware above so it should run after it.
            app.UseMiddleware<ApiExceptionMiddleware>();
        }
    }
}
