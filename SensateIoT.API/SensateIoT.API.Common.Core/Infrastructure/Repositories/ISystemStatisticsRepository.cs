using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SensateIoT.API.Common.Data.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Repositories
{
	public interface ISystemStatisticsRepository
	{
		Task<IEnumerable<SystemStatisticsEntry>> GetAfterAsync(IEnumerable<Sensor> sensors, DateTime dt);
		Task<IEnumerable<SystemStatisticsEntry>> GetAfterAsync(DateTime date);
		Task<IEnumerable<SystemStatisticsEntry>> GetBetweenAsync(Sensor sensor, DateTime start, DateTime end);
		Task<IEnumerable<SystemStatisticsEntry>> GetBetweenAsync(IList<Sensor> sensors, DateTime start, DateTime end);
	}
}