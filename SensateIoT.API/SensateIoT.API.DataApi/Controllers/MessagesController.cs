/*
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
using SensateIoT.API.Common.Data.Dto;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;
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
			var response = new Response<object>();

			response.AddError("Unable to authorize current user!");
			return this.Unauthorized(response);
		}

		[HttpGet("{messageId}")]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<Message>), StatusCodes.Status200OK)]
		public async Task<IActionResult> Get(string messageId)
		{
			var msg = await this.m_messages.GetAsync(messageId).AwaitBackground();
			var response = new Response<Message>();

			if(msg == null) {
				response.Errors.Add($"Message with ID {messageId} not found.");
				return this.NotFound(response);
			}

			var auth = await this.AuthenticateUserForSensor(msg.SensorId.ToString()).AwaitBackground();

			if(!auth) {
				return this.CreateNotAuthorizedResult();
			}

			response.Data = MessageConverter.Convert(msg);
			return this.Ok(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<IEnumerable<Message>>), StatusCodes.Status200OK)]
		public async Task<IActionResult> Get([FromQuery] string sensorId, [FromQuery] DateTime? start, [FromQuery] DateTime? end,
											 [FromQuery] int skip = 0, [FromQuery] int take = 0,
											 [FromQuery] string order = "asc")
		{
			var response = new Response<IEnumerable<Message>>();
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
				response.Errors.Add($"Sensor with ID {sensorId} not found.");
				return this.NotFound(response);
			}

			var msgs = await this.m_messages.GetAsync(sensor, start.Value, end.Value, skip, take, orderDirection).AwaitBackground();
			response.Data = MessageConverter.Convert(msgs);

			return this.Ok(response);
		}

		[HttpPost("filter")]
		[ProducesResponseType(typeof(Response<PaginationResult<Message>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Filter([FromBody] Filter filter)
		{
			var status = new Status();
			var pagination = new Response<PaginationResult<Common.Data.Models.Message>>();

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
				pagination.AddError("None of the input sensors could be found.");
				return this.UnprocessableEntity(pagination);
			}

			await this.GetFilteredMessagesAsync(filter, pagination, filtered).ConfigureAwait(false);
			return this.Ok(pagination);
		}

		private async Task GetFilteredMessagesAsync(Filter filter, Response<PaginationResult<Common.Data.Models.Message>> pagination, List<Sensor> filtered)
		{
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

			pagination.Data = new PaginationResult<Common.Data.Models.Message>();

			if(filter.Latitude != null & filter.Longitude != null && filter.Radius != null && filter.Radius.Value > 0) {
				var coords = new GeoJsonPoint {
					Latitude = filter.Latitude.Value,
					Longitude = filter.Longitude.Value
				};


				pagination.Data.Values = await this.m_messages
					.GetMessagesNearAsync(filtered, filter.Start, filter.End, coords, filter.Radius.Value,
										  filter.Skip!.Value, filter.Limit!.Value, direction).AwaitBackground();
				pagination.Data.Count = (int)await this.m_messages
					.CountAsync(filtered, filter.Start, filter.End, coords, filter.Radius.Value).ConfigureAwait(false);
			} else {
				pagination.Data.Values = await this.m_messages
					.GetMessagesBetweenAsync(filtered, filter.Start, filter.End, filter.Skip!.Value,
											 filter.Limit!.Value, direction).AwaitBackground();
				pagination.Data.Count = (int)await this.m_messages
					.CountAsync(filtered, filter.Start, filter.End, null, 0).ConfigureAwait(false);
			}
		}

		[HttpDelete]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
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
				var response = new Response<object>();
				response.AddError($"Sensor with Sensor ID {sensorId} could not be found.");
				return this.NotFound(response);
			}

			await this.m_messages.DeleteBySensorAsync(sensor, start.Value, end.Value, CancellationToken.None);
			return this.NoContent();
		}
	}
}

