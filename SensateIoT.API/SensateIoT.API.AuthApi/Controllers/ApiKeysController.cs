﻿/*
 * API key controller.
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
using Microsoft.Extensions.Logging;
using SensateIoT.API.AuthApi.Json;
using SensateIoT.API.Common.ApiCore.Attributes;
using SensateIoT.API.Common.ApiCore.Controllers;
using SensateIoT.API.Common.Core.Helpers;
using SensateIoT.API.Common.Core.Infrastructure.Repositories;
using SensateIoT.API.Common.Core.Services.Processing;
using SensateIoT.API.Common.Data.Dto.Json.Out;
using SensateIoT.API.Common.Data.Enums;
using SensateIoT.API.Common.IdentityData.Enums;
using SensateIoT.API.Common.IdentityData.Models;

namespace SensateIoT.API.AuthApi.Controllers
{
	[Produces("application/json")]
	[Route("auth/v1/[controller]")]
	[NormalUser]
	public class ApiKeysController : AbstractController
	{
		private readonly IApiKeyRepository _keys;
		private readonly ILogger<ApiKeysController> m_logger;
		private readonly ICommandPublisher m_publisher;

		public ApiKeysController(IUserRepository users, IApiKeyRepository keys,
								 ILogger<ApiKeysController> logger,
								 ICommandPublisher publisher,
								 IHttpContextAccessor ctx) : base(users, ctx)
		{
			this._keys = keys;
			this.m_logger = logger;
			this.m_publisher = publisher;
		}

		[HttpPost("create")]
		[ActionName("CreateApiKey")]
		[ProducesResponseType(typeof(SensateApiKey), 200)]
		public async Task<IActionResult> Create([FromBody] CreateApiKey request)
		{
			var key = new SensateApiKey {
				Id = Guid.NewGuid().ToString(),
				UserId = this.CurrentUser.Id,
				CreatedOn = DateTime.UtcNow.ToUniversalTime(),
				Revoked = false,
				Type = ApiKeyType.ApiKey,
				ReadOnly = request.ReadOnly,
				Name = request.Name,
				RequestCount = 0
			};

			await this._keys.CreateAsync(key).AwaitBackground();
			return this.CreatedAtAction("CreateApiKey", key);
		}

		private async Task<IActionResult> RevokeAll(bool systemonly)
		{
			var keys = await this._keys.GetByUserAsync(this.CurrentUser).AwaitBackground();
			IEnumerable<SensateApiKey> sorted;

			sorted = systemonly ? keys.Where(key => key.Revoked == false && key.Type == ApiKeyType.SystemKey) :
				keys.Where(key => key.Revoked == false && (key.Type == ApiKeyType.SystemKey || key.Type == ApiKeyType.ApiKey));

			await this._keys.MarkRevokedRangeAsync(sorted).AwaitBackground();
			return this.Ok();
		}

		[HttpDelete("revoke")]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> Revoke([FromQuery] string id, [FromQuery] string key, [FromQuery] bool system = true)
		{
			SensateApiKey apikey;

			if(string.IsNullOrEmpty(id) && string.IsNullOrEmpty(key)) {
				return await this.RevokeAll(system).AwaitBackground();
			}

			if(id != null) {
				apikey = await this._keys.GetByIdAsync(id).AwaitBackground();
			} else {
				apikey = await this._keys.GetByKeyAsync(key).AwaitBackground();
			}

			if(apikey == null) {
				return this.BadRequest();
			}

			if(apikey.Revoked) {
				return this.BadRequest();
			}

			if(apikey.UserId != this.CurrentUser.Id ||
			   !(apikey.Type == ApiKeyType.ApiKey || apikey.Type == ApiKeyType.SystemKey || apikey.Type == ApiKeyType.SensorKey)) {
				return this.BadRequest();
			}

			if(apikey.Type == ApiKeyType.SensorKey) {
				await this.m_publisher.PublishCommand(CommandType.FlushKey, apikey.ApiKey).AwaitBackground();
			}

			await this._keys.MarkRevokedAsync(apikey).AwaitBackground();
			return this.NoContent();
		}

		[HttpPatch("{key}")]
		[ActionName("RefreshApiKey")]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(typeof(SensateApiKey), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> Refresh(string key)
		{
			var apikey = await this._keys.GetByIdAsync(key).AwaitBackground();

			if(apikey == null) {
				return this.NotFound();
			}

			if(!(apikey.Type == ApiKeyType.ApiKey || apikey.Type == ApiKeyType.SystemKey)) {
				return this.BadRequest();
			}

			apikey = await this._keys.RefreshAsync(apikey).AwaitBackground();
			return this.CreatedAtAction("RefreshApiKey", apikey);
		}

		[HttpPost]
		[ProducesResponseType(typeof(PaginationResult<SensateApiKey>), 200)]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(400)]
		[ProducesResponseType(404)]
		public async Task<IActionResult> Filter([FromBody] ApiKeyFilter filter)
		{
			PaginationResult<SensateApiKey> keys;

			filter ??= new ApiKeyFilter() {
				Limit = 0,
				Skip = 0,
				IncludeRevoked = false
			};

			filter.Limit ??= 0;
			filter.Skip ??= 0;

			if(filter.Types == null || filter.Types.Count <= 0) {
				filter.Types = new List<ApiKeyType> {
					ApiKeyType.ApiKey,
					ApiKeyType.SystemKey
				};
			}

			try {
				keys = await this._keys.FilterAsync(this.CurrentUser, filter.Types, filter.Query, filter.IncludeRevoked,
											  filter.Skip.Value, filter.Limit.Value).AwaitBackground();
			} catch(Exception ex) {
				this.m_logger.LogInformation(ex, "Failed to fetch keys!");

				return this.BadRequest(new Status {
					Message = "Unable to fetch API keys",
					ErrorCode = ReplyCode.UnknownError
				});
			}

			return this.Ok(keys);
		}

		[HttpGet]
		[ProducesResponseType(typeof(IEnumerable<SensateApiKey>), 200)]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(400)]
		public async Task<IActionResult> Index()
		{
			var keys = await this._keys.GetByUserAsync(this.CurrentUser).AwaitBackground();
			return this.Ok(keys);
		}
	}
}
