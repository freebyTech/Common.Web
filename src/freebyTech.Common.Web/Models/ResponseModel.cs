using Newtonsoft.Json;

namespace freebyTech.Common.Web.Models
{
    public class ResponseModel
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
