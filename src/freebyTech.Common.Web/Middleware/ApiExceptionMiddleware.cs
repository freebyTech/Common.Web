using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using freebyTech.Common.Web.Models;

namespace freebyTech.Common.Web.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (WebException wex)
            {
                _logger.LogError($"WebException: {wex}");
                await HandleExceptionAsync(httpContext, wex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex}");
                await HandleExceptionAsync(httpContext, null);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return context.Response.WriteAsync(new JsonWebErrorResponse()
            {
                StatusCode = context.Response.StatusCode,
                Message = exception == null ? "Internal Server Error." : exception.Message
            }.ToString());
        }
    }
}
