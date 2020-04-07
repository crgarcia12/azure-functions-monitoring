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
    public class Functions
    {
        private readonly TelemetryClient telemetryClient;

        public Functions(TelemetryConfiguration configuration)
        {
            // If you did not add the APPINSIGHTS_INSTRUMENTATIONKEY in the local.settings.json this constructor is not called
            // This is missing in our docs, check this issue: https://github.com/Azure/azure-functions-host/issues/5530
            this.telemetryClient = new TelemetryClient(configuration);
        }

        [FunctionName(nameof(FunctionWithoutOperations))]
        public void FunctionWithoutOperations([ServiceBusTrigger("functionwithoutoperationsqueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            string functionName = nameof(FunctionWithoutOperations);
            telemetryClient.TrackTrace($"[{functionName}] Received message");
            try
            {
                telemetryClient.TrackTrace($"[{functionName}] Message processed");
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }

            telemetryClient.TrackTrace($"[{functionName}] Done");
        }


        [FunctionName(nameof(FunctionActivityOperation))]
        public void FunctionActivityOperation([ServiceBusTrigger("functionactivityoperationqueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            string functionName = nameof(FunctionActivityOperation);
            var activity = Activity.Current;
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>(activity))
            { 
                telemetryClient.TrackTrace($"[{functionName}] Received message");
                try
                {
                    telemetryClient.TrackTrace($"[{functionName}] Message processed");
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    operation.Telemetry.Success = false;
                    throw;
                }

                telemetryClient.TrackTrace($"[{functionName}] Done");
            }
            telemetryClient.Flush();
        }

        [FunctionName(nameof(FunctionDoubleOperation))]
        public void FunctionDoubleOperation([ServiceBusTrigger("functiondoubleoperationqueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            string functionName = nameof(FunctionDoubleOperation);
            var activity = Activity.Current;
            using (telemetryClient.StartOperation<RequestTelemetry>(activity))
            {
                using (var operation = telemetryClient.StartOperation<RequestTelemetry>("[Function] Processing a new message"))
                {
                    telemetryClient.TrackTrace($"[{functionName}] Received message");
                    try
                    {
                        telemetryClient.TrackTrace($"[{functionName}] Message processed");
                    }
                    catch (Exception ex)
                    {
                        telemetryClient.TrackException(ex);
                        operation.Telemetry.Success = false;
                        throw;
                    }

                    telemetryClient.TrackTrace($"[{functionName}] Done");
                }
            }
            telemetryClient.Flush();
        }

        [FunctionName(nameof(FunctionNewOperationOnly))]
        public void FunctionNewOperationOnly([ServiceBusTrigger("functionnewoperationonlyqueue", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            string functionName = nameof(FunctionNewOperationOnly);
            using (var operation = telemetryClient.StartOperation<RequestTelemetry>("[Function] Processing a new message"))
            {
                telemetryClient.TrackTrace($"[{functionName}] Received message");
                try
                {
                    telemetryClient.TrackTrace($"[{functionName}] Message processed");
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackException(ex);
                    operation.Telemetry.Success = false;
                    throw;
                }

                telemetryClient.TrackTrace($"[{functionName}] Done");
            }
            telemetryClient.Flush();
        }
    }
}
