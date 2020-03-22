using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ConsumerFunction
{
    public class Function1
    {
        private readonly TelemetryClient telemetryClient;

        public Function1(TelemetryConfiguration configuration)
        {
            // If you did not add the APPINSIGHTS_INSTRUMENTATIONKEY in the local.settings.json this constructor is not called
            // This is a horrible missing in our docs, check this issue: https://github.com/Azure/azure-functions-host/issues/5530
            this.telemetryClient = new TelemetryClient(configuration);
        }

        [FunctionName("Function1")]
        public void Run([ServiceBusTrigger("itemsqueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            var activity = Activity.Current;

            using (var operation = telemetryClient.StartOperation<RequestTelemetry>(activity))
            {
                telemetryClient.TrackTrace("Received message");
                try 
                {
                // process message
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    operation.Telemetry.Success = false;
                    throw;
                }

                telemetryClient.TrackTrace("Done");
        }
            telemetryClient.Flush();
        }
    }
}
