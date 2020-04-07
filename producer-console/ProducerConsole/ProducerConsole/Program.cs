using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Amqp.Framing;
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

            for (var i = 1100; i < 1201; i++)
            {
                var telemetryProperties = new Dictionary<string, string> { { "Message", "" }, { "Id", i.ToString() } };
                try
                {
                    // Create a new message to send to the queue.
                    dynamic message = new JObject();
                    message.Item = i;

                    telemetryManager.TrackEvent("Sending message", telemetryProperties);

                    var encodedMessage = new Message(Encoding.UTF8.GetBytes(message.ToString()));
                    telemetryProperties["Message"] = message.ToString();
                    // Send the message to the queue.
                    await queueClient.SendAsync(encodedMessage);
                    
                    telemetryManager.TrackEvent("Message sent", telemetryProperties);
                }
                catch (Exception ex)
                {
                    telemetryManager.TrackException(ex, telemetryProperties);
                }
            }
        }

        static async Task Main(string[] args)
        {
            await WorkOnAllQueues();

        }

        static async Task SimpleFlow()
        {
            // Get App Insights client
            var module = new DependencyTrackingTelemetryModule();
            TelemetryConfiguration config = TelemetryConfiguration.CreateDefault();
            config.InstrumentationKey = "9cb1ad14-ac0a-49ea-a2d2-3009a03646ec";
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");
            module.Initialize(config);
            var telemetryClient = new TelemetryClient(config);
            telemetryClient.Context.Cloud.RoleName = "ProducerOperation";
            telemetryClient.Context.Cloud.RoleInstance = "localhost";

            // Create a msg
            string message = "simple message";
            var encodedMessage = new Message(Encoding.UTF8.GetBytes(message.ToString()));
            var telemetryProperties = new Dictionary<string, string> {{ "Message", message } };

            var queueName = "emptyqueue";
            telemetryClient.TrackEvent("BeforeSending", telemetryProperties);

            // Send msg
            QueueClient queueClient = new QueueClient(ServiceBusConnectionString, queueName);
            await queueClient.SendAsync(encodedMessage);

            telemetryClient.TrackEvent("AfterSending", telemetryProperties);
            telemetryClient.Flush();
        }

        static async Task WorkOnAllQueues()
        {
            var queueNames = new string[] { "functionwithoutoperationsqueue", "functionactivityoperationqueue", "functiondoubleoperationqueue", "functionnewoperationonlyqueue" };
            //await DeleteAllQueues(ServiceBusConnectionString, queueNames)
            //await CreateQueues(ServiceBusConnectionString, queueNames);

            var telemetryManager = new TelemetryManager();
            
            using (var operation = telemetryManager.StartOperation<DependencyTelemetry>("root operation"))
            {
                telemetryManager.TrackTrace("Starting producer");
                

                foreach (string queueName in queueNames)
                {
                    await SendMessagesToQueue(queueName, ServiceBusConnectionString, telemetryManager);
                    telemetryManager.Flush();
                }

                telemetryManager.TrackEvent("Finish producer", null);
                telemetryManager.TrackTrace("Finish producer");
                
            }

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