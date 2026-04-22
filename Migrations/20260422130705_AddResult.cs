using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resume_NCDL.Migrations
{
    /// <inheritdoc />
    public partial class AddResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "ResumeMatchResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SavedFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CandidateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CandidateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpYears = table.Column<double>(type: "float", nullable: false),
                    RawScore = table.Column<double>(type: "float", nullable: false),
                    DisplayScore = table.Column<double>(type: "float", nullable: false),
                    IsScoreSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TierDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JdDomain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DomainSwitch = table.Column<bool>(type: "bit", nullable: false),
                    DomainSwitchFrom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DomainMismatch = table.Column<bool>(type: "bit", nullable: false),
                    NearMiss = table.Column<bool>(type: "bit", nullable: false),
                    CareerBreak = table.Column<bool>(type: "bit", nullable: false),
                    CareerBreakDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentGap = table.Column<bool>(type: "bit", nullable: false),
                    EmploymentGapDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchedSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MissingSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillCoveragePct = table.Column<int>(type: "int", nullable: false),
                    TotalJdSkills = table.Column<int>(type: "int", nullable: false),
                    StayingLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkLocations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyCount = table.Column<int>(type: "int", nullable: false),
                    Education = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Internship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Projects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JdSnippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResumeMatchResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResumeMatchResults");

            migrationBuilder.CreateTable(
                name: "AnalysisSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DomainMismatchCount = table.Column<int>(type: "int", nullable: false),
                    JdText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchedCount = table.Column<int>(type: "int", nullable: false),
                    NearMissCount = table.Column<int>(type: "int", nullable: false),
                    NotMatchedCount = table.Column<int>(type: "int", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    TotalResumes = table.Column<int>(type: "int", nullable: false),
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
                    AffindaAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CandidateType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareerBreak = table.Column<bool>(type: "bit", nullable: false),
                    CareerBreakDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyCount = table.Column<int>(type: "int", nullable: false),
                    CompanyCountEstimated = table.Column<bool>(type: "bit", nullable: false),
                    DisplayScore = table.Column<double>(type: "float", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DomainMismatch = table.Column<bool>(type: "bit", nullable: false),
                    DomainSwitch = table.Column<bool>(type: "bit", nullable: false),
                    DomainSwitchFrom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Education = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentGap = table.Column<bool>(type: "bit", nullable: false),
                    EmploymentGapDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Experience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceYears = table.Column<double>(type: "float", nullable: false),
                    Internship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JdDomain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchedSkillsCsv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MissingSkillsCsv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NearMiss = table.Column<bool>(type: "bit", nullable: false),
                    NearMissDetail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NotMatchedFeedback = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Projects = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawScore = table.Column<double>(type: "float", nullable: false),
                    SavedFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoreSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    SkillCoveragePct = table.Column<int>(type: "int", nullable: false),
                    SkillGapDetail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StayingLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TierDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalJdSkills = table.Column<int>(type: "int", nullable: false),
                    WorkLocations = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
    }
}
