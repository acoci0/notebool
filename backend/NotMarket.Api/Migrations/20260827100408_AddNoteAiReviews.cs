using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotMarket.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteAiReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoteAiReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsTechnicallyValid = table.Column<bool>(type: "boolean", nullable: false),
                    ReadabilityScore = table.Column<int>(type: "integer", nullable: false),
                    CourseMatchScore = table.Column<int>(type: "integer", nullable: false),
                    DepartmentMatchScore = table.Column<int>(type: "integer", nullable: false),
                    ContentCompletenessScore = table.Column<int>(type: "integer", nullable: false),
                    OriginalityAndReliabilityScore = table.Column<int>(type: "integer", nullable: false),
                    OriginalityRiskScore = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    FindingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    DetectedCourse = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    DetectedDepartment = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteAiReviews", x => x.Id);
                    table.CheckConstraint("CK_NoteAiReviews_ScoresRange", "\"ReadabilityScore\" BETWEEN 0 AND 100 AND \"CourseMatchScore\" BETWEEN 0 AND 100 AND \"DepartmentMatchScore\" BETWEEN 0 AND 100 AND \"ContentCompletenessScore\" BETWEEN 0 AND 100 AND \"OriginalityAndReliabilityScore\" BETWEEN 0 AND 100 AND \"OriginalityRiskScore\" BETWEEN 0 AND 100 AND \"OverallScore\" BETWEEN 0 AND 100 AND \"ConfidenceScore\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_NoteAiReviews_NoteSubmissions_NoteSubmissionId",
                        column: x => x.NoteSubmissionId,
                        principalTable: "NoteSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteAiReviews_Decision_ReviewedAt",
                table: "NoteAiReviews",
                columns: new[] { "Decision", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NoteAiReviews_NoteSubmissionId_ReviewedAt",
                table: "NoteAiReviews",
                columns: new[] { "NoteSubmissionId", "ReviewedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteAiReviews");
        }
    }
}
