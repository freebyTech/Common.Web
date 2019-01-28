using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.ExtensionMethods;
using freebyTech.Common.Logging;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Logging.Interfaces;
using Microsoft.AspNetCore.Http;
using NLog;

namespace freebyTech.Common.Web.Logging
{
    /// <summary>
    /// This is the logger used in the pipeline for API requests, it has request level scope so it is 
    /// </summary>
    public class ApiRequestLogger : LoggingBase, IApiRequestLogger
    {
        public ApiRequestLogger(Assembly parentApplication, string applicationLoggingId) : base(parentApplication,
            LoggingMessageTypes.RequestResponse.ToString(), applicationLoggingId)
        {
            // Construction of this logger is about the earliest we can get in the request lifetime.
            SW = Stopwatch.StartNew();
        }
        
        public long ExecutionTime { get; protected set; }
        public double ExecutionTimeMinutes { get; protected set; }

        public Stopwatch SW { get;  }

        public Guid UniqueReguestId { get; } = Guid.NewGuid();

        public RequestModel Request { get; } = new RequestModel();

        public ResponseModel Response { get; } = new ResponseModel();

        public Exception CaugthException { get; protected set;  } = null;

        public async Task PushRequestAsync(HttpContext context)
        {
            Request.Scheme = context.Request.Scheme;
            Request.Host = context.Request.Host.ToString();
            Request.Path = context.Request.Path;
            Request.QueryString = context.Request.QueryString.ToString();
            // TODO: May need to upgrade with this in mind: https://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
            Request.RemoteIpAddress = context.Request.HttpContext?.Connection?.RemoteIpAddress.ToString();

            Request.Body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        public async Task PushResponseAsync(HttpContext context, MemoryStream tempResponseBody)
        {
            Response.StatusCode = context.Response.StatusCode;
            // Status code of 204 is No Content.
            if (context.Response.StatusCode != 204)
            {
                tempResponseBody.Position = 0;
                Response.Body = await new StreamReader(tempResponseBody).ReadToEndAsync();
                tempResponseBody.Position = 0;
            }
        }

        public void PushCaughtException(Exception exception)
        {
            CaugthException = exception;
        }

        public void LogRequestComplete()
        {
            SW.Stop();

            ExecutionTime = SW.ElapsedMilliseconds;
            ExecutionTimeMinutes = SW.Elapsed.TotalMinutes;

            if (CaugthException != null)
            {
                LogError("Request Completed with Exception");
            }
            else
            {
                LogInfo("Request Complete");
            }
        }
        
        public void PushInfoWithTime(string message)
        {
            PushInfo(SW.AddTimeToMessage(message));
        }

        public void PushInfoWithTime(string key, string value)
        {
            PushInfo(key, SW.AddTimeToMessage(value));
        }

        public void PushWarnWithTime(string message)
        {
            PushWarn(SW.AddTimeToMessage(message));
        }

        public void PushWarnWithTime(string key, string value)
        {
            PushWarn(key, SW.AddTimeToMessage(value));
        }

        public void PushErrorWithTime(string message)
        {
            PushError(SW.AddTimeToMessage(message));
        }

        public void PushErrorWithTime(string key, string value)
        {
            PushError(key, SW.AddTimeToMessage(value));
        }

        #region Override Methods

        protected sealed override void SetCustomProperties(LogEventInfo logEvent)
        {
            logEvent.Properties["request"] = Request.ToString();
            logEvent.Properties["response"] = Response.ToString();
            logEvent.Properties["executionTimeMS"] = ExecutionTime;
            logEvent.Properties["executionTimeMinutes"] = ExecutionTimeMinutes;
            // TODO: Needs to be more verbose.
            logEvent.Properties["caughtException"] = CaugthException.ToString();
            SetDerivedClassCustomProperties(logEvent);
        }

        /// <summary>
        /// If implementing a logger on top of this logger you should set your custom properties here rather 
        /// than in SetCustomProperties which is already being used by this class.
        /// </summary>
        /// <param name="logEvent"></param>
        protected virtual void SetDerivedClassCustomProperties(LogEventInfo logEvent)
        {

        }

        #endregion
    }
}
