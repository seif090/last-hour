using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReadStateAndIndexes : Migration
    {
        private static readonly string[] RecipientCreatedAtColumns = new[] { "RecipientId", "CreatedAtUtc", "Id" };
        private static readonly string[] RecipientIsReadCreatedAtColumns = new[] { "RecipientId", "IsRead", "CreatedAtUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAtUtc",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_created_at",
                table: "notifications",
                columns: RecipientCreatedAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_is_read_created_at",
                table: "notifications",
                columns: RecipientIsReadCreatedAtColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_recipient_created_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_recipient_is_read_created_at",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ReadAtUtc",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "notifications");
        }
    }
}
