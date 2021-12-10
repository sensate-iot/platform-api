using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SensateIoT.API.SqlSetup.Migrations
{
	public partial class UpdateTimestampColumns : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("SET TimeZone='UTC'");

			migrationBuilder.DropTable(
				name: "Blobs");

			migrationBuilder.DropTable(
				name: "SensorLinks");

			migrationBuilder.DropTable(
				name: "TriggerActions");

			migrationBuilder.DropTable(
				name: "TriggerInvocations");

			migrationBuilder.DropTable(
				name: "Triggers");

			migrationBuilder.AlterColumn<DateTime>(
				name: "RegisteredAt",
				table: "Users",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "Timestamp",
				table: "PhoneNumberTokens",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ExpiresAt",
				table: "AuthTokens",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "CreatedAt",
				table: "AuthTokens",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "Timestamp",
				table: "AuditLogs",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "CreatedOn",
				table: "ApiKeys",
				type: "timestamp with time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp without time zone");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("SET TimeZone='UTC'");

			migrationBuilder.AlterColumn<DateTime>(
				name: "RegisteredAt",
				table: "Users",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "Timestamp",
				table: "PhoneNumberTokens",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "ExpiresAt",
				table: "AuthTokens",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "CreatedAt",
				table: "AuthTokens",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "Timestamp",
				table: "AuditLogs",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.AlterColumn<DateTime>(
				name: "CreatedOn",
				table: "ApiKeys",
				type: "timestamp without time zone",
				nullable: false,
				oldClrType: typeof(DateTime),
				oldType: "timestamp with time zone");

			migrationBuilder.CreateTable(
				name: "Blobs",
				columns: table => new {
					Id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					FileName = table.Column<string>(type: "text", nullable: false),
					FileSize = table.Column<long>(type: "bigint", nullable: false),
					Path = table.Column<string>(type: "text", nullable: false),
					SensorId = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
					StorageType = table.Column<int>(type: "integer", nullable: false),
					Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
				},
				constraints: table => {
					table.PrimaryKey("PK_Blobs", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "SensorLinks",
				columns: table => new {
					UserId = table.Column<string>(type: "text", nullable: false),
					SensorId = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table => {
					table.PrimaryKey("PK_SensorLinks", x => new { x.UserId, x.SensorId });
					table.ForeignKey(
						name: "FK_SensorLinks_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Triggers",
				columns: table => new {
					Id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					FormalLanguage = table.Column<string>(type: "text", nullable: true),
					KeyValue = table.Column<string>(type: "text", nullable: false),
					LowerEdge = table.Column<decimal>(type: "numeric", nullable: true),
					SensorId = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
					Type = table.Column<int>(type: "integer", nullable: false),
					UpperEdge = table.Column<decimal>(type: "numeric", nullable: true)
				},
				constraints: table => {
					table.PrimaryKey("PK_Triggers", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "TriggerActions",
				columns: table => new {
					TriggerId = table.Column<long>(type: "bigint", nullable: false),
					Channel = table.Column<int>(type: "integer", nullable: false),
					Message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
					Target = table.Column<string>(type: "text", nullable: true)
				},
				constraints: table => {
					table.PrimaryKey("PK_TriggerActions", x => new { x.TriggerId, x.Channel });
					table.ForeignKey(
						name: "FK_TriggerActions_Triggers_TriggerId",
						column: x => x.TriggerId,
						principalTable: "Triggers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "TriggerInvocations",
				columns: table => new {
					Id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
					TriggerId = table.Column<long>(type: "bigint", nullable: false)
				},
				constraints: table => {
					table.PrimaryKey("PK_TriggerInvocations", x => x.Id);
					table.ForeignKey(
						name: "FK_TriggerInvocations_Triggers_TriggerId",
						column: x => x.TriggerId,
						principalTable: "Triggers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Blobs_SensorId",
				table: "Blobs",
				column: "SensorId");

			migrationBuilder.CreateIndex(
				name: "IX_Blobs_SensorId_FileName",
				table: "Blobs",
				columns: new[] { "SensorId", "FileName" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_SensorLinks_UserId",
				table: "SensorLinks",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_TriggerInvocations_TriggerId",
				table: "TriggerInvocations",
				column: "TriggerId");

			migrationBuilder.CreateIndex(
				name: "IX_Triggers_SensorId",
				table: "Triggers",
				column: "SensorId");

			migrationBuilder.CreateIndex(
				name: "IX_Triggers_Type",
				table: "Triggers",
				column: "Type");
		}
	}
}
