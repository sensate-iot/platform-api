﻿/* 
 * Internal MQTT publish service.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SensateIoT.API.Common.Config.Settings;
using SensateIoT.API.Common.Core.Constants;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Data.Enums;

namespace SensateIoT.API.Common.Core.Services.Processing
{
	public class CommandPublisher : AbstractMqttService, ICommandPublisher
	{
		private readonly CommandPublisherOptions _options;

		public CommandPublisher(IOptions<CommandPublisherOptions> options, ILogger<CommandPublisher> logger, IServiceProvider sp) :
			base(options.Value.Host, options.Value.Port, options.Value.Ssl, "", logger, sp)
		{
			this._options = options.Value;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			await this.Connect(this._options.Username, this._options.Password).AwaitBackground();
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			await base.StopAsync(cancellationToken);
			await this.Disconnect().AwaitBackground();

			this._logger.LogInformation("Publish MQTT client disconnected!");
		}

		public async Task PublishOnAsync(string topic, string message, bool retain)
		{
			MqttApplicationMessageBuilder builder;
			MqttApplicationMessage msg;

			if(!this.Client.IsConnected)
				return;

			builder = new MqttApplicationMessageBuilder();
			builder.WithAtLeastOnceQoS();
			builder.WithPayload(message);
			builder.WithTopic(topic);
			builder.WithRetainFlag(retain);
			msg = builder.Build();

			await this.Client.PublishAsync(msg);
		}

		public void PublishOn(string topic, string message, bool retain)
		{
			var task = Task.Run(async () => { await this.PublishOnAsync(topic, message, retain).AwaitBackground(); });
			task.Wait();
		}

		protected override Task OnConnectAsync()
		{
			this._logger.LogInformation("Publish MQTT client connected!");
			return Task.CompletedTask;
		}

		protected override Task OnMessageAsync(string topic, string msg)
		{
			return Task.CompletedTask;
		}

		public async Task PublishCommand(CommandType cmd, string argument, bool retain)
		{
			var obj = new JObject {
				[Commands.CommandKey] = cmd.ToString("G"),
				[Commands.ArgumentKey] = argument
			};

			await this.PublishOnAsync(this._options.CommandsTopic, obj.ToString(Formatting.None), false).AwaitBackground();
		}
	}
}
