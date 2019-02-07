using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace freebyTech.Common.Web.Logging.Initializers.AppInsights
{
    internal class ContextInitializer : ITelemetryInitializer
    {
        private string _applicationName { get; set; }

        public ContextInitializer(string applicationName)
        {
            _applicationName = applicationName;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _applicationName;
        }
    }
}