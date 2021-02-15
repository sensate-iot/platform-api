﻿/*
 * Measurement geo query result model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using Newtonsoft.Json;

using SensateIoT.API.Common.Data.Converters;

namespace SensateIoT.API.Common.Data.Models
{
	public class MeasurementsQueryResult
	{
		[JsonIgnore]
		public ObjectId _id { get; set; }
		[JsonConverter(typeof(ObjectIdJsonConverter))]
		public ObjectId SensorId { get; set; }
		public DateTime Timestamp { get; set; }
		[JsonConverter(typeof(GeoJsonPointJsonConverter))]
		public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
		public IDictionary<string, DataPoint> Data { get; set; }
	}
}
