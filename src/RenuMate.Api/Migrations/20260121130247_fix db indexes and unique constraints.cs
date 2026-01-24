using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenuMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class fixdbindexesanduniqueconstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Name",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ReminderOccurrences_ReminderRuleId_IsSent",
                table: "ReminderOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_ReminderOccurrences_ScheduledAt",
                table: "ReminderOccurrences");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Name",
                table: "Subscriptions",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_StartDate",
                table: "Subscriptions",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_SubscriptionId_NotifyTimeUtc_DaysBeforeRenewal",
                table: "ReminderRules",
                columns: new[] { "SubscriptionId", "NotifyTimeUtc", "DaysBeforeRenewal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOccurrences_IsSent_ScheduledAt",
                table: "ReminderOccurrences",
                columns: new[] { "IsSent", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOccurrences_ReminderRuleId_ScheduledAt",
                table: "ReminderOccurrences",
                columns: new[] { "ReminderRuleId", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId_Name",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId_StartDate",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_SubscriptionId_NotifyTimeUtc_DaysBeforeRenewal",
                table: "ReminderRules");

            migrationBuilder.DropIndex(
                name: "IX_ReminderOccurrences_IsSent_ScheduledAt",
                table: "ReminderOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_ReminderOccurrences_ReminderRuleId_ScheduledAt",
                table: "ReminderOccurrences");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Name",
                table: "Subscriptions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOccurrences_ReminderRuleId_IsSent",
                table: "ReminderOccurrences",
                columns: new[] { "ReminderRuleId", "IsSent" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOccurrences_ScheduledAt",
                table: "ReminderOccurrences",
                column: "ScheduledAt");
        }
    }
}
