/*
 * MongoDB settings.
 *
 * @author: Michel Megens
 * @email:  michel.megens@sonatolabs.com
 */

namespace SensateIoT.API.Common.Core.Infrastructure.Document
{
	public class MongoDBSettings
	{
		public string ConnectionString { get; set; }
		public string DatabaseName { get; set; }
		public int MaxConnections { get; set; }
	}
}
