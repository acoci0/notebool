using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicUnitsAndPrograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_UserId",
                table: "StudentVerifications");

            migrationBuilder.DropIndex(
                name: "IX_AcademicUniversities_IsActive",
                table: "AcademicUniversities");

            migrationBuilder.AlterColumn<string>(
                name: "FacultyName",
                table: "StudentVerifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(180)",
                oldMaxLength: 180);

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "StudentVerifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(180)",
                oldMaxLength: 180);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicProgramId",
                table: "StudentVerifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicUnitId",
                table: "StudentVerifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    UnitType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicUnits_AcademicUniversities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "AcademicUniversities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicPrograms_AcademicUnits_AcademicUnitId",
                        column: x => x.AcademicUnitId,
                        principalTable: "AcademicUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_AcademicProgramId",
                table: "StudentVerifications",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_AcademicUnitId",
                table: "StudentVerifications",
                column: "AcademicUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_UserId_UniversityId_AcademicUnitId_Aca~",
                table: "StudentVerifications",
                columns: new[] { "UserId", "UniversityId", "AcademicUnitId", "AcademicProgramId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversities_CountryCode_IsActive",
                table: "AcademicUniversities",
                columns: new[] { "CountryCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_AcademicUnitId_IsActive",
                table: "AcademicPrograms",
                columns: new[] { "AcademicUnitId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_AcademicUnitId_NormalizedName",
                table: "AcademicPrograms",
                columns: new[] { "AcademicUnitId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUnits_UniversityId_IsActive",
                table: "AcademicUnits",
                columns: new[] { "UniversityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUnits_UniversityId_NormalizedName",
                table: "AcademicUnits",
                columns: new[] { "UniversityId", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVerifications_AcademicPrograms_AcademicProgramId",
                table: "StudentVerifications",
                column: "AcademicProgramId",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentVerifications_AcademicUnits_AcademicUnitId",
                table: "StudentVerifications",
                column: "AcademicUnitId",
                principalTable: "AcademicUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentVerifications_AcademicPrograms_AcademicProgramId",
                table: "StudentVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentVerifications_AcademicUnits_AcademicUnitId",
                table: "StudentVerifications");

            migrationBuilder.DropTable(
                name: "AcademicPrograms");

            migrationBuilder.DropTable(
                name: "AcademicUnits");

            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_AcademicProgramId",
                table: "StudentVerifications");

            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_AcademicUnitId",
                table: "StudentVerifications");

            migrationBuilder.DropIndex(
                name: "IX_StudentVerifications_UserId_UniversityId_AcademicUnitId_Aca~",
                table: "StudentVerifications");

            migrationBuilder.DropIndex(
                name: "IX_AcademicUniversities_CountryCode_IsActive",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                table: "StudentVerifications");

            migrationBuilder.DropColumn(
                name: "AcademicUnitId",
                table: "StudentVerifications");

            migrationBuilder.AlterColumn<string>(
                name: "FacultyName",
                table: "StudentVerifications",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentName",
                table: "StudentVerifications",
                type: "character varying(180)",
                maxLength: 180,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_StudentVerifications_UserId",
                table: "StudentVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversities_IsActive",
                table: "AcademicUniversities",
                column: "IsActive");
        }
    }
}
