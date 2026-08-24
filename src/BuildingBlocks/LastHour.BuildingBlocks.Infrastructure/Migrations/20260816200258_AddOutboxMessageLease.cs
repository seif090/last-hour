using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageLease : Migration
    {
        private static readonly string[] OutboxPendingLeaseIndexColumns = new[] { "ProcessedOnUtc", "LockExpiresAtUtc", "OccurredOnUtc" };
        private static readonly string[] OutboxPendingIndexColumns = new[] { "ProcessedOnUtc", "OccurredOnUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimToken",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockExpiresAtUtc",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "merchants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateOrProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages",
                columns: OutboxPendingLeaseIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_merchants_name",
                table: "merchants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_merchants_owner_id",
                table: "merchants",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_merchants_status",
                table: "merchants",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merchants");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "ClaimToken",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "LockExpiresAtUtc",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_pending",
                table: "outbox_messages",
                columns: OutboxPendingIndexColumns);
        }
    }
}
