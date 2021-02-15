/*
 * Request JWT token reply viewmodel.
 *
 * @author Michel Megens
 * @email   michel.megens@sonatolabs.com
 */

namespace SensateIoT.API.Common.Data.Dto.Json.Out
{
	public class TokenRequestReply
	{
		public string RefreshToken { get; set; }
		public string JwtToken { get; set; }
		public int ExpiresInMinutes { get; set; }
		public int JwtExpiresInMinutes { get; set; }
		public string SystemApiKey { get; set; }
	}
}
