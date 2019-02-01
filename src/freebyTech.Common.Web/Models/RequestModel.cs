using Newtonsoft.Json;

namespace freebyTech.Common.Web.Models
{
    public class RequestModel
    {
        public string Method { get; set; }
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
