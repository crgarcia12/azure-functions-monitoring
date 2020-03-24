using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProducerConsole
{
    class Program
    {
        const string ServiceBusConnectionString = "Endpoint=sb://crgar-function-monitoring-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=K4Ee0JIMTr3fB5dHmXht6KFbDQyyHfgdkEKX29DoaLU=";
        const string QueueName = "itemsqueue";

        // Make sure a bunch of queues are available in the SB namespace
        static async Task CreateQueues(string connectionString, string[] queueNames)
        {
            var managementClient = new ManagementClient(connectionString);
            var allQueues = await managementClient.GetQueuesAsync();

            foreach (string queueName in queueNames)
            {
                var foundQueue = allQueues.Where(q => q.Path == queueName.ToLower()).SingleOrDefault();
                
                if (foundQueue == null)
                {
                    await managementClient.CreateQueueAsync(queueName);
                }

            }
        }
        
        static async Task SendMessagesToQueue(string queueName, string serviceBusConnectionString, TelemetryManager telemetryManager)
        {
            QueueClient queueClient = new QueueClient(serviceBusConnectionString, queueName);

            for (var i = 1100; i < 1101; i++)
            {
                using (var operation = telemetryManager.StartOperation<RequestTelemetry>($"Generating Item"))
                {
                    var telemetryProperties = new Dictionary<string, string> { { "Message", "" }, { "Id", i.ToString() } };
                    try
                    {
                        // Create a new message to send to the queue.
                        dynamic message = new JObject();
                        message.Item = i;

                        var encodedMessage = new Message(Encoding.UTF8.GetBytes(message.ToString()));
                        telemetryProperties["Message"] = message.ToString();


                        telemetryManager.TrackEvent("Sending message", telemetryProperties);

                        // Send the message to the queue.
                        DateTime startTime = DateTime.UtcNow;
                        await queueClient.SendAsync(encodedMessage);

                        telemetryManager.TrackEvent("Message sent", telemetryProperties);
                        operation.Telemetry.Success = true;
                    }
                    catch (Exception ex)
                    {
                        operation.Telemetry.Success = false;
                        telemetryManager.TrackException(ex, telemetryProperties);
                    }
                }
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting application");

            var queueNames = new string[] { "functionwithoutoperationsqueue", "functionactivityoperationqueue", "functiondoubleoperationqueue", "functionnewoperationonlyqueue" };
            //await DeleteAllQueues(ServiceBusConnectionString, queueNames);

            var telemetryManager = new TelemetryManager();
            telemetryManager.TrackTrace("Starting producer");

            //await CreateQueues(ServiceBusConnectionString, queueNames);

            foreach (string queueName in queueNames)
            {
                telemetryManager.SetRole($"Producer for {queueName}");
                using (var operation = telemetryManager.StartOperation<RequestTelemetry>($"RootOperationforQueue", true))
                {
                    await SendMessagesToQueue(queueName, ServiceBusConnectionString, telemetryManager);
                }
                telemetryManager.Flush();
            }

            telemetryManager.TrackTrace("Finish producer");
            telemetryManager.Flush();
            
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static async Task DeleteAllQueues(string connectionString, string[] queueNames)
        {
            var managementClient = new ManagementClient(connectionString);
            IList<QueueDescription> allQueues = await managementClient.GetQueuesAsync();

            foreach (QueueDescription queue in allQueues)
            {
                await managementClient.DeleteQueueAsync(queue.Path);
            }
        }

    }
}