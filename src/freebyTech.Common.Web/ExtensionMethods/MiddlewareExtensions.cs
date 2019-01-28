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
        public static void ConfigureApiExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiExceptionMiddleware>();
        }

        public static void ConfigureApiRequestLoggingMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiRequestLoggingMiddleware>();
        }

        public static void ConfigureStandardApiMiddlware(this IApplicationBuilder app)
        {
            app.EnableRequestBuffering();
            app.UseMiddleware<ApiExceptionMiddleware>();
            app.UseMiddleware<ApiRequestLoggingMiddleware>();
        }
    }
}
