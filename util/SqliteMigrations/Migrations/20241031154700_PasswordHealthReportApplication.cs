﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.SqliteMigrations.Migrations;

/// <inheritdoc />
public partial class PasswordHealthReportApplication : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PasswordHealthReportApplications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OrganizationId = table.Column<Guid>(type: "TEXT", nullable: true),
                Uri = table.Column<string>(type: "TEXT", nullable: true),
                CreationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                RevisionDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordHealthReportApplications", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordHealthReportApplications_Organization_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organization",
                    principalColumn: "Id");
            });
        migrationBuilder.CreateIndex(
            name: "IX_PasswordHealthReportApplications_OrganizationId",
            table: "PasswordHealthReportApplications",
            column: "OrganizationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PasswordHealthReportApplications");
    }
}
