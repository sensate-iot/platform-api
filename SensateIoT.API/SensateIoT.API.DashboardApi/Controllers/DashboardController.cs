﻿/*
 * SensateDashboard index page.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using SensateIoT.API.Common.ApiCore.Attributes;
using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Cache;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.Data.Models;
using SensateIoT.API.Common.IdentityData.Models;
using SensateIoT.API.DashboardApi.Json;

namespace SensateIoT.API.DashboardApi.Controllers
{
	[NormalUser]
	[Produces("application/json")]
	[Route("stats/v1/[controller]")]
	public class DashboardController : AbstractController
	{
		private const int HoursPerDay = 24;

		private readonly ISensorStatisticsRepository _stats;
		private readonly ISensorRepository _sensors;
		private readonly IAuditLogRepository _logs;
		private readonly IUserTokenRepository _tokens;
		private readonly ICacheStrategy<string> m_cache;

		public DashboardController(IUserRepository users,
			ISensorStatisticsRepository stats,
			ISensorRepository sensors,
			IUserTokenRepository tokens,
			IAuditLogRepository logs,
			ICacheStrategy<string> cache,
			IHttpContextAccessor ctx) : base(users, ctx)
		{
			this._stats = stats;
			this._sensors = sensors;
			this._logs = logs;
			this._tokens = tokens;
			this.m_cache = cache;
		}

		[HttpGet]
		[ProducesResponseType(typeof(UserDashboard), 200)]
		[ProducesResponseType(401)]
		[ProducesResponseType(403)]
		public async Task<IActionResult> Index()
		{
			var key = $"dashboard::{this.CurrentUser.Id}";

			var strResult = await this.m_cache.GetAsync(key).AwaitBackground();

			if(strResult != null) {
				return this.Content(strResult, MediaTypeHeaderValue.Parse("application/json"));
			}

			UserDashboard board;
			var raw = await this.GetStatsFor(this.CurrentUser, DateTime.UtcNow.AddMonths(-1)).AwaitBackground();
			List<SensorStatisticsEntry> stats;

			stats = raw?.ToList();

			board = new UserDashboard {
				SensorCount = await this._sensors.CountAsync(this.CurrentUser).AwaitBackground(),
				ApiCallCount = await this.GetApiCallCountAsync().AwaitBackground(),
				MeasurementsTodayCount = await this.CountMeasurementsAsync().AwaitBackground(),
				SecurityTokenCount = await this.CountSecurityTokensAsync(),

				MeasurementsToday = await this.GetMeasurementStatsToday().AwaitBackground(),
				ApiCallsLastWeek = await this.GetApiCallsPerDayAsync().AwaitBackground()
			};

			if(stats != null) {
				board.MeasurementsCumulative = GetMeasurementStatsCumulative(stats);
				board.MeasurementsPerDayCumulative = GetMeasurementStatsCumulativePerDay(stats);
			} else {
				board.MeasurementsCumulative = new Graph<DateTime, long>();
				board.MeasurementsPerDayCumulative = new Graph<int, long>();
			}

			var result = board.ToJson();
			await this.m_cache.SetAsync(key, result.ToString(), CacheTimeout.TimeoutShort.ToInt()).AwaitBackground();

			return this.Ok(result);
		}

		private async Task<long> CountSecurityTokensAsync()
		{
			var count = await this._tokens.CountAsync(token =>
					token.UserId == this.CurrentUser.Id && token.Valid && token.ExpiresAt >= DateTime.UtcNow)
				.AwaitBackground();
			return count;
		}

		private async Task<long> CountMeasurementsAsync()
		{
			var start = DateTime.UtcNow.Date;

			var sensors = await this._sensors.GetAsync(this.CurrentUser).AwaitBackground();
			var stats = await this._stats.GetBetweenAsync(sensors.ToList(), start, DateTime.UtcNow).AwaitBackground();
			return stats.Aggregate(0L, (value, current) => value + current.Count);
		}

		private async Task<long> GetApiCallCountAsync()
		{
			var start = DateTime.UtcNow.AddMonths(-1);
			var logs = await this._logs.CountAsync(entry => entry.AuthorId == this.CurrentUser.Id &&
														  entry.Timestamp >= start &&
														  entry.Timestamp <= DateTime.UtcNow &&
														  (entry.Method == RequestMethod.HttpGet ||
														   entry.Method == RequestMethod.HttpDelete ||
														   entry.Method == RequestMethod.HttpPatch ||
														   entry.Method == RequestMethod.HttpPost ||
														   entry.Method == RequestMethod.HttpPut)).AwaitBackground();
			return logs;
		}

		private async Task<IEnumerable<SensorStatisticsEntry>> GetStatsFor(SensateUser user, DateTime date)
		{
			var raw = await this._sensors.GetAsync(user).AwaitBackground();
			var sensors = raw.ToList();

			if(sensors.Count <= 0) {
				return null;
			}

			return await this._stats.GetAfterAsync(sensors, date).AwaitBackground();
		}

		private async Task<Graph<DateTime, long>> GetApiCallsPerDayAsync()
		{
			Graph<DateTime, long> graph;
			var lastweek = DateTime.UtcNow.Date.AddDays(-DaysPerWeek + 1);
			var start = lastweek;

			var logs = await this._logs.GetAsync(entry => entry.AuthorId == this.CurrentUser.Id &&
														  entry.Timestamp >= start &&
														  entry.Timestamp <= DateTime.UtcNow &&
														  (entry.Method == RequestMethod.HttpGet ||
														   entry.Method == RequestMethod.HttpDelete ||
														   entry.Method == RequestMethod.HttpPatch ||
														   entry.Method == RequestMethod.HttpPost ||
														   entry.Method == RequestMethod.HttpPut)).AwaitBackground();
			var data = logs.GroupBy(log => log.Timestamp.Date).Select(grp => new {
				DayOfMonth = grp.Key,
				Count = grp.AsEnumerable().Count()
			}).ToList();

			graph = new Graph<DateTime, long>();

			for(var idx = 0; idx < DaysPerWeek; lastweek = lastweek.AddDays(1), idx++) {
				var count = data.SingleOrDefault(x => x.DayOfMonth == lastweek);

				if(count == null) {
					graph.Add(lastweek, 0L);
					continue;
				}

				graph.Add(count.DayOfMonth, count.Count);
			}

			return graph;
		}

		private const int DaysPerWeek = 7;

		private static long AccumulateStatisticsEntries(IEnumerable<SensorStatisticsEntry> entries)
		{
			return entries.Aggregate(0L, (current, entry) => current + entry.Count);
		}

		private static Graph<int, long> GetMeasurementStatsCumulativePerDay(IEnumerable<SensorStatisticsEntry> statistics)
		{
			Graph<int, long> data;

			var entries = statistics.GroupBy(entry => entry.Timestamp)
				.Select(grp => new { DayOfWeek = (int)grp.Key.DayOfWeek, Count = AccumulateStatisticsEntries(grp.AsEnumerable()) }).ToList();

			data = new Graph<int, long>();

			for(var idx = 0; idx < DaysPerWeek; idx++) {
				var entry = entries.Where(e => e.DayOfWeek == idx).ToList();
				var count = entry.Aggregate(0L, (current, value) => current + value.Count);

				data.Add(idx, count);
			}

			return data;
		}

		private static Graph<DateTime, long> GetMeasurementStatsCumulative(IEnumerable<SensorStatisticsEntry> statistics)
		{
			Graph<DateTime, long> graph;
			Dictionary<DateTime, long> totals;
			long counter;

			graph = new Graph<DateTime, long>();
			totals = new Dictionary<DateTime, long>();
			counter = 0L;

			var grouped = statistics.GroupBy(entry => entry.Timestamp).Select(grp => new {
				grp.Key.Date,
				Count = grp.AsEnumerable().Aggregate(0L, (current, entry) => current + entry.Count)
			}).ToList();

			foreach(var entry in grouped) {
				counter += entry.Count;
				totals[entry.Date] = counter;
			}

			var sorted = totals.Keys.AsEnumerable().OrderBy(value => value).ToArray();

			if(sorted.Length <= 0) {
				return graph;
			}

			graph.Add(sorted[0].Date.AddDays(-1), 0L);

			foreach(var key in sorted) {
				graph.Add(key, totals[key]);
			}

			return graph;
		}

		private async Task<Graph<DateTime, long>> GetMeasurementStatsToday()
		{
			DateTime today;
			Graph<DateTime, long> graph;
			Dictionary<long, long> totals;

			today = DateTime.Now.AddHours(-23D).ToUniversalTime().ThisHour();
			graph = new Graph<DateTime, long>();
			totals = new Dictionary<long, long>();
			var measurements = await this.GetStatsFor(this.CurrentUser, today).AwaitBackground();

			if(measurements == null) {
				return graph;
			}

			foreach(var entry in measurements) {
				if(!totals.TryGetValue(entry.Timestamp.Ticks, out var value)) {
					value = 0L;
				}

				value += entry.Count;
				totals[entry.Timestamp.Ticks] = value;
			}

			for(var idx = 0; idx < HoursPerDay; idx++) {
				if(!totals.TryGetValue(today.Ticks, out var value)) {
					value = 0L;
				}

				graph.Add(today, value);
				today = today.AddHours(1D);
			}

			return graph;
		}
	}
}