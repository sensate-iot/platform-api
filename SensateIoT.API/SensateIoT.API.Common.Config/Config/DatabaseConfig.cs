﻿/*
 * Sensate database configuration wrapper.
 *
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

namespace SensateIoT.API.Common.Config.Config
{
	public class DatabaseConfig
	{
		public MongoDBConfig MongoDB { get; set; }
		public PgSQLConfig PgSQL { get; set; }
		public PgSQLConfig Network { get; set; }
		public RedisConfig Redis { get; set; }
	}

	public class MongoDBConfig
	{
		public string DatabaseName { get; set; }
		public string ConnectionString { get; set; }
		public int MaxConnections { get; set; }
	}

	public class PgSQLConfig
	{
		public string ConnectionString { get; set; }
	}

	public class RedisConfig
	{
		public string Host { get; set; }
		public string InstanceName { get; set; }
	}
}