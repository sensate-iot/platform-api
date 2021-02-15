﻿/*
 * API key data layer implementation.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Models;
using SensateIoT.API.Common.IdentityData.Enums;
using SensateIoT.API.Common.IdentityData.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Sql
{
	public class ApiKeyRepository : AbstractSqlRepository<SensateApiKey>, IApiKeyRepository
	{
		private readonly Random _rng;
		private const int UserTokenLength = 32;

		public ApiKeyRepository(SensateSqlContext context) : base(context)
		{
			this._rng = new Random(StaticRandom.Next());
		}

		public override Task CreateAsync(SensateApiKey key, CancellationToken token = default)
		{
			key.ApiKey = this._rng.NextStringWithSymbols(UserTokenLength);
			key.CreatedOn = DateTime.UtcNow;

			this.Data.Add(key);
			return this._sqlContext.SaveChangesAsync(token);
		}

		public virtual async Task CreateSensorKey(SensateApiKey key, Sensor sensor, CancellationToken token = default)
		{
			if(string.IsNullOrEmpty(sensor.Secret)) {
				key.ApiKey = this._rng.NextStringWithSymbols(UserTokenLength);
				sensor.Secret = key.ApiKey;
			} else {
				key.ApiKey = sensor.Secret;
			}

			key.UserId = sensor.Owner;
			key.Id = Guid.NewGuid().ToString();
			key.Revoked = false;
			key.Type = ApiKeyType.SensorKey;
			key.Name = sensor.Name;
			key.Revoked = key.ReadOnly = false;
			key.CreatedOn = DateTime.UtcNow;

			this.Data.Add(key);
			await this.CommitAsync(token).AwaitBackground();
		}

		public async Task<SensateApiKey> GetAsync(string key, CancellationToken token = default)
		{
			var query = this.Data.Where(k => k.ApiKey == key);
			var apikey = await query.FirstOrDefaultAsync(token);

			if(apikey == null) {
				return null;
			}

			apikey.CreatedOn = DateTime.SpecifyKind(apikey.CreatedOn, DateTimeKind.Utc);
			return apikey;
		}

		public async Task<SensateApiKey> GetByKeyAsync(string key, CancellationToken token = default)
		{
			var query = this.Data.Where(apikey => apikey.ApiKey == key).Include(apikey => apikey.User)
				.ThenInclude(user => user.ApiKeys);
			var _apikey = await query.FirstOrDefaultAsync(token).AwaitBackground();

			if(_apikey == null)
				return null;

			_apikey.CreatedOn = DateTime.SpecifyKind(_apikey.CreatedOn, DateTimeKind.Utc);

			return _apikey;
		}

		public async Task<SensateApiKey> GetByIdAsync(string id, CancellationToken token = default)
		{
			var query = this.Data.Where(apikey => apikey.Id == id).Include(apikey => apikey.User)
				.ThenInclude(user => user.ApiKeys);
			var _apikey = await query.FirstOrDefaultAsync(token).AwaitBackground();

			if(_apikey == null)
				return null;

			_apikey.CreatedOn = DateTime.SpecifyKind(_apikey.CreatedOn, DateTimeKind.Utc);

			return _apikey;
		}

		public async Task<PaginationResult<SensateApiKey>> FilterAsync(SensateUser user,
																	   IList<ApiKeyType> types,
																	   string query = null,
																	   bool revoked = true,
																	   int skip = 0,
																	   int limit = 0)
		{
			var rv = new PaginationResult<SensateApiKey>();
			var q = this.Data.Where(x => x.UserId == user.Id && types.Contains(x.Type));

			if(!string.IsNullOrEmpty(query)) {
				q = q.Where(x => x.Name.Contains(query));
			}

			if(!revoked) {
				q = q.Where(x => !x.Revoked);
			}

			rv.Count = await q.CountAsync().AwaitBackground();

			if(skip > 0) {
				q = q.Skip(skip);
			}

			if(limit > 0) {
				q = q.Take(limit);
			}

			q = q.Include(key => key.User);

			rv.Values = await q.ToListAsync().AwaitBackground();

			foreach(var key in rv.Values) {
				key.CreatedOn = DateTime.SpecifyKind(key.CreatedOn, DateTimeKind.Utc);
			}

			return rv;
		}

		public Task IncrementRequestCountAsync(SensateApiKey key, CancellationToken ct = default)
		{
			this.StartUpdate(key);
			key.RequestCount += 1;
			return this.EndUpdateAsync(ct);
		}

		public async Task DeleteAsync(SensateUser user, CancellationToken ct = default)
		{
			var query = this.Data.Where(x => x.UserId == user.Id);
			this.Data.RemoveRange(query);

			await this.CommitAsync(ct).AwaitBackground();
		}

		public async Task DeleteAsync(string key, CancellationToken ct = default)
		{
			var apiKey = this.Data.Where(apikey => apikey.ApiKey == key);

			this.Data.RemoveRange(apiKey);
			await this.CommitAsync(ct).AwaitBackground();
		}

		public Task MarkRevokedAsync(SensateApiKey key, CancellationToken token = default)
		{
			this.StartUpdate(key);
			key.Revoked = true;
			return this.EndUpdateAsync(token);
		}

		public Task MarkRevokedRangeAsync(IEnumerable<SensateApiKey> keys, CancellationToken token = default)
		{
			var apikeys = keys.ToList();

			if(apikeys.Count <= 0)
				return Task.CompletedTask;

			this._sqlContext.UpdateRange(apikeys);

			foreach(var key in apikeys) {
				key.Revoked = true;
			}

			return this._sqlContext.SaveChangesAsync(token);
		}

		public async Task<SensateApiKey> RefreshAsync(SensateApiKey apikey, string key, CancellationToken token = default)
		{
			this.StartUpdate(apikey);
			apikey.ApiKey = key;
			await this.EndUpdateAsync(token).AwaitBackground();

			return apikey;
		}

		public async Task<SensateApiKey> RefreshAsync(SensateApiKey apikey, CancellationToken token = default)
		{
			this.StartUpdate(apikey);
			apikey.ApiKey = this._rng.NextStringWithSymbols(UserTokenLength);
			await this.EndUpdateAsync(token).AwaitBackground();

			return apikey;
		}

		public async Task<SensateApiKey> RefreshAsync(string id, CancellationToken token = default)
		{
			var apikey = await this.GetByIdAsync(id, token).AwaitBackground();

			if(apikey == null || apikey.Revoked)
				return null;

			return await this.RefreshAsync(apikey, token).AwaitBackground();
		}

		public string GenerateApiKey()
		{
			return this._rng.NextStringWithSymbols(UserTokenLength);
		}

		public async Task<IEnumerable<SensateApiKey>> GetByUserAsync(SensateUser user, int skip = 0, int limit = 0, CancellationToken token = default)
		{
			IQueryable<SensateApiKey> query = this.Data.Where(apikey => apikey.UserId == user.Id).Include(apikey => apikey.User)
				.ThenInclude(u => u.ApiKeys);

			if(skip > 0) {
				query = query.Skip(skip);
			}

			if(limit > 0) {
				query = query.Take(limit);
			}

			var keys = await query.ToListAsync(token).AwaitBackground();

			if(keys == null) {
				return null;
			}

			foreach(var key in keys) {
				key.CreatedOn = DateTime.SpecifyKind(key.CreatedOn, DateTimeKind.Utc);
			}

			return keys;
		}

		public async Task<IEnumerable<SensateApiKey>> GetByUserAsync(SensateUser user, ApiKeyType type,
																	 int skip = 0, int limit = 0,
																	 CancellationToken token = default)
		{
			IQueryable<SensateApiKey> query = this.Data.Where(apikey => apikey.UserId == user.Id && apikey.Type == type).Include(apikey => apikey.User)
				.ThenInclude(u => u.ApiKeys);

			if(skip > 0) {
				query = query.Skip(skip);
			}

			if(limit > 0) {
				query = query.Take(limit);
			}

			var keys = await query.ToListAsync(token).AwaitBackground();

			if(keys == null) {
				return null;
			}

			foreach(var key in keys) {
				key.CreatedOn = DateTime.SpecifyKind(key.CreatedOn, DateTimeKind.Utc);
			}

			return keys;
		}
	}
}
