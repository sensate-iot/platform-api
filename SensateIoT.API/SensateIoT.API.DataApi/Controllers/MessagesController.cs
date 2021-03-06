﻿/*
 * Measurement API controller.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.DataProcessing;
using SensateIoT.API.Common.Data.Converters;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.DataApi.Dto;

using Message = SensateIoT.API.Common.Data.Dto.Generic.Message;

namespace SensateIoT.API.DataApi.Controllers
{
	[Produces("application/json")]
	[Route("data/v1/[controller]")]
	public class MessagesController : AbstractDataController
	{
		private readonly IMessageRepository m_messages;
		private readonly ISensorService m_sensorService;

		public MessagesController(IHttpContextAccessor ctx,
								  IMessageRepository messages,
								  ISensorLinkRepository links,
								  IApiKeyRepository keys,
								  ISensorService sensorService,
								  ISensorRepository sensors) : base(ctx, sensors, links, keys)
		{
			this.m_messages = messages;
			this.m_sensorService = sensorService;
		}

		private IActionResult CreateNotAuthorizedResult()
		{
			return this.Unauthorized(new Status {
				Message = "Unable to authorize current user!",
				ErrorCode = ReplyCode.NotAllowed
			});
		}

		[HttpGet("{messageId}")]
		[ProducesResponseType(typeof(Status), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
		public async Task<IActionResult> Get(string messageId)
		{
			var msg = await this.m_messages.GetAsync(messageId).AwaitBackground();

			if(msg == null) {
				return this.NotFound();
			}

			var auth = await this.AuthenticateUserForSensor(msg.SensorId.ToString()).AwaitBackground();

			return auth ? this.Ok(MessageConverter.Convert(msg)) : this.CreateNotAuthorizedResult();
		}

		[HttpGet]
		[ProducesResponseType(typeof(Status), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(IEnumerable<Message>), StatusCodes.Status200OK)]
		public async Task<IActionResult> Get([FromQuery] string sensorId, [FromQuery] DateTime? start, [FromQuery] DateTime? end,
											 [FromQuery] int skip = 0, [FromQuery] int take = 0,
											 [FromQuery] string order = "asc")
		{
			var orderDirection = order switch {
				"asc" => OrderDirection.Ascending,
				"desc" => OrderDirection.Descending,
				_ => OrderDirection.None,
			};

			start ??= DateTime.MinValue;
			end ??= DateTime.Now;

			start = start.Value.ToUniversalTime();
			end = end.Value.ToUniversalTime();

			var auth = await this.AuthenticateUserForSensor(sensorId).AwaitBackground();

			if(!auth) {
				return this.CreateNotAuthorizedResult();
			}

			var sensor = await this.m_sensors.GetAsync(sensorId).AwaitBackground();

			if(sensor == null) {
				return this.NotFound();
			}

			var msgs = await this.m_messages.GetAsync(sensor, start.Value, end.Value, skip, take, orderDirection).AwaitBackground();
			return this.Ok(MessageConverter.Convert(msgs));
		}

		[HttpPost("filter")]
		[ProducesResponseType(typeof(IEnumerable<Message>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(Status), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Filter([FromBody] Filter filter)
		{
			var status = new Status();
			var pagination = new PaginationResult<Common.Data.Models.Message>();

			if(filter.SensorIds == null || filter.SensorIds.Count <= 0) {
				status.ErrorCode = ReplyCode.BadInput;
				status.Message = "Sensor ID list cannot be empty!";

				return this.UnprocessableEntity(status);
			}

			filter.Skip ??= -1;
			filter.Limit ??= -1;

			var sensors = await this.m_sensorService.GetSensorsAsync(this.CurrentUser).AwaitBackground();
			var filtered = sensors.Values.Where(x => filter.SensorIds.Contains(x.InternalId.ToString())).ToList();

			if(filtered.Count <= 0) {
				status.Message = "No sensors available!";
				status.ErrorCode = ReplyCode.NotAllowed;

				return this.UnprocessableEntity(status);
			}

			OrderDirection direction;

			if(string.IsNullOrEmpty(filter.OrderDirection)) {
				filter.OrderDirection = "";
			}

			if(filter.End == DateTime.MinValue) {
				filter.End = DateTime.MaxValue;
			}

			direction = filter.OrderDirection switch {
				"asc" => OrderDirection.Ascending,
				"desc" => OrderDirection.Descending,
				_ => OrderDirection.None,
			};

			if(filter.Latitude != null & filter.Longitude != null && filter.Radius != null && filter.Radius.Value > 0) {
				var coords = new GeoJsonPoint {
					Latitude = filter.Latitude.Value,
					Longitude = filter.Longitude.Value
				};

				pagination.Values = await this.m_messages
					.GetMessagesNearAsync(filtered, filter.Start, filter.End, coords, filter.Radius.Value,
						filter.Skip.Value, filter.Limit.Value, direction).AwaitBackground();
				pagination.Count = (int)await this.m_messages
					.CountAsync(filtered, filter.Start, filter.End, coords, filter.Radius.Value).ConfigureAwait(false);
			} else {
				pagination.Values = await this.m_messages
					.GetMessagesBetweenAsync(filtered, filter.Start, filter.End, filter.Skip.Value,
												 filter.Limit.Value, direction).AwaitBackground();
				pagination.Count = (int)await this.m_messages
					.CountAsync(filtered, filter.Start, filter.End, null, 0).ConfigureAwait(false);
			}


			return this.Ok(pagination);
		}

		[HttpDelete]
		[ProducesResponseType(typeof(Status), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<IActionResult> DeleteAsync([FromQuery] string sensorId,
													 [FromQuery] DateTime? start,
													 [FromQuery] DateTime? end)
		{
			start ??= DateTime.MinValue;
			end ??= DateTime.Now;

			start = start.Value.ToUniversalTime();
			end = end.Value.ToUniversalTime();

			var auth = await this.AuthenticateUserForSensor(sensorId).AwaitBackground();

			if(!auth) {
				return this.CreateNotAuthorizedResult();
			}

			var sensor = await this.m_sensors.GetAsync(sensorId).AwaitBackground();

			if(sensor == null) {
				return this.NotFound();
			}

			await this.m_messages.DeleteBySensorAsync(sensor, start.Value, end.Value, CancellationToken.None);
			return this.NoContent();
		}
	}
}

