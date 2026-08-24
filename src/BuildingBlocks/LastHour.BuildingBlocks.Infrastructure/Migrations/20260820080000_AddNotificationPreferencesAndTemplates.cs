using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastHour.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndTemplates : Migration
    {
        private static readonly string[] PrefUserTypeChannelCols = new[] { "UserId", "NotificationType", "Channel" };
        private static readonly string[] PrefUserTypeCols = new[] { "UserId", "NotificationType" };
        private static readonly string[] TplKeyChannelVerCols = new[] { "TemplateKey", "Channel", "Version" };
        private static readonly string[] TplTypeChannelActiveCols = new[] { "NotificationType", "Channel", "IsActive" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_type_channel",
                table: "notification_preferences",
                columns: PrefUserTypeChannelCols,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_type",
                table: "notification_preferences",
                columns: PrefUserTypeCols);

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_key_channel_version",
                table: "notification_templates",
                columns: TplKeyChannelVerCols,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_type_channel_active",
                table: "notification_templates",
                columns: TplTypeChannelActiveCols);

            migrationBuilder.CreateTable(
                name: "notification_quiet_hours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    StartLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_quiet_hours", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_quiet_hours_user_id",
                table: "notification_quiet_hours",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "notification_preferences");
            migrationBuilder.DropTable(name: "notification_templates");
            migrationBuilder.DropTable(name: "notification_quiet_hours");
        }
    }
}
