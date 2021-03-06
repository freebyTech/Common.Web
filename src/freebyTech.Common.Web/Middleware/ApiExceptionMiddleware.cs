﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using freebyTech.Common.Web.Exceptions;
using freebyTech.Common.Web.Logging.Interfaces;
using freebyTech.Common.Web.Models;

namespace freebyTech.Common.Web.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        
        public ApiExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IApiRequestLogger logger)
        {
            try
            {
                await _next(httpContext);
            }
            catch (WebRequestException wre)
            {
                logger.PushCaughtException(wre);

                await HandleExceptionAsync(httpContext, wre.ResponseStatusCode, wre.Message);
            }
            catch (Exception ex)
            {
                logger.PushCaughtException(ex);
                await HandleExceptionAsync(httpContext, (int)HttpStatusCode.InternalServerError, "Internal Server Error");
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(new JsonWebErrorResponse()
            {
                StatusCode = context.Response.StatusCode,
                Message = message
            }.ToString());
        }
    }
}
