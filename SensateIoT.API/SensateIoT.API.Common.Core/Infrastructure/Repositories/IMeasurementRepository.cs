/*
 * Abstract measurement repository
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;

using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;

using MeasurementModel = SensateIoT.API.Common.Data.Models.Measurement;
using MeasurementsQueryResult = SensateIoT.API.Common.Data.Models.MeasurementsQueryResult;

namespace SensateIoT.API.Common.Core.Infrastructure.Repositories
{
	using MeasurementMap = IDictionary<ObjectId, List<MeasurementModel>>;

	public interface IMeasurementRepository
	{
		Task<IEnumerable<MeasurementsQueryResult>> GetBetweenAsync(Sensor sensor, DateTime start, DateTime end,
																   int skip = -1, int limit = -1,
																   OrderDirection order = OrderDirection.None);
		Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsBetweenAsync(IEnumerable<Sensor> sensors,
																			   DateTime start, DateTime end,
																			   int skip = -1, int limit = -1,
																			   OrderDirection order = OrderDirection.None,
																			   CancellationToken ct = default);

		Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsNearAsync(Sensor sensor, DateTime start, DateTime end, GeoJsonPoint coords,
			int max = 100, int skip = -1, int limit = -1, OrderDirection order = OrderDirection.None, CancellationToken ct = default);

		Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsNearAsync(IEnumerable<Sensor> sensors, DateTime start, DateTime end, GeoJsonPoint coords,
			int max = 100, int skip = -1, int limit = -1, OrderDirection order = OrderDirection.None, CancellationToken ct = default);

		Task DeleteBetweenAsync(Sensor sensor, DateTime start, DateTime end, CancellationToken ct = default);
	}
}
