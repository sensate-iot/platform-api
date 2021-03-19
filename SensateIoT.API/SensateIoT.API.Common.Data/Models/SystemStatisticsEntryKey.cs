using System;
using MongoDB.Bson;

namespace SensateIoT.API.Common.Data.Models
{
	public class SystemStatisticsEntryKey
	{
		public ObjectId SensorId { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
