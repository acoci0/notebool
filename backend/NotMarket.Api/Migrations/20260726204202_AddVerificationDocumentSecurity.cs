using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationDocumentSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_DocumentHash",
                table: "StudentVerifications");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_DocumentHash",
                table: "StudentVerifications",
                column: "DocumentHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_DocumentHash",
                table: "StudentVerifications");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_DocumentHash",
                table: "StudentVerifications",
                column: "DocumentHash");
        }
    }
}
