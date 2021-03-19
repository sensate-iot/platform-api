using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Driver;

using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Data.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public class SystemStatisticsRepository : AbstractDocumentRepository<SystemStatisticsEntry>, ISystemStatisticsRepository
	{
		public SystemStatisticsRepository(SensateContext ctx) : base(ctx.SystemStatistics)
		{
		}

		public async Task<IEnumerable<SystemStatisticsEntry>> GetBetweenAsync(IList<Sensor> sensors, DateTime start, DateTime end)
		{
			var builder = Builders<SystemStatisticsEntry>.Filter;
			var ids = sensors.Select(x => x.InternalId);
			var startHour = start.ThisHour();
			var endHour = end.ThisHour();
			var filter = builder.In(x => x.Id.SensorId, ids) &
						 builder.Gte(x => x.Id.Timestamp, startHour) &
						 builder.Lte(x => x.Id.Timestamp, endHour);

			var result = await this._collection.FindAsync(filter).AwaitBackground();
			return await result.ToListAsync().AwaitBackground();
		}


		public async Task<IEnumerable<SystemStatisticsEntry>> GetAfterAsync(IEnumerable<Sensor> sensors, DateTime dt)
		{
			FilterDefinition<SystemStatisticsEntry> filter;
			var filterBuilder = Builders<SystemStatisticsEntry>.Filter;
			var date = dt.ThisHour();
			var ids = sensors.Select(x => x.InternalId);

			filter = filterBuilder.In(x => x.Id.SensorId, ids) &
					 filterBuilder.Gte(x => x.Id.Timestamp, date);
			var result = await this._collection.FindAsync(filter).AwaitBackground();

			if(result == null) {
				return null;
			}

			return await result.ToListAsync().AwaitBackground();
		}

		public async Task<IEnumerable<SystemStatisticsEntry>> GetAfterAsync(DateTime date)
		{
			FilterDefinition<SystemStatisticsEntry> filter;
			var filterBuilder = Builders<SystemStatisticsEntry>.Filter;

			filter = filterBuilder.Gte(x => x.Id.Timestamp, date);
			var result = await this._collection.FindAsync(filter).AwaitBackground();

			if(result == null) {
				return null;
			}

			return await result.ToListAsync().AwaitBackground();
		}

		public async Task<IEnumerable<SystemStatisticsEntry>> GetBetweenAsync(Sensor sensor, DateTime start, DateTime end)
		{
			FilterDefinition<SystemStatisticsEntry> filter;

			var builder = Builders<SystemStatisticsEntry>.Filter;
			var startDate = start.ThisHour();
			var endDate = end.ThisHour();

			filter = builder.Eq(x => x.Id.SensorId, sensor.InternalId) &
					 builder.Gte(x => x.Id.Timestamp, startDate) &
					 builder.Lte(x => x.Id.Timestamp, endDate);
			var result = await this._collection.FindAsync(filter).AwaitBackground();

			if(result == null) {
				return null;
			}

			return await result.ToListAsync().AwaitBackground();
		}

	}
}