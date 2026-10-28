using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Models;
using Microsoft.AspNetCore.Http;

namespace freebyTech.Common.Web.Logging.Interfaces
{
    public interface IApiRequestLogger : IBasicLogger
    {
        #region Request Response Properties
        RequestModel Request { get; }
        ResponseModel Response { get; }
        Exception CaughtException { get; }
        string ActivityId { get; set; }
        string ParentActivityId { get; set; }
        string UserId { get; set; }
        long RequestExecutionTime { get; set; }
        double RequestExecutionTimeMinutes { get; set; }

        /// <summary>
        /// True only when the configured verbosity records request/response bodies. The logging middleware
        /// uses this to decide whether the response must be diverted through a memory buffer at all.
        /// </summary>
        bool CapturesBodies { get; }

        #endregion

        /// <summary>
        /// Pushes all the information of the request into the log queue.
        /// </summary>
        /// <param name="mb"></param>
        Task PushRequestAsync(HttpContext context);

        /// <summary>
        /// Pushes a caught exception into the log queue.
        /// </summary>
        /// <param name="exception"></param>
        void PushCaughtException(Exception exception);

        /// <summary>
        /// Pushese the response into the log queue.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseBody"></param>
        /// <summary>
        /// Records the response status code and user WITHOUT a body — the path taken when
        /// <see cref="CapturesBodies"/> is false and the response streamed straight to the client.
        /// </summary>
        void PushResponse(HttpContext context);

        Task PushResponseAsync(HttpContext context, MemoryStream responseBody);

        void LogCompletedRequest();
    }
}
