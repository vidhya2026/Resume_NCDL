// Models/ResumeMatchResult.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resume_NCDL.Models
{
    public class ResumeMatchResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        // File info
        public string OriginalFileName { get; set; } = "";
        public string SavedFileName { get; set; } = "";
        public string PublicUrl { get; set; } = "";

        // Candidate
        public string CandidateName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CandidateType { get; set; } = "";   // Fresher / Experienced
        public string Experience { get; set; } = "";
        public double ExpYears { get; set; }

        // Scores
        public double RawScore { get; set; }
        public double DisplayScore { get; set; }
        public bool IsScoreSuppressed { get; set; }
        public int Threshold { get; set; }

        // Status / Tier
        public string Status { get; set; } = "";
        public string Tier { get; set; } = "";
        public string TierDescription { get; set; } = "";

        // Domain
        public string Domain { get; set; } = "";
        public string JdDomain { get; set; } = "";
        public bool DomainSwitch { get; set; }
        public string DomainSwitchFrom { get; set; } = "";

        // Flags
        public bool DomainMismatch { get; set; }
        public bool NearMiss { get; set; }
        public bool CareerBreak { get; set; }
        public string CareerBreakDetail { get; set; } = "";
        public bool EmploymentGap { get; set; }
        public string EmploymentGapDetail { get; set; } = "";

        // Skills (stored as comma-separated)
        public string MatchedSkills { get; set; } = "";
        public string MissingSkills { get; set; } = "";
        public int SkillCoveragePct { get; set; }
        public int TotalJdSkills { get; set; }

        // Location
        public string StayingLocation { get; set; } = "";
        public string WorkLocations { get; set; } = "";

        // Companies
        public int CompanyCount { get; set; }

        // Fresher extras
        public string Education { get; set; } = "";
        public string Internship { get; set; } = "";
        public string Projects { get; set; } = "";

        // JD snippet for reference
        public string JdSnippet { get; set; } = "";

        // Summary / recommendation
        public string Summary { get; set; } = "";
    }
}