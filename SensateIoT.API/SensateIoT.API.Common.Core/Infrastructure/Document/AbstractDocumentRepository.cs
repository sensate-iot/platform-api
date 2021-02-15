/*
 * Abstract document repository
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using SensateIoT.API.Common.Core.Helpers;

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public abstract class AbstractDocumentRepository<T>
	{
		protected readonly IMongoCollection<T> _collection;

		protected AbstractDocumentRepository(IMongoCollection<T> collection)
		{
			this._collection = collection;
		}

		protected ObjectId GenerateId(DateTime? ts)
		{
			DateTime timestamp;

			timestamp = ts ?? DateTime.Now;
			return ObjectId.GenerateNewId(timestamp);
		}

		public virtual async Task CreateAsync(T obj, CancellationToken ct = default)
		{
			await this._collection.InsertOneAsync(obj, default, ct).AwaitBackground();
		}
	}
}
