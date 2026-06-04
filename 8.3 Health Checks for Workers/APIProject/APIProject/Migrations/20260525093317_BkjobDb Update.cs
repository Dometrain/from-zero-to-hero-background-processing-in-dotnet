using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIProject.Migrations
{
    /// <inheritdoc />
    public partial class BkjobDbUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DropTable(
                name: "BackgroundJobs");
            
            migrationBuilder.CreateTable(
                name: "BackgroundJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    LastErrorType = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", nullable: false),
                    TransitionedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobTransitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", nullable: false),
                    Sent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailNotifications_DocumentId_Type",
                table: "EmailNotifications",
                columns: new[] { "DocumentId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobs");

            migrationBuilder.DropTable(
                name: "BackgroundJobTransitions");

            migrationBuilder.DropTable(
                name: "EmailNotifications");
        }
    }
}
