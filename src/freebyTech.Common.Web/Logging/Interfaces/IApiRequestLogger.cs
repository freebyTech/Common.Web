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

        Guid UniqueReguestId { get; }
        RequestModel Request { get; }
        ResponseModel Response { get; }
        Exception CaugthException { get; }

        #endregion

        Stopwatch SW { get; }

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
        Task PushResponseAsync(HttpContext context, MemoryStream responseBody);

        void PushInfoWithTime(string message);
        void PushInfoWithTime(string key, string value);

        void PushWarnWithTime(string message);
        void PushWarnWithTime(string key, string value);
        
        void PushErrorWithTime(string message);
        void PushErrorWithTime(string key, string value);

        void LogRequestComplete();
    }
}
