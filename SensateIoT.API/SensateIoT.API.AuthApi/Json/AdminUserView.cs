﻿/*
 * JSON view for admin lookups.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System;
using SensateIoT.API.Common.Data.Dto.Json.Out;

namespace SensateIoT.API.AuthApi.Json
{
	public class AdminUserView : User
	{
		public string UnconfirmedPhoneNumber { get; set; }
		public bool BillingLockout { get; set; }
		public DateTime? PasswordLockout { get; set; }
		public bool PasswordLockoutEnabled { get; set; }
		public bool EmailConfirmed { get; set; }
	}
}