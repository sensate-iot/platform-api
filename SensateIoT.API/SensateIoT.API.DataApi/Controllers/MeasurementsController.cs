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
using Microsoft.Extensions.Logging;

using SensateIoT.API.Common.ApiCore.Attributes;
using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Exceptions;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.DataProcessing;
using SensateIoT.API.Common.Data.Converters;
using SensateIoT.API.Common.Data.Dto;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;
using SensateIoT.API.DataApi.Dto;

using MeasurementsQueryResult = SensateIoT.API.Common.Data.Models.MeasurementsQueryResult;
using MQR = SensateIoT.API.Common.Data.Dto.Generic.MeasurementsQueryResult;

namespace SensateIoT.API.DataApi.Controllers
{
	[Produces("application/json")]
	[Route("data/v1/[controller]")]
	public class MeasurementsController : AbstractDataController
	{
		private readonly IMeasurementRepository m_measurements;
		private readonly ISensorService m_sensorService;
		private readonly ILogger<MeasurementsController> m_logger;

		public MeasurementsController(IMeasurementRepository measurements,
									  ISensorService sensorService,
									  ISensorLinkRepository links,
									  ISensorRepository sensors,
									  IApiKeyRepository keys,
									  ILogger<MeasurementsController> logger,
									  IHttpContextAccessor ctx) : base(ctx, sensors, links, keys)
		{
			this.m_measurements = measurements;
			this.m_sensorService = sensorService;
			this.m_logger = logger;
		}

		[HttpPost("filter")]
		[ProducesResponseType(typeof(Response<IEnumerable<MQR>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Filter([FromBody] Filter filter)
		{
			IEnumerable<MeasurementsQueryResult> result;
			var response = new Response<IEnumerable<MQR>>();

			if(filter.SensorIds == null || filter.SensorIds.Count <= 0) {
				response.Errors.Add("Sensor ID list cannot be empty!");
				return this.UnprocessableEntity(response);
			}

			filter.Skip ??= -1;
			filter.Limit ??= -1;

			var sensors = await this.m_sensorService.GetSensorsAsync(this.CurrentUser).AwaitBackground();
			var filtered = sensors.Values.Where(x => filter.SensorIds.Contains(x.InternalId.ToString())).ToList();

			if(filtered.Count <= 0) {
				response.Errors.Add("None of the input sensor IDs can be found!");
				return this.UnprocessableEntity(response);
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
				result = await this.m_measurements
					.GetMeasurementsNearAsync(filtered, filter.Start, filter.End, coords, filter.Radius.Value,
						filter.Skip.Value, filter.Limit.Value, direction).AwaitBackground();
			} else {
				result = await this.m_measurements
					.GetMeasurementsBetweenAsync(filtered, filter.Start, filter.End, filter.Skip.Value,
												 filter.Limit.Value, direction).AwaitBackground();
			}

			response.Data = MeasurementConverter.Convert(result);
			return this.Ok(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(Response<IEnumerable<MQR>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> Get([FromQuery] string sensorId, [FromQuery] DateTime start, [FromQuery] DateTime end,
			[FromQuery] double? longitude, [FromQuery] double? latitude, [FromQuery] int? radius,
			[FromQuery] int skip = -1, [FromQuery] int limit = -1, [FromQuery] string order = "")
		{
			var response = new Response<IEnumerable<MQR>>();
			var sensor = await this.m_sensors.GetAsync(sensorId).AwaitBackground();
			var orderDirection = order switch {
				"asc" => OrderDirection.Ascending,
				"desc" => OrderDirection.Descending,
				_ => OrderDirection.None,
			};

			if(sensor == null) {
				response.Errors.Add($"Sensor ID {sensorId} not found.");
				return this.NotFound(response);
			}

			var linked = await this.IsLinkedSensor(sensorId).AwaitBackground();

			if(!await this.AuthenticateUserForSensor(sensor, false).AwaitBackground() && !linked) {
				response.Errors.Add($"User {this.CurrentUser?.Id} not authorized on sensor {sensorId}.");
				return this.Unauthorized(response);
			}

			if(end == DateTime.MinValue) {
				end = DateTime.MaxValue;
			}

			IEnumerable<MeasurementsQueryResult> data;

			if(longitude != null && latitude != null) {
				var maxDist = radius ?? 100;
				var coords = new GeoJsonPoint {
					Latitude = latitude.Value,
					Longitude = longitude.Value
				};

				data = await this.m_measurements
					.GetMeasurementsNearAsync(sensor, start, end, coords, maxDist, skip, limit, orderDirection)
					.AwaitBackground();
			} else {
				data = await this.m_measurements.GetBetweenAsync(sensor, start, end,
																	 skip, limit, orderDirection).AwaitBackground();
			}

			response.Data = MeasurementConverter.Convert(data);
			return this.Ok(response);
		}

		[HttpDelete]
		[ReadWriteApiKey]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(Response<object>), StatusCodes.Status401Unauthorized)]
		public async Task<IActionResult> Delete([FromQuery] string sensorId, [FromQuery] DateTime bucketStart, DateTime bucketEnd)
		{
			Sensor sensor;
			var response = new Response<object>();

			try {
				sensor = await this.m_sensors.GetAsync(sensorId).AwaitBackground();

				if(sensor == null) {
					response.Errors.Add($"Sensor ID {sensorId} not found.");
					return this.NotFound(response);
				}

				if(!await this.AuthenticateUserForSensor(sensor, false).AwaitBackground()) {
					response.Errors.Add($"User {this.CurrentUser?.Id} not authorized on sensor {sensorId}.");
					return this.Unauthorized(response);
				}

				await this.m_measurements.DeleteBucketAsync(sensor, bucketStart, bucketEnd, CancellationToken.None).ConfigureAwait(false);
			} catch(DatabaseException ex) {
				this.m_logger.LogWarning(ex, $"Unable to delete measurements for sensor {sensorId} in bucket {bucketStart} - {bucketEnd}.");
				response.Errors.Add($"Unable to delete measurements for sensor {sensorId} in bucket {bucketStart} - {bucketEnd}.");
				return this.StatusCode(500, response);
			}

			return this.NoContent();
		}
	}
}
