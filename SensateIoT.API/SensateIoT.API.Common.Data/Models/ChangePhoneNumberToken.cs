﻿/*
 * Change phone number token bridge.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SensateIoT.API.Common.Data.Models
{
	[Table("PhoneNumberTokens")]
	public class ChangePhoneNumberToken
	{
		[Key]
		public string IdentityToken { get; set; }
		[Key]
		public string PhoneNumber { get; set; }
		public string UserToken { get; set; }
		public string UserId { get; set; }
		public DateTime Timestamp { get; set; }
	}
}