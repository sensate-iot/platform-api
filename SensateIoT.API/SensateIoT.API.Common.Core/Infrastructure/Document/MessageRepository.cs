/*
 * Repository abstraction for the Message model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.DataProcessing;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;

using Message = SensateIoT.API.Common.Data.Models.Message;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public class MessageRepository : AbstractDocumentRepository<Message>, IMessageRepository
	{
		private readonly IGeoQueryService _geoService;

		public MessageRepository(SensateContext ctx, IGeoQueryService geo) : base(ctx.Messages)
		{
			this._geoService = geo;
		}

		public async Task<IEnumerable<Message>> GetAsync(Sensor sensor, DateTime start, DateTime end,
														 int skip = 0, int take = -1,
														 OrderDirection order = OrderDirection.None, CancellationToken ct = default)
		{
			var fb = Builders<Message>.Filter;

			var filter = fb.Eq(m => m.SensorId, sensor.InternalId) &
						 fb.Gte(m => m.Timestamp, start) &
						 fb.Lte(m => m.Timestamp, end);

			var query = this._collection.Find(filter);

			if(order == OrderDirection.Ascending) {
				query = query.SortBy(x => x.Timestamp);
			} else if(order == OrderDirection.Descending) {
				query = query.SortByDescending(x => x.Timestamp);
			}

			if(skip > 0) {
				query = query.Skip(skip);
			}

			if(take > 0) {
				query = query.Limit(take);
			}

			var results = await query.ToListAsync(cancellationToken: ct).AwaitBackground();
			return results;
		}

		public async Task<Message> GetAsync(string messageId, CancellationToken ct = default)
		{
			if(!ObjectId.TryParse(messageId, out var objectId)) {
				return null;
			}

			var filter = Builders<Message>.Filter.Where(x => x.InternalId == objectId);
			var result = await this._collection.FindAsync(filter, cancellationToken: ct).AwaitBackground();

			return await result.FirstOrDefaultAsync(cancellationToken: ct).AwaitBackground();
		}

		public async Task<IEnumerable<Message>> GetMessagesNearAsync(IEnumerable<Sensor> sensors, DateTime start, DateTime end, GeoJsonPoint coords, int max = 100,
		                                                                            int skip = -1, int limit = -1, OrderDirection order = OrderDirection.None,
		                                                                            CancellationToken ct = default)
		{
			var data = await this.GetMessagesBetweenAsync(sensors, start, end, -1, -1, order, ct).ConfigureAwait(false);

			if(data == null) {
				return null;
			}


			return this._geoService.GetMessagesNear(data.ToList(), coords, max, skip, limit, order);
		}

		public async Task<long> CountAsync(IEnumerable<Sensor> sensors,
		                                                   DateTime start,
		                                                   DateTime end,
		                                                   GeoJsonPoint coords = null,
														   int radius = 100,
		                                                   CancellationToken ct = default)
		{
			var ids = new BsonArray();

			foreach(var sensor in sensors) {
				ids.Add(sensor.InternalId);
			}

			var match = new BsonDocument {
				{
					"SensorId", new BsonDocument {
						{"$in", ids}
					}
				}, {
					"Timestamp", new BsonDocument {
						{"$gte", start},
						{"$lt", end}
					}
				}
			};

			if(coords == null) {
				return await this._collection.CountDocumentsAsync(match, cancellationToken:ct).ConfigureAwait(false);
			}

			var cursor = await this._collection.FindAsync(match, cancellationToken: ct).ConfigureAwait(false);
			var messages = await cursor.ToListAsync(ct).ConfigureAwait(false);

			return this._geoService.GetMessagesNear(messages, coords, radius, -1, -1, OrderDirection.None).Count;
		}

		public async Task<IEnumerable<Message>> GetMessagesBetweenAsync(IEnumerable<Sensor> sensors, DateTime start, DateTime end, int skip = -1, int limit = -1,
		                                                                OrderDirection order = OrderDirection.None, CancellationToken ct = default)
		{
			var cur = this.GetCursor(sensors, start, end, skip, limit, order, ct);
			return await cur.ToListAsync(ct).ConfigureAwait(false);
		}

		private IAsyncCursor<Message> GetCursor(IEnumerable<Sensor> sensors, DateTime start, DateTime end,
		                                                  int skip = -1, int limit = -1,
		                                                  OrderDirection order = OrderDirection.None,
		                                                  CancellationToken ct = default)
		{
			var ids = new BsonArray();

			foreach(var sensor in sensors) {
				ids.Add(sensor.InternalId);
			}

			var initialMatch = new BsonDocument {
				{
					"SensorId", new BsonDocument {
						{"$in", ids}
					}
				}, {
					"Timestamp", new BsonDocument {
						{"$gte", start},
						{"$lt", end}
					}
				}
			};

			var pipeline = BuildPipeline(initialMatch, skip, limit, order);
			return  this._collection.Aggregate<Message>(pipeline, cancellationToken: ct);
		}

		private static List<BsonDocument> BuildPipeline(BsonValue initialMatch, int skip, int limit, OrderDirection order)
		{
			var pipeline = new List<BsonDocument> {
				new BsonDocument {{"$match", initialMatch}},
			};

			if(order != OrderDirection.None) {
				var tSort = new BsonDocument {
					{"Timestamp", order.ToInt()}
				};

				var sort = new BsonDocument {
					{"$sort", tSort}
				};

				pipeline.Add(sort);
			}

			if(skip > 0) {
				pipeline.Add(new BsonDocument { { "$skip", skip } });
			}

			if(limit > 0) {
				pipeline.Add(new BsonDocument { { "$limit", limit } });
			}



			return pipeline;
		}

		public async Task DeleteBySensorAsync(Sensor sensor, DateTime start, DateTime end, CancellationToken ct = default)
		{
			var builder = Builders<Message>.Filter;
			var filter = builder.Eq(x => x.SensorId, sensor.InternalId) &
						 builder.Gte(x => x.Timestamp, start) &
						 builder.Lte(x => x.Timestamp, end);

			await this._collection.DeleteManyAsync(filter, ct).AwaitBackground();
		}
	}
}
