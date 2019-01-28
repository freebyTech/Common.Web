using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace freebyTech.Common.Web.Logging.Interfaces
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
