using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixHook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PayloadTemplateJson = table.Column<string>(type: "TEXT", nullable: false),
                    HttpHeadersTemplateJson = table.Column<string>(type: "TEXT", nullable: false),
                    SignatureStrategyKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTemplates_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DestinationUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    RenderedPayload = table.Column<string>(type: "TEXT", nullable: false),
                    RenderedHeaders = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ResponseBody = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ReplayOfDispatchId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispatches_Dispatches_ReplayOfDispatchId",
                        column: x => x.ReplayOfDispatchId,
                        principalTable: "Dispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dispatches_EventTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EventTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_ReplayOfDispatchId",
                table: "Dispatches",
                column: "ReplayOfDispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_RequestedAt",
                table: "Dispatches",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_TemplateId",
                table: "Dispatches",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTemplates_ProviderId",
                table: "EventTemplates",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Slug",
                table: "Providers",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Dispatches");
            migrationBuilder.DropTable(name: "EventTemplates");
            migrationBuilder.DropTable(name: "Providers");
        }
    }
}
