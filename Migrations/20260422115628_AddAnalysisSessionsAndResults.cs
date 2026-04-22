using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resume_NCDL.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisSessionsAndResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JdText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TotalResumes = table.Column<int>(type: "int", nullable: false),
                    MatchedCount = table.Column<int>(type: "int", nullable: false),
                    NotMatchedCount = table.Column<int>(type: "int", nullable: false),
                    NearMissCount = table.Column<int>(type: "int", nullable: false),
                    DomainMismatchCount = table.Column<int>(type: "int", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisSessions", x => x.Id);
                });

         

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SavedFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawScore = table.Column<double>(type: "float", nullable: false),
                    DisplayScore = table.Column<double>(type: "float", nullable: false),
                    ScoreSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TierDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CandidateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceYears = table.Column<double>(type: "float", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JdDomain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DomainMismatch = table.Column<bool>(type: "bit", nullable: false),
                    DomainSwitch = table.Column<bool>(type: "bit", nullable: false),
                    DomainSwitchFrom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareerBreak = table.Column<bool>(type: "bit", nullable: false),
                    CareerBreakDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentGap = table.Column<bool>(type: "bit", nullable: false),
                    EmploymentGapDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Education = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Internship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Projects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StayingLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkLocations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyCount = table.Column<int>(type: "int", nullable: false),
                    CompanyCountEstimated = table.Column<bool>(type: "bit", nullable: false),
                    MatchedSkillsCsv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MissingSkillsCsv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SkillCoveragePct = table.Column<int>(type: "int", nullable: false),
                    TotalJdSkills = table.Column<int>(type: "int", nullable: false),
                    SkillGapDetail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NearMiss = table.Column<bool>(type: "bit", nullable: false),
                    AffindaAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NotMatchedFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NearMissDetail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_AnalysisSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AnalysisSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_SessionId",
                table: "AnalysisResults",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisResults");


            migrationBuilder.DropTable(
                name: "AnalysisSessions");
        }
    }
}
