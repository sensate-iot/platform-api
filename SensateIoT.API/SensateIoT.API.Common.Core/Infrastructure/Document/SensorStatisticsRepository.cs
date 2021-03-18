/*
 * Sensor statistics implementation.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Driver;

using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public class SensorStatisticsRepository : AbstractDocumentRepository<SensorStatisticsEntry>, ISensorStatisticsRepository
	{
		private readonly IMongoCollection<SensorStatisticsEntry> _stats;

		public SensorStatisticsRepository(SensateContext context) : base(context.Statistics)
		{
			this._stats = context.Statistics;
		}

		public async Task<IEnumerable<SensorStatisticsEntry>> GetBetweenAsync(IList<Sensor> sensors, DateTime start, DateTime end)
		{
			var builder = Builders<SensorStatisticsEntry>.Filter;
			var ids = sensors.Select(x => x.InternalId);
			var startHour = start.ThisHour();
			var endHour = end.ThisHour();
			var filter = builder.In(x => x.SensorId, ids) &
						 builder.Gte(x => x.Timestamp, startHour) &
						 builder.Lte(x => x.Timestamp, endHour) & 
						 builder.Eq(x => x.Type, StatisticsType.MeasurementStorage);

			var result = await this._stats.FindAsync(filter).AwaitBackground();
			return await result.ToListAsync().AwaitBackground();
		}


		public async Task<IEnumerable<SensorStatisticsEntry>> GetAfterAsync(IEnumerable<Sensor> sensors, DateTime dt)
		{
			FilterDefinition<SensorStatisticsEntry> filter;
			var filterBuilder = Builders<SensorStatisticsEntry>.Filter;
			var date = dt.ThisHour();
			var ids = sensors.Select(x => x.InternalId);

			filter = filterBuilder.In(x => x.SensorId, ids) &
			         filterBuilder.Gte(x => x.Timestamp, date) &
			         filterBuilder.Eq(x => x.Type, StatisticsType.MeasurementStorage);
			var result = await this._stats.FindAsync(filter).AwaitBackground();

			if(result == null) {
				return null;
			}

			return await result.ToListAsync().AwaitBackground();
		}

		public async Task<IEnumerable<SensorStatisticsEntry>> GetAfterAsync(DateTime date)
		{
			FilterDefinition<SensorStatisticsEntry> filter;
			var filterBuilder = Builders<SensorStatisticsEntry>.Filter;

			filter = filterBuilder.Gte(x => x.Timestamp, date) &
				filterBuilder.Eq(x => x.Type, StatisticsType.MeasurementStorage);
			var result = await this._stats.FindAsync(filter).AwaitBackground();

			if(result == null)
				return null;

			return await result.ToListAsync().AwaitBackground();
		}

		public async Task<IEnumerable<SensorStatisticsEntry>> GetBetweenAsync(Sensor sensor, DateTime start, DateTime end)
		{
			FilterDefinition<SensorStatisticsEntry> filter;

			var builder = Builders<SensorStatisticsEntry>.Filter;
			var startDate = start.ThisHour();
			var endDate = end.ThisHour();

			filter = builder.Eq(x => x.SensorId, sensor.InternalId) & builder.Gte(x => x.Timestamp, startDate) &
					 builder.Lte(x => x.Timestamp, endDate);
			var result = await this._stats.FindAsync(filter).AwaitBackground();

			if(result == null)
				return null;

			return await result.ToListAsync().AwaitBackground();
		}
	}
}