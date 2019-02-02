using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using freebyTech.Common.Logging.Interfaces;
using freebyTech.Common.Web.Models;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Serilog.Events;
using Serilog.ExtensionMethods;

namespace freebyTech.Common.Web.Logging.Converters
{
    public static class LogEventConverters {
        public static ITelemetry ConvertLogEventsToEventTelemetryWithContext(LogEvent logEvent, IFormatProvider formatProvider)
        {            
            return (ConvertToEventTelemetry(logEvent, formatProvider).UpdateWithContextTelemitryProperties(logEvent));
        }

        public static ITelemetry ConvertToEventTelemetry(LogEvent logEvent, IFormatProvider formatProvider)
        {
            // first create a default EventTelemetry using the sink's default logic
            // .. but without the log level, and (rendered) message (template) included in the Properties
            if (logEvent.Exception != null) {
                // Exception telemetry
                return logEvent.ToDefaultExceptionTelemetry(
                formatProvider,
                includeLogLevelAsProperty: true,
                includeRenderedMessageAsProperty: false,
                includeMessageTemplateAsProperty: false);
            }
            else {
                // default telemetry
                return logEvent.ToDefaultEventTelemetry(
                formatProvider,
                includeLogLevelAsProperty: true,
                includeRenderedMessageAsProperty: false,
                includeMessageTemplateAsProperty: false);
            }
        }

        public static ITelemetry ConvertLogEventsToTraceTelemetryWithContext(LogEvent logEvent, IFormatProvider formatProvider)
        {
            return (ConvertToTraceTelemetry(logEvent, formatProvider).UpdateWithContextTelemitryProperties(logEvent));
        }

        public static ITelemetry ConvertToTraceTelemetry(LogEvent logEvent, IFormatProvider formatProvider)
        {
            // first create a default EventTelemetry using the sink's default logic
            // .. but without the log level, and (rendered) message (template) included in the Properties
            if (logEvent.Exception != null)
            {
                // Exception telemetry
                return logEvent.ToDefaultExceptionTelemetry(
                    formatProvider,
                    includeLogLevelAsProperty: true,
                    includeRenderedMessageAsProperty: false,
                    includeMessageTemplateAsProperty: false);
            }
            else
            {
                // default telemetry
                return logEvent.ToDefaultTraceTelemetry(
                    formatProvider,
                    includeLogLevelAsProperty: true,
                    includeRenderedMessageAsProperty: false,
                    includeMessageTemplateAsProperty: false);
            }
        }

        public static ITelemetry UpdateWithContextTelemitryProperties(this ITelemetry telemetry, LogEvent logEvent)
        {
            // Post-process the telemetry's context to contain the user id as desired
            if (logEvent.Properties.ContainsKey("userId"))
            {
                telemetry.Context.User.Id = logEvent.Properties["userId"].ToString();
            }
            // Post-process the telemetry's context to contain the operation id
            if (logEvent.Properties.ContainsKey("activityId"))
            {
                telemetry.Context.Operation.Id = logEvent.Properties["activityId"].ToString();
            }
            // Post-process the telemetry's context to contain the operation parent id
            if (logEvent.Properties.ContainsKey("parentActivityId"))
            {
                telemetry.Context.Operation.ParentId = logEvent.Properties["parentActivityId"].ToString();
            }
            // Typecast to ISupportProperties so you can manipulate the properties as desired
            ISupportProperties propTelematry = (ISupportProperties)telemetry;

            // Find now redundent properties
            var removeProps = new[] { "userId", "activityId", "parentActivityId" };
            removeProps = removeProps.Where(prop => propTelematry.Properties.ContainsKey(prop)).ToArray();

            foreach (var prop in removeProps)
            {
                // Remove those properties
                propTelematry.Properties.Remove(prop);
            }	
            return telemetry;
        }
    }
}
