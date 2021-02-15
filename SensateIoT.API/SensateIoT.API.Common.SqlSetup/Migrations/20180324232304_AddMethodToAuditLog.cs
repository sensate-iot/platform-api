﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace SensateIoT.API.SqlSetup.Migrations
{
	public partial class AddMethodToAuditLog : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "Method",
				table: "AuditLogs",
				nullable: false,
				defaultValue: 0);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "Method",
				table: "AuditLogs");
		}
	}
}
