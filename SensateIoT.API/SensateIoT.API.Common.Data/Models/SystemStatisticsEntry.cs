/*
 * Model for the system statistics view.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using MongoDB.Bson.Serialization.Attributes;

namespace SensateIoT.API.Common.Data.Models
{
	public class SystemStatisticsEntry
	{
		[BsonId]
		public SystemStatisticsEntryKey Id { get; set; }
		public long TotalMessagesRouted { get; set; }
		public long TotalMeasurementsStored { get; set; }
		public long TotalMessagesStored { get; set; }
		public long TotalTriggersExecuted { get; set; }
	}
}
