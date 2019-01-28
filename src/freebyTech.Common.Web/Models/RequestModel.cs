using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace freebyTech.Common.Web.Logging.Interfaces
{
    public class RequestModel
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RemoteIpAddress { get; set; }
        public string Body { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
