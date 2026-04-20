#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resume_NCDL.Models
{
    // ── Not-matched feedback from Python API ──────────────────────────
    public class NotMatchedFeedback
    {
        [JsonPropertyName("why_not_matched")]
        public List<string> WhyNotMatched { get; set; } = new();

        [JsonPropertyName("resume_best_fit_domain")]
        public string ResumeBestFitDomain { get; set; } = "";

        [JsonPropertyName("resume_best_fit_level")]
        public string ResumeBestFitLevel { get; set; } = "";

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = "";
    }

    // ── Near-miss detail — close but below threshold ──────────────────
    public class NearMissDetail
    {
        [JsonPropertyName("is_near_miss")]
        public bool IsNearMiss { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("threshold")]
        public double Threshold { get; set; }

        [JsonPropertyName("gap")]
        public double Gap { get; set; }

        [JsonPropertyName("reasons")]
        public List<string> Reasons { get; set; } = new();

        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; } = new();

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = "";
    }

    public class ResumeResult
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = "";

        [JsonPropertyName("score")]
        public JsonElement Score { get; set; }

        [JsonPropertyName("display_score")]
        public JsonElement DisplayScore { get; set; }

        [JsonIgnore]
        public bool IsScoreSuppressed =>
            DisplayScore.ValueKind == JsonValueKind.String;

        [JsonIgnore]
        public double RawScore =>
            Score.ValueKind == JsonValueKind.Number
                ? Score.GetDouble()
                : 0.0;

        [JsonIgnore]
        public double NumericScore =>
            !IsScoreSuppressed && DisplayScore.ValueKind == JsonValueKind.Number
                ? DisplayScore.GetDouble()
                : RawScore;

        // ── NEW: set by the controller after saving to wwwroot/uploads ────────
        /// <summary>The unique filename as saved in wwwroot/uploads (e.g. resume_abc123.pdf).</summary>
        [JsonIgnore]
        public string SavedFileName { get; set; } = "";

        /// <summary>Public URL to view/download the file (e.g. /uploads/resume_abc123.pdf).</summary>
        [JsonIgnore]
        public string PublicUrl { get; set; } = "";
        // ──────────────────────────────────────────────────────────────────────

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("tier")]
        public string Tier { get; set; } = "";

        [JsonPropertyName("tier_description")]
        public string TierDescription { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("name_source")]
        public string NameSource { get; set; } = "";

        [JsonPropertyName("candidate_type")]
        public string CandidateType { get; set; } = "";

        [JsonPropertyName("candidate_type_source")]
        public string CandidateTypeSource { get; set; } = "";

        [JsonPropertyName("experience")]
        public string Experience { get; set; } = "";

        [JsonPropertyName("experience_source")]
        public string ExperienceSource { get; set; } = "";

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "";

        [JsonPropertyName("domain_source")]
        public string DomainSource { get; set; } = "";

        [JsonPropertyName("jd_domain")]
        public string JdDomain { get; set; } = "";

        [JsonPropertyName("domain_switch")]
        public bool DomainSwitch { get; set; }

        [JsonPropertyName("domain_switch_label")]
        public string DomainSwitchLabel { get; set; } = "";

        [JsonPropertyName("domain_switch_from")]
        public string DomainSwitchFrom { get; set; } = "";

        // Career break (between jobs — experienced candidates only)
        [JsonPropertyName("career_break")]
        public bool CareerBreak { get; set; }

        [JsonPropertyName("career_break_detail")]
        public string CareerBreakDetail { get; set; } = "";

        [JsonPropertyName("career_break_source")]
        public string CareerBreakSource { get; set; } = "";

        // Employment gap (for freshers — time since graduation with no work)
        [JsonPropertyName("employment_gap")]
        public bool EmploymentGap { get; set; }

        [JsonPropertyName("employment_gap_detail")]
        public string EmploymentGapDetail { get; set; } = "";

        [JsonPropertyName("locations")]
        public string Locations { get; set; } = "";

        [JsonPropertyName("locations_source")]
        public string LocationsSource { get; set; } = "";

        [JsonPropertyName("company_count")]
        public int CompanyCount { get; set; }

        [JsonPropertyName("company_count_estimated")]
        public bool CompanyCountEstimated { get; set; }

        [JsonPropertyName("company_count_source")]
        public string CompanyCountSource { get; set; } = "";

        [JsonPropertyName("matched_skills")]
        public List<string> MatchedSkills { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("skill_gap_detail")]
        public string SkillGapDetail { get; set; } = "";

        [JsonPropertyName("affinda_skills")]
        public List<string> AffindaSkills { get; set; } = new();

        [JsonPropertyName("near_miss")]
        public bool NearMiss { get; set; }

        [JsonPropertyName("domain_mismatch")]
        public bool DomainMismatch { get; set; }

        [JsonPropertyName("wrong_profile")]
        public bool WrongProfile { get; set; }

        [JsonPropertyName("affinda_available")]
        public bool AffindaAvailable { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("low_match_reasons")]
        public List<string> LowMatchReasons { get; set; } = new();

        // Not-matched feedback with why + best-fit domain/level
        [JsonPropertyName("not_matched_feedback")]
        public NotMatchedFeedback? NotMatchedFeedback { get; set; }

        // Near-miss detail — close but below threshold
        [JsonPropertyName("near_miss_detail")]
        public NearMissDetail? NearMissDetail { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class BatchApiResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("matched")]
        public int Matched { get; set; }

        [JsonPropertyName("not_matched")]
        public int NotMatched { get; set; }

        [JsonPropertyName("near_misses")]
        public int NearMisses { get; set; }

        [JsonPropertyName("wrong_profiles")]
        public int WrongProfiles { get; set; }

        [JsonPropertyName("domain_mismatches")]
        public int DomainMismatches { get; set; }

        [JsonPropertyName("tier_summary")]
        public Dictionary<string, int> TierSummary { get; set; } = new();

        [JsonPropertyName("threshold")]
        public int Threshold { get; set; }

        [JsonPropertyName("results")]
        public List<ResumeResult> Results { get; set; } = new();
    }

    public class ResumeMatcherInput
    {
        public string JdText { get; set; } = "";
        public int Threshold { get; set; } = 40;
        public List<IFormFile> Resumes { get; set; } = new();
    }

    public class ResumeMatcherViewModel
    {
        public string JdText { get; set; } = "";
        public int Threshold { get; set; } = 40;
        public BatchApiResponse? ApiResult { get; set; }
        public string? ErrorMsg { get; set; }
    }
}