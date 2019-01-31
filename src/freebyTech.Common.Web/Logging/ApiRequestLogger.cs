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
using NLog;

namespace freebyTech.Common.Web.Logging
{
    /// <summary>
    /// This is the logger used in the pipeline for API requests, it has request level scope so it is 
    /// </summary>
    public class ApiRequestLogger : LoggerBase, IApiRequestLogger
    {
        public ApiRequestLogger(Assembly parentApplication, string applicationLoggingId, ILogFrameworkAgent frameworkLogger) : base(parentApplication,
            LoggingMessageTypes.RequestResponse.ToString(), applicationLoggingId, frameworkLogger) 
        {
            LogDurationInPushes = true;
        }

        private long ExecutionTime { get; set; }
        
        private double ExecutionTimeMinutes { get; set; }

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

        public void LogCompletedRequest()
        {
            var duration = GetDuration();
            ExecutionTime = duration.Ms;
            ExecutionTimeMinutes = duration.Minutes;

            if (CaugthException != null)
            {
                LogError("Request {Request.Verb} {Request.Path} Completed with Exception", CaugthException);
            }
            else
            {
                LogInfo("Request {Request.Verb} {Request.Path}  Complete");
            }
        }
        
        #region Override Methods

        protected sealed override void SetCustomProperties(Dictionary<string, object> customProperties)
        {
            customProperties["request"] = Request.ToString();
            customProperties["response"] = Response.ToString();
            customProperties["executionTimeMS"] = ExecutionTime;
            customProperties["executionTimeMinutes"] = ExecutionTimeMinutes;
            if(!Activity.Current.Id.IsNullOrEmpty()) customProperties["activityId"] = Activity.Current.Id;
            if(!Activity.Current.ParentId.IsNullOrEmpty()) customProperties["parentActivityId"] = Activity.Current.ParentId;
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
