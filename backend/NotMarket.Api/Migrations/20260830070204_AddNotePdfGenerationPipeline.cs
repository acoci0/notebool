using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotePdfGenerationPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfCompilerName",
                table: "NoteSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfConversionPromptVersion",
                table: "NoteSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PdfGeneratedAt",
                table: "NoteSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PdfGenerationAttemptCount",
                table: "NoteSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PdfGenerationError",
                table: "NoteSubmissions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfGenerationModelName",
                table: "NoteSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfTemplateVersion",
                table: "NoteSubmissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoteSubmissions_Status",
                table: "NoteSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSubmissions_Status_CreatedAt",
                table: "NoteSubmissions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_NoteSubmissions_PdfGenerationAttemptCount",
                table: "NoteSubmissions",
                sql: "\"PdfGenerationAttemptCount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NoteSubmissions_Status",
                table: "NoteSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_NoteSubmissions_Status_CreatedAt",
                table: "NoteSubmissions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NoteSubmissions_PdfGenerationAttemptCount",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfCompilerName",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfConversionPromptVersion",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfGeneratedAt",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfGenerationAttemptCount",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfGenerationError",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfGenerationModelName",
                table: "NoteSubmissions");

            migrationBuilder.DropColumn(
                name: "PdfTemplateVersion",
                table: "NoteSubmissions");
        }
    }
}
