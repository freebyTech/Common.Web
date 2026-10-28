using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.Logging.Core;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Logging.LoggerTypes;
using freebyTech.Common.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace freebyTech.Common.Web.Tests.Middleware
{
  /// <summary>
  /// Guards the one invariant that matters most: whatever the logging verbosity, the client
  /// receives the full response. (NoRequestResponse once sent 0 bytes because the buffered
  /// response was never rewound before being copied to the real stream.)
  /// </summary>
  public class ApiRequestLoggingMiddlewareTester
  {
    private sealed class CapturingAgent : ILogFrameworkAgent
    {
      public readonly List<GenericLogEventInfo> Events = new List<GenericLogEventInfo>();
      public void Log(string loggerName, GenericLogEventInfo logEvent) => Events.Add(logEvent);
    }

    private static (ApiRequestLogger logger, CapturingAgent agent) BuildLogger(ApiLogVerbosity verbosity)
    {
      var agent = new CapturingAgent();
      var logger = new ApiRequestLogger(typeof(ApiRequestLoggingMiddlewareTester).Assembly, "tests", agent, verbosity);
      return (logger, agent);
    }

    private static DefaultHttpContext BuildContext(string requestBody = "{\"a\":1}", string requestContentType = "application/json")
    {
      var context = new DefaultHttpContext();
      context.Request.Method = "PUT";
      context.Request.Path = "/tiv/budgets";
      context.Request.QueryString = new QueryString("?year=2026");
      context.Request.ContentType = requestContentType;
      context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
      context.Response.Body = new MemoryStream();
      return context;
    }

    private static RequestDelegate Writes(string text, int statusCode = 200, string contentType = "application/json")
    {
      return async ctx =>
      {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = contentType;
        var bytes = Encoding.UTF8.GetBytes(text);
        await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
      };
    }

    private static string ClientReceived(HttpContext context)
    {
      var stream = (MemoryStream)context.Response.Body;
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Theory]
    [InlineData(ApiLogVerbosity.LogEverything)]
    [InlineData(ApiLogVerbosity.LogMinimalRequest)]
    [InlineData(ApiLogVerbosity.NoRequestResponse)]
    public async Task Client_receives_the_full_response_at_every_verbosity(ApiLogVerbosity verbosity)
    {
      var (logger, _) = BuildLogger(verbosity);
      var context = BuildContext();
      var originalStream = context.Response.Body;

      await new ApiRequestLoggingMiddleware(Writes("{\"ok\":true}")).Invoke(context, logger);

      Assert.Equal("{\"ok\":true}", ClientReceived(context));
      Assert.Same(originalStream, context.Response.Body);
      Assert.Equal(200, logger.Response.StatusCode);
    }

    [Fact]
    public async Task LogEverything_captures_bodies_and_attaches_them_to_the_event()
    {
      var (logger, agent) = BuildLogger(ApiLogVerbosity.LogEverything);
      var context = BuildContext();

      await new ApiRequestLoggingMiddleware(Writes("{\"ok\":true}")).Invoke(context, logger);

      Assert.True(logger.CapturesBodies);
      Assert.Equal("{\"a\":1}", logger.Request.Body);
      Assert.Equal("{\"ok\":true}", logger.Response.Body);
      // The request stream is rewound so the controller can still read it.
      Assert.Equal(0, context.Request.Body.Position);
      var completed = Assert.Single(agent.Events);
      // request/response are JSON-serialised models, so the body appears escaped inside them.
      Assert.Contains("{\\\"a\\\":1}", (string)completed.ExtraProperties["request"]);
      Assert.Contains("{\\\"ok\\\":true}", (string)completed.ExtraProperties["response"]);
    }

    [Fact]
    public async Task LogMinimalRequest_logs_query_and_status_but_never_a_body()
    {
      var (logger, agent) = BuildLogger(ApiLogVerbosity.LogMinimalRequest);
      var context = BuildContext();

      await new ApiRequestLoggingMiddleware(Writes("{\"ok\":true}")).Invoke(context, logger);

      Assert.False(logger.CapturesBodies);
      Assert.Null(logger.Request.Body);
      Assert.Null(logger.Response.Body);
      // The request stream was never consumed.
      Assert.Equal(0, context.Request.Body.Position);
      var completed = Assert.Single(agent.Events);
      Assert.Equal("?year=2026", completed.ExtraProperties["requestQueryString"]);
      Assert.Equal(200, completed.ExtraProperties["responseStatusCode"]);
      Assert.False(completed.ExtraProperties.ContainsKey("requestBody"));
      Assert.False(completed.ExtraProperties.ContainsKey("responseBody"));
      Assert.False(completed.ExtraProperties.ContainsKey("request"));
      Assert.False(completed.ExtraProperties.ContainsKey("response"));
    }

    [Fact]
    public async Task NoRequestResponse_logs_neither_query_nor_bodies()
    {
      var (logger, agent) = BuildLogger(ApiLogVerbosity.NoRequestResponse);
      var context = BuildContext();

      await new ApiRequestLoggingMiddleware(Writes("{\"ok\":true}")).Invoke(context, logger);

      Assert.Null(logger.Request.Body);
      Assert.Null(logger.Response.Body);
      var completed = Assert.Single(agent.Events);
      foreach (var key in new[] { "requestQueryString", "responseStatusCode", "requestBody", "responseBody", "request", "response" })
      {
        Assert.False(completed.ExtraProperties.ContainsKey(key), key);
      }
      // The always-present context still rides along.
      Assert.True(completed.ExtraProperties.ContainsKey("requestExecutionTimeMS"));
    }

    [Fact]
    public async Task A_204_is_passed_through_without_a_captured_body()
    {
      var (logger, _) = BuildLogger(ApiLogVerbosity.LogEverything);
      var context = BuildContext();

      await new ApiRequestLoggingMiddleware(Writes("", statusCode: 204)).Invoke(context, logger);

      Assert.Equal("", ClientReceived(context));
      Assert.Equal(204, logger.Response.StatusCode);
      Assert.Null(logger.Response.Body);
    }

    [Fact]
    public async Task Binary_bodies_are_summarised_not_captured()
    {
      var (logger, _) = BuildLogger(ApiLogVerbosity.LogEverything);
      var context = BuildContext(requestBody: "PNG...", requestContentType: "image/png");

      await new ApiRequestLoggingMiddleware(Writes("BLOB", contentType: "application/octet-stream")).Invoke(context, logger);

      Assert.Equal("BLOB", ClientReceived(context));
      Assert.StartsWith("[image/png:", logger.Request.Body);
      Assert.StartsWith("[application/octet-stream:", logger.Response.Body);
    }

    [Fact]
    public async Task Captured_bodies_are_truncated_but_the_client_gets_everything()
    {
      var (logger, _) = BuildLogger(ApiLogVerbosity.LogEverything);
      var context = BuildContext();
      var huge = new string('x', ApiRequestLogger.MaxCapturedBodyChars + 1000);

      await new ApiRequestLoggingMiddleware(Writes(huge)).Invoke(context, logger);

      Assert.Equal(huge, ClientReceived(context));
      Assert.StartsWith(new string('x', ApiRequestLogger.MaxCapturedBodyChars), logger.Response.Body);
      Assert.Contains("[truncated at", logger.Response.Body);
    }

    [Theory]
    [InlineData(ApiLogVerbosity.LogEverything)]
    [InlineData(ApiLogVerbosity.LogMinimalRequest)]
    public async Task An_exception_from_the_pipeline_still_completes_the_log_and_restores_the_stream(ApiLogVerbosity verbosity)
    {
      var (logger, agent) = BuildLogger(verbosity);
      var context = BuildContext();
      var originalStream = context.Response.Body;
      RequestDelegate boom = _ => throw new InvalidOperationException("boom");

      await Assert.ThrowsAsync<InvalidOperationException>(() => new ApiRequestLoggingMiddleware(boom).Invoke(context, logger));

      Assert.Same(originalStream, context.Response.Body);
      Assert.Single(agent.Events);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("application/json; charset=utf-8", true)]
    [InlineData("application/problem+json", true)]
    [InlineData("text/plain", true)]
    [InlineData("application/x-www-form-urlencoded", true)]
    [InlineData("image/png", false)]
    [InlineData("application/octet-stream", false)]
    [InlineData("multipart/form-data; boundary=x", false)]
    public void Text_content_types_are_recognised(string contentType, bool expected)
    {
      Assert.Equal(expected, ApiRequestLogger.IsTextContentType(contentType));
    }
  }
}
