using System;
using System.IO;
using System.Threading.Tasks;
using freebyTech.Common.Web.Logging.Interfaces;
using Microsoft.AspNetCore.Http;

namespace freebyTech.Common.Web.Middleware
{
    /// <summary>
    /// Logs one event per API request through <see cref="IApiRequestLogger"/>.
    /// The response is only diverted through a memory buffer when the logger's verbosity actually
    /// captures bodies (<see cref="IApiRequestLogger.CapturesBodies"/>); every other verbosity lets the
    /// response stream straight to the client. The buffer rewinds live HERE, next to the copy, so no
    /// verbosity can ever skip them and send an empty response.
    /// </summary>
    public class ApiRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiRequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IApiRequestLogger logger)
        {
            await logger.PushRequestAsync(context);

            if (!logger.CapturesBodies)
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    logger.PushResponse(context);
                    logger.LogCompletedRequest();
                }
                return;
            }

            var originalBodyStream = context.Response.Body;

            try
            {
                using (var tempResponseBody = new MemoryStream())
                {
                    context.Response.Body = tempResponseBody;

                    await _next(context);

                    tempResponseBody.Position = 0;
                    await logger.PushResponseAsync(context, tempResponseBody);
                    tempResponseBody.Position = 0;
                    await tempResponseBody.CopyToAsync(originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                logger.LogCompletedRequest();
            }
        }
    }
}
