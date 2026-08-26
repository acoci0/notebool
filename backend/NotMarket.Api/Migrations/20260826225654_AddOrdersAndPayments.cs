using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "NoteSubmissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteTitleSnapshot = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformCommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SellerEarningAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_AmountBalance", "\"GrossAmount\" = \"PlatformCommissionAmount\" + \"SellerEarningAmount\"");
                    table.CheckConstraint("CK_Orders_DifferentUsers", "\"BuyerId\" <> \"SellerId\"");
                    table.CheckConstraint("CK_Orders_PositiveAmounts", "\"GrossAmount\" > 0 AND \"PlatformCommissionAmount\" >= 0 AND \"SellerEarningAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Orders_NoteSubmissions_NoteSubmissionId",
                        column: x => x.NoteSubmissionId,
                        principalTable: "NoteSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_PositiveAmount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_NoteSubmissions_SalePrice",
                table: "NoteSubmissions",
                sql: "\"SalePrice\" IS NULL OR \"SalePrice\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerId_NoteSubmissionId",
                table: "Orders",
                columns: new[] { "BuyerId", "NoteSubmissionId" },
                unique: true,
                filter: "\"Status\" IN ('PendingPayment', 'Paid')");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerId_Status",
                table: "Orders",
                columns: new[] { "BuyerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_NoteSubmissionId",
                table: "Orders",
                column: "NoteSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SellerId_Status",
                table: "Orders",
                columns: new[] { "SellerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderPaymentId",
                table: "Payments",
                column: "ProviderPaymentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NoteSubmissions_SalePrice",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "NoteSubmissions");
        }
    }
}
