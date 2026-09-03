using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotePdfGenerationArtifactCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotePdfGenerationArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDocumentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentModelJson = table.Column<string>(type: "jsonb", nullable: false),
                    LatexSource = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConvertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RenderedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotePdfGenerationArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotePdfGenerationArtifacts_NoteSubmissions_NoteSubmissionId",
                        column: x => x.NoteSubmissionId,
                        principalTable: "NoteSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotePdfGenerationArtifacts_NoteSubmissionId",
                table: "NotePdfGenerationArtifacts",
                column: "NoteSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotePdfGenerationArtifacts_SourceDocumentSha256",
                table: "NotePdfGenerationArtifacts",
                column: "SourceDocumentSha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotePdfGenerationArtifacts");
        }
    }
}
