using System;

namespace freebyTech.Common.Web.Exceptions
{
    /// <summary>
    /// The exception that should be thrown whenever a known and common error has occurred, this will be caught and properly returned to the user
    /// in whatever message and status code is contained in this exception.
    /// </summary>
    public class WebRequestException : Exception
    {
        public WebRequestException(int responseStatusCode, string responseStatusDescription) : base(responseStatusDescription)
        {
            ResponseStatusCode = responseStatusCode;
        }
        public int ResponseStatusCode { get; set; }
    }
}
