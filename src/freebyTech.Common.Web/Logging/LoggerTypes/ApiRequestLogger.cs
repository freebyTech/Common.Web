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

namespace freebyTech.Common.Web.Logging.LoggerTypes
{
    public enum ApiLogVerbosity
    {
        LogEverything,
        LogMinimalRequest,
        NoRequestResponse
    }

    /// <summary>
    /// This is the logger used in the pipeline for API requests, it has request level scope so it is 
    /// </summary>
    public class ApiRequestLogger : LoggerBase, IApiRequestLogger
    {
        private ApiLogVerbosity _apiLogVerbosity;

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

        public Exception CaugthException { get; protected set;  } = null;

        public string ActivityId { get; set; }

        public string ParentActivityId { get; set; }

        public string UserId { get; set; }

        public long RequestExecutionTime { get; set; }

        public double RequestExecutionTimeMinutes { get; set; }

        public async Task PushRequestAsync(HttpContext context)
        {
            if(Activity.Current != null) {
                if(!Activity.Current.Id.IsNullOrEmpty()) ActivityId = Activity.Current.Id;
                if(!Activity.Current.ParentId.IsNullOrEmpty()) ParentActivityId = Activity.Current.ParentId;
            }
            
            Request.QueryString = context.Request.QueryString.ToString();
            // TODO: May need to upgrade with this in mind: https://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
            Request.RemoteIpAddress = context.Request.HttpContext?.Connection?.RemoteIpAddress.ToString();

            Request.Method = context.Request.Method;
            Request.Scheme = context.Request.Scheme;
            Request.Host = context.Request.Host.ToString();
            Request.Path = context.Request.Path;

            if(_apiLogVerbosity != ApiLogVerbosity.NoRequestResponse) 
            {                
                Request.Body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;
            }            
        }

        public async Task PushResponseAsync(HttpContext context, MemoryStream tempResponseBody)
        {
            if(context.User != null) {
                UserId = context.User.Identity?.Name;
            }
             
            Response.StatusCode = context.Response.StatusCode;
            // Status code of 204 is No Content.
            if (context.Response.StatusCode != 204 && _apiLogVerbosity != ApiLogVerbosity.NoRequestResponse)
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
            RequestExecutionTime = duration.Ms;
            RequestExecutionTimeMinutes = duration.Minutes;

            if (CaugthException != null)
            {
                LogError($"Request {Request.Method} to {Request.Path} Completed with Exception", CaugthException);
            }
            else
            {
                LogInfo($"Request {Request.Method} to {Request.Path} Complete");
            }
        }
        
        #region Override Methods

        protected sealed override void SetCustomProperties(Dictionary<string, object> customProperties)
        {
            if(_apiLogVerbosity == ApiLogVerbosity.LogEverything)
            {
                customProperties["request"] = Request.ToString();
                customProperties["response"] = Response.ToString();
            }
            else if(_apiLogVerbosity == ApiLogVerbosity.LogMinimalRequest)
            {
                customProperties["requestQueryString"] = Request.QueryString;
                customProperties["requestBody"] = Request.Body;
                customProperties["responseStatusCode"] = Response.StatusCode;
                customProperties["responseBody"] = Response.Body;
            }

            customProperties["remoteIpAddress"] = Request.RemoteIpAddress;
            customProperties["requestExecutionTimeMS"] = RequestExecutionTime;
            customProperties["requestExecutionTimeMinutes"] = RequestExecutionTimeMinutes;

            if(!ActivityId.IsNullOrEmpty()) {
                customProperties["activityId"] = ActivityId;
            }
            if(!ParentActivityId.IsNullOrEmpty()) {
                customProperties["parentActivityId"] = ParentActivityId;
            }
            if(!UserId.IsNullOrEmpty()) {
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
