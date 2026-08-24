using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationBusinessIdempotencyIndex : Migration
    {
        private static readonly string[] NotificationEventIdTypeColumns = new[] { "EventId", "Type" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_notifications_event_id_type",
                table: "notifications",
                columns: NotificationEventIdTypeColumns,
                unique: true,
                filter: "\"EventId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_event_id_type",
                table: "notifications");
        }
    }
}
