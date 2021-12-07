/*
 * Sensate database context (MongoDB).
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SensateIoT.API.Common.Data.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public sealed class SensateContext
	{
		private readonly IMongoDatabase _db;
		private readonly IMongoClient _client;

		public IMongoCollection<MeasurementBucket> Measurements => this._db.GetCollection<MeasurementBucket>("Measurements");
		public IMongoCollection<Measurement> MeasurementData => this._db.GetCollection<Measurement>("Measurements");
		public IMongoCollection<Sensor> Sensors => this._db.GetCollection<Sensor>("Sensors");
		public IMongoCollection<Message> Messages => this._db.GetCollection<Message>("Messages");
		public IMongoCollection<ControlMessage> ControlMessages => this._db.GetCollection<ControlMessage>("ControlMessages");
		public IMongoCollection<SystemStatisticsEntry> SystemStatistics => this._db.GetCollection<SystemStatisticsEntry>("SystemStatistics");
		public IMongoCollection<SensorStatisticsEntry> Statistics => this._db.GetCollection<SensorStatisticsEntry>("Statistics");

		public SensateContext(IOptions<MongoDBSettings> options) : this(options.Value)
		{ }

		public SensateContext(MongoDBSettings settings)
		{
			try {
				MongoClientSettings mongosettings = MongoClientSettings.FromUrl(new MongoUrl(
					settings.ConnectionString
				));

				mongosettings.MaxConnectionPoolSize = settings.MaxConnections;
				this._client = new MongoClient(mongosettings);
				this._db = this._client.GetDatabase(settings.DatabaseName);
			} catch(Exception ex) {
				Console.WriteLine($"Unable to connect to MongoDB: {ex.Message}");
				throw;
			}

		}
	}
}
