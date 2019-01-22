using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace EDennis.AspNetCore.ApiLauncher {

    public class MqttService : IHostedService, IDisposable {
        private readonly ILogger _logger;
        private readonly IOptions<MqttConfig> _config;
        private readonly Launcher _launcher;
        private IMqttServer _mqttServer;

        private const string SERVER_ACCEPT_TOPIC = "NeedApis";
        private const string SERVER_PUBLISH_TOPIC = "HaveApis";

        public MqttService(ILogger<MqttService> logger, IOptions<MqttConfig> config,
            Launcher launcher) {
            _logger = logger;
            _config = config;
            _launcher = launcher;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Starting MQTT Daemon on port " + _config.Value.Port);

            //Building the config
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(1000)
                .WithDefaultEndpointPort(_config.Value.Port);


            //Getting an MQTT Instance
            _mqttServer = new MqttFactory().CreateMqttServer();

            //Wiring up all the events...

            _mqttServer.ClientSubscribedTopic += _mqttServer_ClientSubscribedTopic;
            _mqttServer.ClientUnsubscribedTopic += _mqttServer_ClientUnsubscribedTopic;
            _mqttServer.ClientConnected += _mqttServer_ClientConnected;
            _mqttServer.ClientDisconnected += _mqttServer_ClientDisconnected;
            _mqttServer.ApplicationMessageReceived += _mqttServer_ApplicationMessageReceived;

            //Now, start the server -- Notice this is resturning the MQTT Server's StartAsync, which is a task.
            await _mqttServer.StartAsync(optionsBuilder.Build());

            return;
        }

        private void _mqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e) {

            _logger.LogInformation($"{e.ClientId} sent message with topic {e.ApplicationMessage.Topic}\n{e.ApplicationMessage.Payload}");

            var topic = e.ApplicationMessage.Topic;
            var topicTokens = topic.Split("/");
            var topicString = topicTokens[0];
            var clientId = topicTokens[1];

            if (!topic.StartsWith(SERVER_ACCEPT_TOPIC)) {
                return;
            }

            List<NeedApi> needApis;

            string payloadString = "";
            try {
                var payloadBytes = e.ApplicationMessage.Payload;
                payloadString = Encoding.UTF8.GetString(payloadBytes, 0, payloadBytes.Length);
                _logger.LogInformation($"Message Payload\n{payloadString}");
                needApis = JToken.Parse(payloadString).ToObject<List<NeedApi>>();
            } catch (Exception ex) {
                _logger.LogError($"EXCEPTION with message\n:{payloadString}\n{ex.Message}");
                throw new ArgumentException(ex.Message);
            }

            if (needApis.Count == 0) {
                _launcher.StopApis();
            } else {
                _launcher.StartApis(needApis);

                var haveApisJson = JToken.FromObject(_launcher.LaunchedApis).ToString();

                var msg = new MqttApplicationMessageBuilder()
                    .WithExactlyOnceQoS()
                    .WithTopic(SERVER_PUBLISH_TOPIC + "/" + clientId)
                    .WithPayload(haveApisJson)
                    .Build();

                _mqttServer.PublishAsync(msg);

                foreach (var api in _launcher.LaunchedApis)
                    _logger.LogInformation($"Server has {api.ProjectName} @ {api.Port}");
            }


        }

        private void _mqttServer_ClientDisconnected(object sender, MqttClientDisconnectedEventArgs e) {
            _logger.LogInformation(e.ClientId + " Disonnected.");
        }

        private void _mqttServer_ClientConnected(object sender, MqttClientConnectedEventArgs e) {
            _logger.LogInformation(e.ClientId + " Connected.");
        }

        private void _mqttServer_ClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e) {
            _logger.LogInformation(e.ClientId + " unsubscribed to " + e.TopicFilter);
        }

        private void _mqttServer_ClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e) {
            _logger.LogInformation(e.ClientId + " subscribed to " + e.TopicFilter);
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Stopping Mqtt Daemon.");
            _launcher.StopApis();
            return _mqttServer.StopAsync();
        }

        public void Dispose() {
            _logger.LogInformation("Disposing....");
            _launcher.Dispose();
        }
    }
}