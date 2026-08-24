using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRetentionAndAdminIndexes : Migration
    {
        private static readonly string[] DeliveryStatusChannelCreatedCols = new[] { "Status", "Channel", "CreatedAtUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_created_at",
                table: "notification_delivery_attempts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_status_channel_created_at",
                table: "notification_deliveries",
                columns: DeliveryStatusChannelCreatedCols);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_delivery_attempts_created_at",
                table: "notification_delivery_attempts");

            migrationBuilder.DropIndex(
                name: "IX_notification_deliveries_status_channel_created_at",
                table: "notification_deliveries");
        }
    }
}
