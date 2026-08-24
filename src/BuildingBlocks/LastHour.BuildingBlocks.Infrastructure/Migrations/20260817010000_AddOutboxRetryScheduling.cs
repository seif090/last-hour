using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryScheduling : Migration
    {
        private static readonly string[] OutboxPendingRetryIndexColumns = new[] { "ProcessedOnUtc", "NextAttemptAtUtc", "LockExpiresAtUtc", "OccurredOnUtc" };
        private static readonly string[] OutboxPendingLeaseIndexColumns = new[] { "ProcessedOnUtc", "LockExpiresAtUtc", "OccurredOnUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages",
                columns: OutboxPendingRetryIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages",
                columns: OutboxPendingLeaseIndexColumns);
        }
    }
}
