using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;

namespace EDennis.Samples.GatewayApi {

    public class Program {

        public static List<HaveApi> HaveApis = new List<HaveApi>();
        public static string clientId = Guid.NewGuid().ToString();

        private const string SERVER_ACCEPT_TOPIC = "NeedApis";
        private const string SERVER_PUBLISH_TOPIC = "HaveApis";

        public static void Main(string[] args) {
            var builder = CreateWebHostBuilder(args).Build();
            while(HaveApis.Count == 0) {
                Thread.Sleep(1000);
            }
            Task.Run(() => {
                Thread.Sleep(5000);
                StopApis();
            });
            builder.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            var builder = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

            if (builder.GetSetting("Environment") == EnvironmentName.Development) {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.Development.Json")
                    .Build();

                List<NeedApi> needApis = new List<NeedApi>();
                config.GetSection("NeedApis").Bind(needApis);

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", 1883) // Port is optional
                    .Build();

                // Create a new MQTT client.
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();

                mqttClient.Connected += async (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    // Subscribe to a topic
                    await mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic($"HaveApis/{clientId}").Build());

                    Console.WriteLine("### SUBSCRIBED ###");
                };

                mqttClient.ApplicationMessageReceived += (s, e) =>
                {
                    var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    HaveApis = JToken.Parse(json).ToObject<List<HaveApi>>();

                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {json}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();

                };

                mqttClient.ConnectAsync(options).Wait();

                var needApisJson = JToken.FromObject(needApis).ToString();

                var msg = new MqttApplicationMessageBuilder()
                    .WithExactlyOnceQoS()
                    .WithTopic(SERVER_ACCEPT_TOPIC + "/" + clientId)
                    .WithPayload(needApisJson)
                    .Build();

                mqttClient.PublishAsync(msg);

            }
            
            return builder;
        }

        private static void StopApis() {
            // Create a new MQTT client.
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883) // Port is optional
                .Build();

            mqttClient.ConnectAsync(options).Wait();

            var msg = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic(SERVER_ACCEPT_TOPIC + "/" + clientId)
                .WithPayload("[]")
                .Build();

            mqttClient.PublishAsync(msg);


        }


    }
}
