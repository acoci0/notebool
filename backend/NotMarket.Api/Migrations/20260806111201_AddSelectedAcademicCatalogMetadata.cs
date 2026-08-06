using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedAcademicCatalogMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "AcademicUniversities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogVersion",
                table: "AcademicUniversities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "AcademicUniversities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastVerifiedAt",
                table: "AcademicUniversities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "AcademicUniversities",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "AcademicUnits",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogVersion",
                table: "AcademicUnits",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastVerifiedAt",
                table: "AcademicUnits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "AcademicUnits",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogKey",
                table: "AcademicPrograms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogVersion",
                table: "AcademicPrograms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DegreeLevel",
                table: "AcademicPrograms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLanguage",
                table: "AcademicPrograms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelectable",
                table: "AcademicPrograms",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastVerifiedAt",
                table: "AcademicPrograms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "AcademicPrograms",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicUniversityAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alias = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NormalizedAlias = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicUniversityAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicUniversityAliases_AcademicUniversities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "AcademicUniversities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversities_CatalogKey",
                table: "AcademicUniversities",
                column: "CatalogKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUnits_CatalogKey",
                table: "AcademicUnits",
                column: "CatalogKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_AcademicUnitId_IsActive_IsSelectable",
                table: "AcademicPrograms",
                columns: new[] { "AcademicUnitId", "IsActive", "IsSelectable" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_CatalogKey",
                table: "AcademicPrograms",
                column: "CatalogKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversityAliases_NormalizedAlias",
                table: "AcademicUniversityAliases",
                column: "NormalizedAlias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicUniversityAliases_UniversityId",
                table: "AcademicUniversityAliases",
                column: "UniversityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicUniversityAliases");

            migrationBuilder.DropIndex(
                name: "IX_AcademicUniversities_CatalogKey",
                table: "AcademicUniversities");

            migrationBuilder.DropIndex(
                name: "IX_AcademicUnits_CatalogKey",
                table: "AcademicUnits");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPrograms_AcademicUnitId_IsActive_IsSelectable",
                table: "AcademicPrograms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPrograms_CatalogKey",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "CatalogVersion",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "City",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "AcademicUniversities");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "AcademicUnits");

            migrationBuilder.DropColumn(
                name: "CatalogVersion",
                table: "AcademicUnits");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "AcademicUnits");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "AcademicUnits");

            migrationBuilder.DropColumn(
                name: "CatalogKey",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "CatalogVersion",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "DegreeLevel",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "EducationLanguage",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "IsSelectable",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "AcademicPrograms");
        }
    }
}
