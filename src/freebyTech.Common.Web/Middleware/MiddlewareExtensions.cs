using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace freebyTech.Common.Web.Middleware
{
    public static class MiddlewareExtensions
    {
        public static void ConfigureApiExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ApiExceptionMiddleware>();
        }
    }
}
