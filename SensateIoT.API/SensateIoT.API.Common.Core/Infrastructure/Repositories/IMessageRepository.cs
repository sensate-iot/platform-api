/*
 * Repository abstraction for the Message model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;

using Message = SensateIoT.API.Common.Data.Models.Message;

namespace SensateIoT.API.Common.Core.Infrastructure.Repositories
{
	public interface IMessageRepository
	{
		Task<IEnumerable<Message>> GetAsync(Sensor sensor, DateTime start, DateTime end, int skip = 0, int take = -1, OrderDirection order = OrderDirection.None, CancellationToken ct = default);
		Task<Message> GetAsync(string messageId, CancellationToken ct = default);
		Task<IEnumerable<Message>> GetMessagesNearAsync(IEnumerable<Sensor> sensors,
		                                                               DateTime start, 
		                                                               DateTime end, 
		                                                               GeoJsonPoint coords, 
		                                                               int max = 100,
		                                                               int skip = -1,
		                                                               int limit = -1, 
		                                                               OrderDirection order = OrderDirection.None,
		                                                               CancellationToken ct = default);
		Task<long> CountAsync(IEnumerable<Sensor> sensors,
		                                                               DateTime start, 
		                                                               DateTime end, 
		                                                               GeoJsonPoint coords = null,
		                                                               int radius = 100,
		                                                               CancellationToken ct = default);
		Task<IEnumerable<Message>> GetMessagesBetweenAsync(IEnumerable<Sensor> sensors,
																			   DateTime start, DateTime end,
																			   int skip = -1, int limit = -1,
																			   OrderDirection order = OrderDirection.None,
																			   CancellationToken ct = default);
		Task DeleteBySensorAsync(Sensor sensor, DateTime start, DateTime end, CancellationToken ct = default);
	}
}

