using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.ExtensionMethods;
using freebyTech.Common.Logging.Core;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Logging.Interfaces;
using freebyTech.Common.Web.Models;
using Microsoft.AspNetCore.Http;

namespace freebyTech.Common.Web.Logging.LoggerTypes
{
  public enum ApiLogVerbosity
  {
    /// <summary>Request + response bodies are captured (text content types only, capped at
    /// <see cref="ApiRequestLogger.MaxCapturedBodyChars"/>). Costs a full response buffer per request — local development only.</summary>
    LogEverything,
    /// <summary>Query string + status code, no bodies, no buffering. The production setting.</summary>
    LogMinimalRequest,
    /// <summary>Method, path, status, user, IP and timing only.</summary>
    NoRequestResponse
  }

  /// <summary>
  /// This is the logger used in the pipeline for API requests, it has request level scope so it is 
  /// </summary>
  public class ApiRequestLogger : LoggerBase, IApiRequestLogger
  {
    private ApiLogVerbosity _apiLogVerbosity;

    /// <summary>Upper bound on characters kept from a captured body; longer bodies are truncated with a marker.</summary>
    public const int MaxCapturedBodyChars = 64 * 1024;

    public ApiRequestLogger(Assembly parentApplication, string applicationLoggingId, ILogFrameworkAgent frameworkLogger, ApiLogVerbosity apiLogVerbosity) : base(parentApplication,
        LoggingMessageTypes.RequestResponse.ToString(), applicationLoggingId, frameworkLogger)
    {
      LogDurationInPushes = true;
      _apiLogVerbosity = apiLogVerbosity;
    }

    private long PipelineExecutionTime { get; set; }

    private double PipelineExecutionTimeMinutes { get; set; }

    public RequestModel Request { get; } = new RequestModel();

    public ResponseModel Response { get; } = new ResponseModel();

    public Exception CaughtException { get; protected set; } = null;

    public string ActivityId { get; set; }

    public string ParentActivityId { get; set; }

    public string UserId { get; set; }

    public long RequestExecutionTime { get; set; }

    public double RequestExecutionTimeMinutes { get; set; }

    public bool CapturesBodies => _apiLogVerbosity == ApiLogVerbosity.LogEverything;

    public async Task PushRequestAsync(HttpContext context)
    {
      if (Activity.Current != null)
      {
        if (!Activity.Current.Id.IsNullOrEmpty()) ActivityId = Activity.Current.Id;
        if (!Activity.Current.ParentId.IsNullOrEmpty()) ParentActivityId = Activity.Current.ParentId;
      }

      Request.QueryString = context.Request.QueryString.ToString();
      // TODO: May need to upgrade with this in mind: https://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
      Request.RemoteIpAddress = context.Request.HttpContext?.Connection?.RemoteIpAddress?.ToString();

      Request.Method = context.Request.Method;
      Request.Scheme = context.Request.Scheme;
      Request.Host = context.Request.Host.ToString();
      Request.Path = context.Request.Path;

      if (CapturesBodies)
      {
        Request.Body = await CaptureBodyAsync(context.Request.Body, context.Request.ContentType);
        if (context.Request.Body.CanSeek)
        {
          context.Request.Body.Position = 0;
        }
      }
    }

    public void PushResponse(HttpContext context)
    {
      if (context.User != null)
      {
        UserId = context.User.Identity?.Name;
      }

      Response.StatusCode = context.Response.StatusCode;
    }

    public async Task PushResponseAsync(HttpContext context, MemoryStream tempResponseBody)
    {
      PushResponse(context);

      // Status code of 204 is No Content.
      if (CapturesBodies && context.Response.StatusCode != 204 && tempResponseBody != null)
      {
        // Defensive: the middleware rewinds too, but a caller must never be able to leave the
        // buffer at its end and have the client receive nothing.
        tempResponseBody.Position = 0;
        Response.Body = await CaptureBodyAsync(tempResponseBody, context.Response.ContentType);
        tempResponseBody.Position = 0;
      }
    }

    /// <summary>
    /// Reads a body for logging: text content types only, truncated at <see cref="MaxCapturedBodyChars"/>.
    /// Binary payloads (uploads, downloads) are summarised rather than captured.
    /// </summary>
    private static async Task<string> CaptureBodyAsync(Stream body, string contentType)
    {
      if (body == null) return null;
      if (!IsTextContentType(contentType))
      {
        var length = body.CanSeek ? body.Length.ToString() : "?";
        return $"[{contentType}: {length} bytes not captured]";
      }

      var buffer = new char[MaxCapturedBodyChars];
      var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
      var total = 0;
      while (total < buffer.Length)
      {
        var read = await reader.ReadAsync(buffer, total, buffer.Length - total);
        if (read == 0) break;
        total += read;
      }
      var text = new string(buffer, 0, total);
      if (total == buffer.Length && reader.Peek() >= 0)
      {
        text += $"...[truncated at {MaxCapturedBodyChars} chars]";
      }
      return text;
    }

    public static bool IsTextContentType(string contentType)
    {
      // No content type = no body worth worrying about (GETs, 204s); let the reader see it.
      if (contentType.IsNullOrEmpty()) return true;
      var ct = contentType.Split(';')[0].Trim().ToLowerInvariant();
      return ct.StartsWith("text/")
        || ct == "application/json"
        || ct == "application/xml"
        || ct == "application/x-www-form-urlencoded"
        || ct.EndsWith("+json")
        || ct.EndsWith("+xml");
    }

    public void PushCaughtException(Exception exception)
    {
      CaughtException = exception;
    }

    public void LogCompletedRequest()
    {
      var duration = GetDuration();
      RequestExecutionTime = duration.Ms;
      RequestExecutionTimeMinutes = duration.Minutes;

      if (CaughtException != null)
      {
        var errorString = $"Api Request {Request.Method} to {Request.Path} Failed with {CaughtException?.GetType()}&NL{CaughtException.Source} : {CaughtException.Message}&NL";

        if (CaughtException.TargetSite != null)
        {
          errorString += ($"Target Site: {CaughtException.TargetSite}&NL");
        }
        errorString += $"{CaughtException.StackTrace}&NL";
        if (CaughtException.InnerException != null)
        {
          errorString += $"Inner Exception: {CaughtException.InnerException?.GetType()} : {CaughtException.InnerException.Source} : {CaughtException.InnerException.Message}&NL";
          errorString += $"{CaughtException.InnerException.StackTrace}";
        }
        errorString = errorString.Replace("&NL", System.Environment.NewLine);
        LogError(errorString, CaughtException);
      }
      else
      {
        LogInfo($"Api Request {Request.Method} to {Request.Path} Completed");
      }
    }

    #region Override Methods

    protected sealed override void SetCustomProperties(Dictionary<string, object> customProperties)
    {
      if (_apiLogVerbosity == ApiLogVerbosity.LogEverything)
      {
        customProperties["request"] = Request.ToString();
        customProperties["response"] = Response.ToString();
      }
      else if (_apiLogVerbosity == ApiLogVerbosity.LogMinimalRequest)
      {
        // Query string + status only — bodies are never captured at this verbosity.
        customProperties["requestQueryString"] = Request.QueryString;
        customProperties["responseStatusCode"] = Response.StatusCode;
      }

      customProperties["remoteIpAddress"] = Request.RemoteIpAddress;
      customProperties["requestExecutionTimeMS"] = RequestExecutionTime;
      customProperties["requestExecutionTimeMinutes"] = RequestExecutionTimeMinutes;

      if (!ActivityId.IsNullOrEmpty())
      {
        customProperties["activityId"] = ActivityId;
      }
      if (!ParentActivityId.IsNullOrEmpty())
      {
        customProperties["parentActivityId"] = ParentActivityId;
      }
      if (!UserId.IsNullOrEmpty())
      {
        customProperties["userId"] = UserId;
      }
      SetDerivedClassCustomProperties(customProperties);
    }

    /// <summary>
    /// If implementing a logger on top of this logger you should set your custom properties here rather 
    /// than in SetCustomProperties which is already being used by this class.
    /// </summary>
    protected virtual void SetDerivedClassCustomProperties(Dictionary<string, object> customProperties)
    {

    }

    #endregion
  }
}
