using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProducerConsole
{
    public class TelemetryManager
    {
        const string messageHeader = "[Producer] ";
        TelemetryClient telemetryClient;

        private void ConfigureAppInsights()
        {
            var module = new DependencyTrackingTelemetryModule();

            TelemetryConfiguration config = TelemetryConfiguration.CreateDefault(); // Reads ApplicationInsights.config file if present
            config.InstrumentationKey = "9cb1ad14-ac0a-49ea-a2d2-3009a03646ec";

            // enable known dependency tracking, note that in future versions, we will extend this list. 
            // please check default settings in https://github.com/Microsoft/ApplicationInsights-dotnet-server/blob/develop/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            
            // initialize the module
            module.Initialize(config);

            telemetryClient = new TelemetryClient(config);
            SetRole("ProducerApplication");
        }

        public void SetRole(string roleName)
        {
            telemetryClient.Context.Cloud.RoleName = roleName;
            telemetryClient.Context.Cloud.RoleInstance = "localhost";
        }

        public TelemetryManager()
        {
            ConfigureAppInsights();
        }

        public void TrackTrace(string message, [CallerLineNumber] int lineNumber = 0)
        {
            this.telemetryClient.TrackTrace(messageHeader + lineNumber + ": " + message);
        }

        public void TrackEvent(string eventName, Dictionary<string,string> properties, [CallerLineNumber] int lineNumber = 0)
        {
            telemetryClient.TrackEvent(messageHeader + lineNumber + ": " + eventName, properties);
        }

        public IOperationHolder<T> StartOperation<T>(string operationName, bool rootOperation = false, [CallerLineNumber] int lineNumber = 0) where T : OperationTelemetry, new()
        {
            if (rootOperation)
            {
                var operation = Guid.NewGuid().ToString();
                return telemetryClient.StartOperation<T>(messageHeader + lineNumber + ": " + operationName, operation, operation);
            }
            return telemetryClient.StartOperation<T>(messageHeader + lineNumber + ": " + operationName);
        }

        internal void Flush()
        {
            telemetryClient.Flush();
        }

        internal void TrackException(Exception ex, Dictionary<string, string> telemetryProperties)
        {
            telemetryClient.TrackException(ex, telemetryProperties);
        }
    }
}
