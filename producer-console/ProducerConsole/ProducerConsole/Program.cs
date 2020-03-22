using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProducerConsole
{
    class Program
    {
        private static TelemetryClient telemetryClient;

        const string ServiceBusConnectionString = "Endpoint=sb://crgar-function-monitoring-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=K4Ee0JIMTr3fB5dHmXht6KFbDQyyHfgdkEKX29DoaLU=";
        const string QueueName = "itemsqueue";
        static IQueueClient queueClient;

        private static void ConfigureAppInsights()
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
            telemetryClient.TrackTrace("App Insights initialized!");
        }


        static async Task Main(string[] args)
        {
            ConfigureAppInsights();

            Console.WriteLine("Starting application");
            telemetryClient.TrackTrace("Starting producer");

            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);

            for (var i = 20; i < 30; i++)
            {
                using(var operation = telemetryClient.StartOperation<RequestTelemetry>($"Generating Item"))
                {
                    var telemetryProperties = new Dictionary<string, string> { { "Message", "" }, { "Id", i.ToString() } };
                    try
                    {
                        // Create a new message to send to the queue.
                        dynamic message = new JObject();
                        message.Item = i;

                        var encodedMessage = new Message(Encoding.UTF8.GetBytes(message.ToString()));
                        telemetryProperties["Message"] = message.ToString();


                        telemetryClient.TrackEvent("Sending message", telemetryProperties);

                        // Send the message to the queue.
                        DateTime startTime = DateTime.UtcNow;
                        await queueClient.SendAsync(encodedMessage);

                        telemetryClient.TrackEvent("Message sent", telemetryProperties);
                        operation.Telemetry.Success = true;
                    }
                    catch (Exception ex)
                    {
                        operation.Telemetry.Success = false;
                        telemetryClient.TrackException(ex, telemetryProperties);
                    }
                }
            }

            telemetryClient.TrackTrace("Finish producer");
            telemetryClient.Flush();
            
            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}