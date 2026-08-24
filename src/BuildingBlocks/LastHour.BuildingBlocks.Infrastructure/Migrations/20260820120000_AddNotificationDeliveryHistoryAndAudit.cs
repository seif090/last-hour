using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDeliveryHistoryAndAudit : Migration
    {
        private static readonly string[] AttemptDeliveryIdAttemptNumCols = new[] { "DeliveryId", "AttemptNumber" };
        private static readonly string[] AttemptDeliveryIdStartedAtCols = new[] { "DeliveryId", "StartedAtUtc" };
        private static readonly string[] AuditDeliveryIdOccurredAtCols = new[] { "DeliveryId", "OccurredAtUtc" };
        private static readonly string[] AuditActorIdOccurredAtCols = new[] { "ActorId", "OccurredAtUtc" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_delivery_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FailureType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_attempts_notification_deliveries_De~",
                        column: x => x.DeliveryId,
                        principalTable: "notification_deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_delivery_id_attempt_number",
                table: "notification_delivery_attempts",
                columns: AttemptDeliveryIdAttemptNumCols,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_delivery_id_started_at",
                table: "notification_delivery_attempts",
                columns: AttemptDeliveryIdStartedAtCols);

            migrationBuilder.CreateTable(
                name: "notification_delivery_audit_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_audit_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_delivery_audit_entries_notification_deliver~",
                        column: x => x.DeliveryId,
                        principalTable: "notification_deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_audit_entries_delivery_id_occurred_at",
                table: "notification_delivery_audit_entries",
                columns: AuditDeliveryIdOccurredAtCols);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_audit_entries_occurred_at",
                table: "notification_delivery_audit_entries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_audit_entries_actor_id_occurred_at",
                table: "notification_delivery_audit_entries",
                columns: AuditActorIdOccurredAtCols);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "notification_delivery_attempts");
            migrationBuilder.DropTable(name: "notification_delivery_audit_entries");
        }
    }
}
