/*
 * MongoDB measurement repository implementation.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;
using SensateIoT.API.Common.Core.Exceptions;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.DataProcessing;
using SensateIoT.API.Common.Data.Dto.Generic;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;

using MeasurementsQueryResult = SensateIoT.API.Common.Data.Models.MeasurementsQueryResult;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public class MeasurementRepository : AbstractDocumentRepository<MeasurementBucket>, IMeasurementRepository
	{
		private readonly IGeoQueryService m_geoService;
		private readonly ILogger<MeasurementRepository> _logger;

		public MeasurementRepository(SensateContext context, IGeoQueryService geo,
									 ILogger<MeasurementRepository> logger) : base(context.Measurements)
		{
			this._logger = logger;
			this.m_geoService = geo;
		}

		public virtual async Task DeleteBucketAsync(Sensor sensor, DateTime bucketStart, DateTime bucketEnd, CancellationToken ct)
		{
			try {
				var builder = Builders<MeasurementBucket>.Filter;
				var filter = builder.Eq(x => x.SensorId, sensor.InternalId) &
							 builder.Gte(x => x.Timestamp, bucketStart.ThisHour()) &
							 builder.Lt(x => x.Timestamp, bucketEnd.ThisHour());

				await this._collection.DeleteManyAsync(filter, ct).ConfigureAwait(false);
			} catch(MongoException ex) {
				this._logger.LogError(ex, "Unable to remove buckets (bucket: {bucketStart:O} - {bucketEnd:O}, sensor: {sensor}).", 
				                      bucketStart, bucketEnd, sensor.InternalId);
				throw new DatabaseException($"Unable to remove {bucketStart:O} - {bucketEnd:O} from database (Sensor: {sensor.InternalId}",
				                            "Measurements", ex);
			}
		}

		public virtual async Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsBetweenAsync(
			IEnumerable<Sensor> sensors,
			DateTime start,
			DateTime end,
			int skip = -1,
			int limit = -1,
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
					"First", new BsonDocument {
						{"$lte", end}
					}
				}, {
					"Last", new BsonDocument {
						{"$gte", start}
					}
				}, {
					"Measurements.Timestamp", new BsonDocument {
						{"$gte", start},
						{"$lte", end}
					}
				}
			};

			var pipeline = BuildPipeline(initialMatch, start, end, skip, limit, order);
			var query = this._collection.Aggregate<MeasurementsQueryResult>(pipeline, cancellationToken: ct);
			var results = await query.ToListAsync(ct).AwaitBackground();

			return results;
		}

		public virtual async Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsNearAsync(
			IEnumerable<Sensor> sensors,
			DateTime start, DateTime end, GeoJsonPoint coords,
			int max = 100,
			int skip = -1,
			int limit = -1,
			OrderDirection order = OrderDirection.None,
			CancellationToken ct = default
		)
		{
			var measurements = await this.GetMeasurementsBetweenAsync(sensors, start, end, ct: ct).AwaitBackground();
			return this.m_geoService.GetMeasurementsNear(measurements.ToList(), coords, max, skip, limit, order, ct);
		}

		public virtual async Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsNearAsync(
			Sensor sensor, DateTime start,
			DateTime end, GeoJsonPoint coords,
			int max = 100,
			int skip = -1,
			int limit = -1,
			OrderDirection order = OrderDirection.None,
			CancellationToken ct = default)
		{
			var measurements = await this.GetMeasurementsBetweenAsync(sensor, start, end, ct: ct).AwaitBackground();
			return this.m_geoService.GetMeasurementsNear(measurements.ToList(), coords, max, skip, limit, order, ct);
		}

		private static List<BsonDocument> BuildPipeline(BsonValue initialMatch, DateTime start, DateTime end, int skip, int limit, OrderDirection order)
		{
			var projectRewrite = new BsonDocument {
				{"_id", 1},
				{"SensorId", 1},
				{"Timestamp", "$Measurements.Timestamp"},
				{"Location", "$Measurements.Location"},
				{"Data", "$Measurements.Data"},
			};

			var timestampMatch = new BsonDocument {
				{
					"Timestamp", new BsonDocument {
						{"$gte", start},
						{"$lt", end}
					}
				}
			};

			var pipeline = new List<BsonDocument> {
				new BsonDocument {{"$match", initialMatch}},
				new BsonDocument {{"$unwind", "$Measurements"}},
				new BsonDocument {{"$project", projectRewrite}},
				new BsonDocument {{"$match", timestampMatch}},
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

		private async Task<IEnumerable<MeasurementsQueryResult>> GetMeasurementsBetweenAsync(
			Sensor sensor, DateTime start, DateTime end,
			int skip = -1, int limit = -1,
			OrderDirection order = OrderDirection.None, CancellationToken ct = default)
		{
			var matchTimestamp = new BsonDocument {
				{
					"SensorId", sensor.InternalId
				}, {
					"First", new BsonDocument {
						{"$lte", end}
					}
				}, {
					"Last", new BsonDocument {
						{"$gte", start}
					}
				}, {
					"Measurements.Timestamp", new BsonDocument {
						{"$gte", start},
						{"$lte", end}
					}
				},
			};

			var pipeline = BuildPipeline(matchTimestamp, start, end, skip, limit, order);
			var query = this._collection.Aggregate<MeasurementsQueryResult>(pipeline, cancellationToken: ct);
			var results = await query.ToListAsync(ct).AwaitBackground();

			return results;
		}

		public virtual async Task<IEnumerable<MeasurementsQueryResult>> GetBetweenAsync(
			Sensor sensor, DateTime start, DateTime end, int skip = -1, int limit = -1, OrderDirection order = OrderDirection.None)
		{
			var data = await this.GetMeasurementsBetweenAsync(sensor, start, end, skip, limit, order).AwaitBackground();
			return data;
		}
	}
}