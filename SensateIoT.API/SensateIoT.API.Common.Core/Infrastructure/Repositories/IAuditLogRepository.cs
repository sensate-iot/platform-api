﻿/*
 * AuditLog database coupling.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;
using SensateIoT.API.Common.IdentityData.Models;

namespace SensateIoT.API.Common.Core.Infrastructure.Repositories
{
	public interface IAuditLogRepository
	{
		Task<PaginationResult<AuditLog>> GetByUserAsync(SensateUser user, int skip = 0, int limit = 0, OrderDirection direction = OrderDirection.None);
		Task<PaginationResult<AuditLog>> GetByRequestTypeAsync(SensateUser user, RequestMethod method, int skip = 0, int limit = 0, OrderDirection direction = OrderDirection.None);
		Task<IEnumerable<AuditLog>> GetAsync(Expression<Func<AuditLog, bool>> expr);
		Task<AuditLog> GetAsync(long id);
		Task<PaginationResult<AuditLog>> GetAllAsync(RequestMethod method, int skip = 0, int limit = 0, OrderDirection direction = OrderDirection.None);

		Task<int> CountAsync(Expression<Func<AuditLog, bool>> predicate = null);

		Task<PaginationResult<AuditLog>> FindAsync(string text, RequestMethod method = RequestMethod.Any, int skip = 0, int limit = 0, OrderDirection direction = OrderDirection.None);
		Task<PaginationResult<AuditLog>> FindAsync(IEnumerable<string> uids, string text, RequestMethod method = RequestMethod.Any, int skip = 0, int limit = 0, OrderDirection direction = OrderDirection.None);

		Task CreateAsync(string route, RequestMethod method, IPAddress address, SensateUser user = null);
		Task CreateAsync(AuditLog log, CancellationToken ct = default);

		Task DeleteBetweenAsync(SensateUser user, DateTime start, DateTime end);
		Task DeleteBetweenAsync(SensateUser user, string route, DateTime start, DateTime end);
		Task DeleteAsync(IEnumerable<long> ids, CancellationToken ct = default);
	}
}
