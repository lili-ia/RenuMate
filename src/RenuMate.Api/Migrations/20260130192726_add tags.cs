using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RenuMate.Api.Migrations
{
    /// <inheritdoc />
    public partial class addtags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTags",
                columns: table => new
                {
                    SubscriptionsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTags", x => new { x.SubscriptionsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_SubscriptionTags_Subscriptions_SubscriptionsId",
                        column: x => x.SubscriptionsId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Color", "CreatedAt", "IsSystem", "Name", "UserId" },
                values: new object[,]
                {
                    { new Guid("10e9d371-e94f-4f81-b739-94a3038803df"), "#4CAF50", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Sport", null },
                    { new Guid("20b1723d-b723-4f54-9d7f-b72accf8ecae"), "#FF9800", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Finance", null },
                    { new Guid("4aa91298-37a1-46e8-b1f8-5b31882c6c7e"), "#E91E63", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Streaming", null },
                    { new Guid("538ac968-6ab4-4341-9777-dec6f9eb4fb4"), "#607D8B", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Cloud Storage", null },
                    { new Guid("62e4982e-4875-49f6-8fab-2634cd26489e"), "#FFC107", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Shopping", null },
                    { new Guid("670f7a67-07a5-4941-940f-e29150da1037"), "#00BCD4", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Telephony", null },
                    { new Guid("7f6b33d8-d0ad-4f28-9ff1-059cc7e1bde7"), "#03A9F4", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Education", null },
                    { new Guid("97f23909-edff-4401-ae5f-0747587d136c"), "#3F51B5", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Security", null },
                    { new Guid("9eb8c6c5-fb75-48cb-8886-bffa682de635"), "#FF5722", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Food & Drinks", null },
                    { new Guid("a40355c0-87e9-4558-abe7-22ea6c4da41d"), "#795548", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Household", null },
                    { new Guid("b2f40faa-f537-4020-9792-b79874e747ec"), "#673AB7", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "News & Media", null },
                    { new Guid("c30588eb-c063-410f-985c-2c9ff063d2fc"), "#009688", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Lifestyle", null },
                    { new Guid("d545838b-28b2-4fbc-9191-35f9bf968c5f"), "#2196F3", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Work", null },
                    { new Guid("de3e6465-7993-4a55-8ab7-792b107ebe17"), "#F44336", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Pets", null },
                    { new Guid("e9e68348-dcd2-4904-b1b1-91a1883c720c"), "#9C27B0", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Gaming", null },
                    { new Guid("f3b512f7-a07a-45b8-b9f1-604cb574ae27"), "#8BC34A", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Health", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTags_TagsId",
                table: "SubscriptionTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsSystem",
                table: "Tags",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true,
                filter: "\"IsSystem\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId",
                table: "Tags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionTags");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
