﻿/*
 * Measurement statistics repository.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using SensateIoT.API.Common.ApiCore.Attributes;
using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Data.Dto;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;
using SensateIoT.API.Common.IdentityData.Models;
using SensateIoT.API.DataApi.Json;

namespace SensateIoT.API.DataApi.Controllers
{
	[Produces("application/json")]
	[Route("data/v1/[controller]")]
	public class StatisticsController : AbstractDataController
	{
		private readonly ISensorRepository _sensors;
		private readonly IAuditLogRepository m_auditlogs;
		private readonly ILogger<StatisticsController> m_logger;
		private readonly IUserRepository m_users;
		private readonly IBlobRepository m_blobs;
		private readonly ISystemStatisticsRepository m_systemStats;

		public StatisticsController(ISensorRepository sensors,
									ISystemStatisticsRepository systemStats,
									ISensorLinkRepository links,
									IAuditLogRepository logs,
									IUserRepository users,
									IBlobRepository blobs,
									IApiKeyRepository keys,
									ILogger<StatisticsController> loger,
									IHttpContextAccessor ctx) : base(ctx, sensors, links, keys)
		{
			this._sensors = sensors;
			this.m_logger = loger;
			this.m_auditlogs = logs;
			this.m_users = users;
			this.m_blobs = blobs;
			this.m_systemStats = systemStats;
		}

		[HttpGet(Name = "StatsIndex")]
		[ActionName("QueryAllStats")]
		[ProducesResponseType(typeof(Response<Count>), 200)]
		[ProducesResponseType(typeof(Response<object>), 401)]
		public async Task<IActionResult> Index([FromQuery] DateTime start, [FromQuery] DateTime end)
		{
			var __sensors = await this._sensors.GetAsync(this.CurrentUser).AwaitBackground();
			var sensors = __sensors.ToList();

			if(sensors.Count <= 0) {
				return this.Ok(new Response<Count>());
			}

			return await this.CountAsync(this.CurrentUser, sensors, start, end).ConfigureAwait(false);
		}

		[HttpGet("{sensorId}")]
		[ActionName("QueryStatsByDate")]
		[ProducesResponseType(typeof(Response<Count>), 200)]
		[ProducesResponseType(typeof(Response<object>), 401)]
		public async Task<IActionResult> StatisticsBySensor(string sensorId, [FromQuery] DateTime start, [FromQuery] DateTime end)
		{
			var sensor = await this._sensors.GetAsync(sensorId).ConfigureAwait(false);
			var auth = await this.AuthenticateUserForSensor(sensor, false);

			if(!auth) {
				var response = new Response<object>();
				response.AddError($"Unable to authorize user for {sensorId}.");
				return this.Unauthorized(response);
			}

			var sensors = new List<Sensor> { sensor };
			return await this.CountAsync(this.CurrentUser, sensors, start, end).AwaitBackground();
		}

		[AdminApiKey]
		[HttpGet("count/{userId}/{sensorId}")]
		[ProducesResponseType(typeof(Response<Count>), 200)]
		[ProducesResponseType(typeof(Response<object>), 401)]
		public async Task<IActionResult> CountByUserAndSensorAsync(string userId,
											   string sensorId,
											   [FromQuery] DateTime start,
											   [FromQuery] DateTime end)
		{

			var user = await this.m_users.GetAsync(userId).AwaitBackground();
			var sensor = await this._sensors.GetAsync(sensorId).ConfigureAwait(false);
			var sensors = new List<Sensor> { sensor };

			if(sensor.Owner != user.Id) {
				var response = new Response<object>();
				response.AddError($"Unable to authorize user for {sensorId}.");
				return this.Unauthorized(response);
			}

			return await this.CountAsync(user, sensors, start, end).AwaitBackground();
		}


		[AdminApiKey]
		[HttpGet("count/{userId}")]
		[ProducesResponseType(typeof(Response<Count>), 200)]
		[ProducesResponseType(typeof(Response<object>), 401)]
		public async Task<IActionResult> CountByUserIdAsync(string userId,
											   [FromQuery] DateTime start,
											   [FromQuery] DateTime end)
		{

			var user = await this.m_users.GetAsync(userId).AwaitBackground();

			if(user == null) {
				return this.NotFound();
			}

			var sensors = await this._sensors.GetAsync(user).ConfigureAwait(false);
			return await this.CountAsync(user, sensors.ToList(), start, end).ConfigureAwait(false);
		}

		[HttpGet("count")]
		[ProducesResponseType(typeof(Response<Count>), 200)]
		[ProducesResponseType(typeof(Response<object>), 401)]
		public async Task<IActionResult> CountBySensorIdAsync([FromQuery] string sensorId,
															  [FromQuery] DateTime start,
															  [FromQuery] DateTime end)
		{
			var sensor = await this._sensors.GetAsync(sensorId).ConfigureAwait(false);
			var auth = await this.AuthenticateUserForSensor(sensor, false);

			if(!auth) {
				var response = new Response<object>();
				response.AddError($"Unable to authorize user for {sensorId}.");
				return this.Unauthorized(response);
			}

			var sensors = new List<Sensor> { sensor };

			return await this.CountAsync(this.CurrentUser, sensors, start, end).AwaitBackground();
		}

		private async Task<IActionResult> CountAsync(SensateUser user,
											   IList<Sensor> sensors,
											   DateTime start,
											   DateTime end)
		{
			var response = new Response<Count>();

			try {


				var statsTask = this.m_systemStats.GetBetweenAsync(sensors, start, end);
				var blobTask = this.m_blobs.GetAsync(sensors, start, end);
				var blobs = await blobTask.AwaitBackground();
				long bytes = 0;

				if(blobs != null) {
					bytes = blobs.Aggregate(0L, (x, blob) => x + blob.FileSize);
				}

				var logs = await this.m_auditlogs.CountAsync(entry => entry.AuthorId == user.Id &&
														  entry.Timestamp >= start.ToUniversalTime() &&
														  entry.Timestamp <= end.ToUniversalTime() &&
														  (entry.Method == RequestMethod.HttpGet ||
														   entry.Method == RequestMethod.MqttWebSocket ||
														   entry.Method == RequestMethod.HttpDelete ||
														   entry.Method == RequestMethod.HttpPatch ||
														   entry.Method == RequestMethod.HttpPost ||
														   entry.Method == RequestMethod.HttpPut)).AwaitBackground();

				var statsResult = await statsTask.ConfigureAwait(false);

				response.Data = new Count {
					BlobStorage = bytes,
					Sensors = await this._sensors.CountAsync(user).AwaitBackground(),
					Measurements = 0,
					Links = await this.m_links.CountAsync(user).AwaitBackground(),
					TriggerInvocations = 0,
					ApiCalls = logs,
					Messages = 0,
					MessagesRoutedOther = 0
				};

				foreach(var systemStatisticsEntry in statsResult) {
					response.Data.MessagesRoutedOther += systemStatisticsEntry.TotalMessagesRouted -
												 systemStatisticsEntry.TotalTriggersExecuted;
					response.Data.Messages += systemStatisticsEntry.TotalMessagesStored;
					response.Data.Measurements += systemStatisticsEntry.TotalMeasurementsStored;
					response.Data.TriggerInvocations += systemStatisticsEntry.TotalTriggersExecuted;
				}
			} catch(Exception ex) {
				this.m_logger.LogWarning(ex, $"Unable to count statistics between {start} and {end}");

				return this.StatusCode(500, new Status {
					Message = "Unable to count statistics",
					ErrorCode = ReplyCode.BadInput
				});
			}

			return this.Ok(response);
		}
	}
}
