using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialInfrastructure : Migration
    {
        private static readonly string[] OutboxPendingIndexColumns = new[] { "ProcessedOnUtc", "OccurredOnUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seeding_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeederType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seeding_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages",
                columns: OutboxPendingIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_seeding_history_seeder_type",
                table: "seeding_history",
                column: "SeederType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "seeding_history");
        }
    }
}
