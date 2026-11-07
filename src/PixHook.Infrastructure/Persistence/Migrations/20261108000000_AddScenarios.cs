using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixHook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DelayBeforeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioSteps_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScenarioSteps_EventTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EventTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scenarios_Slug",
                table: "Scenarios",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSteps_ScenarioId",
                table: "ScenarioSteps",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSteps_TemplateId",
                table: "ScenarioSteps",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScenarioSteps");
            migrationBuilder.DropTable(name: "Scenarios");
        }
    }
}
