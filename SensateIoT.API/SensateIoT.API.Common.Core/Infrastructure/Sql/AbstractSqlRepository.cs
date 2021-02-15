/*
 * Abstract SQL repository
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SensateIoT.API.Common.Core.Helpers;

namespace SensateIoT.API.Common.Core.Infrastructure.Sql
{
	public abstract class AbstractSqlRepository<T> : IBulkWriter<T>, IDisposable where T : class
	{
		protected readonly SensateSqlContext _sqlContext;
		protected DbSet<T> Data;

		protected AbstractSqlRepository(SensateSqlContext context)
		{
			this._sqlContext = context;
			this.Data = context.Set<T>();
		}

		public void Commit(T obj)
		{
			this._sqlContext.SaveChanges();
		}

		public async Task CommitAsync(T obj, CancellationToken ct = default)
		{
			await this._sqlContext.SaveChangesAsync(ct).AwaitBackground();
		}

		public async Task CommitAsync(CancellationToken ct = default)
		{
			await this._sqlContext.SaveChangesAsync(ct).AwaitBackground();
		}

		public virtual void Commit()
		{
			this._sqlContext.SaveChanges();
		}

		public virtual void StartUpdate(T obj)
		{
			this._sqlContext.Update(obj);
		}

		public virtual async Task EndUpdateAsync(CancellationToken token = default)
		{
			await this._sqlContext.SaveChangesAsync(token).AwaitBackground();
		}

		public void EndUpdate()
		{
			this._sqlContext.SaveChanges();
		}

		public virtual void Create(T obj)
		{
			this.Data.Add(obj);
			this.Commit();
		}

		public virtual async Task CreateAsync(T obj, CancellationToken ct = default)
		{
			this.Data.Add(obj);
			await this.CommitAsync(ct).AwaitBackground();
		}

		public void AddRange(IEnumerable<T> objs)
		{
			this.Data.AddRange(objs);
		}

		public virtual async Task CreateRangeAsync(IEnumerable<T> objs, CancellationToken token)
		{
			this.AddRange(objs);
			await this.CommitAsync(token).AwaitBackground();
		}

		public void Dispose()
		{
			this._sqlContext?.Dispose();
		}
	}
}
