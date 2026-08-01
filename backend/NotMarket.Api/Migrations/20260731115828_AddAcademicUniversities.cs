using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicUniversities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UniversityName",
                table: "StudentVerifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(180)",
                oldMaxLength: 180);

            migrationBuilder.AddColumn<Guid>(
                name: "UniversityId",
                table: "StudentVerifications",
                type: "uuid",
                maxLength: 180,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicUniversities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicUniversities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_UniversityId",
                table: "StudentVerifications",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversities_CountryCode_NormalizedName",
                table: "AcademicUniversities",
                columns: new[] { "CountryCode", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversities_IsActive",
                table: "AcademicUniversities",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVerifications_AcademicUniversities_UniversityId",
                table: "StudentVerifications",
                column: "UniversityId",
                principalTable: "AcademicUniversities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentVerifications_AcademicUniversities_UniversityId",
                table: "StudentVerifications");

            migrationBuilder.DropTable(
                name: "AcademicUniversities");

            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_UniversityId",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                table: "StudentVerifications");

            migrationBuilder.AlterColumn<string>(
                name: "UniversityName",
                table: "StudentVerifications",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
