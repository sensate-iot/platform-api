﻿/*
 * Factory class for the Sensate SQL context.
 * 
 * @author Michel Megens
 * @email  michel.megens@sonatolabs.com
 */

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SensateIoT.API.Common.Config.Config;
using SensateIoT.API.Common.Core.Infrastructure.Sql;

namespace SensateIoT.API.SqlSetup.Application
{
	public class SensateSqlContextFactory : IDesignTimeDbContextFactory<SensateSqlContext>
	{
		private IConfiguration Configuration;

		public SensateSqlContext CreateDbContext(string[] args)
		{
			var builder = new DbContextOptionsBuilder<SensateSqlContext>();
			var db = new DatabaseConfig();

			this.BuildConfiguration();
			this.Configuration.GetSection("Database").Bind(db);

			builder.UseNpgsql(db.PgSQL.ConnectionString, x => x.MigrationsAssembly("SensateIoT.API.SqlSetup"));
			return new SensateSqlContext(builder.Options);
		}

		private void BuildConfiguration()
		{
			var builder = new ConfigurationBuilder();

			builder.SetBasePath(Path.Combine(AppContext.BaseDirectory));
			builder.AddEnvironmentVariables();

			builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
			this.Configuration = builder.Build();
		}
	}
}
