﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using CsvHelper;

using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.DataProcessing;
using SensateIoT.API.Common.Data.Converters;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.DataApi.Dto;

namespace SensateIoT.API.DataApi.Controllers
{
	[Produces("application/json")]
	[Route("data/v1/[controller]")]
	public class ExportController : AbstractDataController
	{
		private readonly IMeasurementRepository m_measurements;
		private readonly ISensorService m_sensorService;

		public ExportController(IHttpContextAccessor ctx,
								IMeasurementRepository measurements,
								ISensorService sensorService,
								ISensorRepository sensors,
								ISensorLinkRepository links,
								IApiKeyRepository keys) : base(ctx, sensors, links, keys)
		{
			this.m_measurements = measurements;
			this.m_sensorService = sensorService;
		}

		[HttpPost("measurements")]
		[ProducesResponseType(typeof(Status), StatusCodes.Status422UnprocessableEntity)]
		public async Task<IActionResult> Filter([FromBody] Filter filter)
		{
			var measurements = await this.GetMeasurementsAsync(filter).AwaitBackground();

			if(measurements == null) {
				var status = new Status {
					Message = "Unable to fetch measurements!",
					ErrorCode = ReplyCode.BadInput
				};

				return this.UnprocessableEntity(status);
			}

			var records = new List<dynamic>();

			foreach(var measurement in measurements) {
				dynamic record = new ExpandoObject();

				record.SensorId = measurement.SensorId;
				record.Longitude = measurement.Location.Longitude;
				record.Latitude = measurement.Location.Latitude;
				record.Timestamp = measurement.Timestamp.ToString("o");

				foreach(var kvp in measurement.Data) {
					var precision = 0D;
					var accuracy = 0D;

					AddProperty(record, kvp.Key, kvp.Value.Value);

					if(kvp.Value.Accuracy.HasValue) {
						precision = kvp.Value.Accuracy.Value;
					}

					if(kvp.Value.Precision.HasValue) {
						accuracy = kvp.Value.Precision.Value;
					}

					AddProperty(record, $"{kvp.Key}_Precision", precision);
					AddProperty(record, $"{kvp.Key}_Accuracy", accuracy);
					AddProperty(record, $"{kvp.Key}_Unit", kvp.Value.Unit);
				}

				records.Add(record);
			}

			var stream = new MemoryStream();
			await using(var writer = new StreamWriter(stream, leaveOpen: true)) {
				var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, true);
				await csv.WriteRecordsAsync(records).AwaitBackground();
			}

			stream.Position = 0;
			return this.File(stream, "application/octet-stream", "measurements.csv");
		}

		private static void AddProperty(ExpandoObject expando, string propertyName, object obj)
		{
			var expandoDict = expando as IDictionary<string, object>;

			if(expandoDict.ContainsKey(propertyName)) {
				expandoDict[propertyName] = obj;
			} else {
				expandoDict.Add(propertyName, obj);
			}
		}

		private async Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsAsync(Filter filter)
		{
			var status = new Status();
			IEnumerable<Common.Data.Models.MeasurementsQueryResult> result;

			if(filter.SensorIds == null || filter.SensorIds.Count <= 0) {
				status.ErrorCode = ReplyCode.BadInput;
				status.Message = "Sensor ID list cannot be empty!";

				return null;
			}

			filter.Skip ??= -1;
			filter.Limit ??= -1;

			var sensors = await this.m_sensorService.GetSensorsAsync(this.CurrentUser).AwaitBackground();
			var filtered = sensors.Values.Where(x => filter.SensorIds.Contains(x.InternalId.ToString())).ToList();

			if(filtered.Count <= 0) {
				status.Message = "No sensors available!";
				status.ErrorCode = ReplyCode.NotAllowed;

				return null;
			}

			OrderDirection direction;

			if(string.IsNullOrEmpty(filter.OrderDirection)) {
				filter.OrderDirection = "";
			}

			if(filter.End == DateTime.MinValue) {
				filter.End = DateTime.MaxValue;
			}

			direction = filter.OrderDirection switch
			{
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

			return MeasurementConverter.Convert(result);
		}
	}
}
