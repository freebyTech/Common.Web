﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Logging.Interfaces;
using Microsoft.AspNetCore.Http;

namespace freebyTech.Common.Web.Middleware
{
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

            var originalBodyStream = context.Response.Body;

            try
            {
                using (var tempResponseBody = new MemoryStream())
                {
                    context.Response.Body = tempResponseBody;

                    await _next(context);

                    await logger.PushResponseAsync(context, tempResponseBody);
                    await tempResponseBody.CopyToAsync(originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
            logger.LogRequestComplete();
        }
    }
}
