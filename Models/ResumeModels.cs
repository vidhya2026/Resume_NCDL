#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resume_NCDL.Models
{
    // ── Not-matched feedback from Python API ──────────────────────────────
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

    // ── Near-miss detail ──────────────────────────────────────────────────
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
        // ── File ─────────────────────────────────────────────────────────
        [JsonPropertyName("file")]
        public string File { get; set; } = "";

        // ── Score fields ──────────────────────────────────────────────────
        [JsonPropertyName("score")]
        public JsonElement Score { get; set; }

        [JsonPropertyName("display_score")]
        public JsonElement DisplayScore { get; set; }

        [JsonIgnore]
        public bool IsScoreSuppressed =>
            DisplayScore.ValueKind == JsonValueKind.String &&
            (DisplayScore.GetString() ?? "").Trim().ToUpper() == "N/A";

        [JsonIgnore]
        public double RawScore =>
            Score.ValueKind == JsonValueKind.Number ? Score.GetDouble() : 0.0;

        [JsonIgnore]
        public double NumericScore
        {
            get
            {
                if (IsScoreSuppressed) return RawScore;
                if (DisplayScore.ValueKind == JsonValueKind.Number)
                    return DisplayScore.GetDouble();
                if (DisplayScore.ValueKind == JsonValueKind.String)
                {
                    var s = (DisplayScore.GetString() ?? "").Replace("%", "").Trim();
                    if (double.TryParse(s,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var v))
                        return v;
                }
                return RawScore;
            }
        }

        // ── Set by controller after saving file ───────────────────────────
        [JsonIgnore] public string SavedFileName { get; set; } = "";
        [JsonIgnore] public string PublicUrl { get; set; } = "";

        // ── Status / Tier ─────────────────────────────────────────────────
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("tier")] public string Tier { get; set; } = "";
        [JsonPropertyName("tier_description")] public string TierDescription { get; set; } = "";

        // ── Identity ──────────────────────────────────────────────────────
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("email")] public string Email { get; set; } = "";
        [JsonPropertyName("phone")] public string Phone { get; set; } = "";

        // Python sends "type" — map to CandidateType
        [JsonPropertyName("type")]
        public string CandidateType { get; set; } = "";

        // Fallback: older Python builds may send "candidate_type"
        [JsonPropertyName("candidate_type")]
        public string CandidateTypeFallback
        {
            set { if (string.IsNullOrWhiteSpace(CandidateType)) CandidateType = value; }
        }

        // ── Experience ────────────────────────────────────────────────────
        [JsonPropertyName("experience")] public string Experience { get; set; } = "";

        // Python sends "resume_domain" — map to Domain
        [JsonPropertyName("resume_domain")]
        public string Domain { get; set; } = "";

        // Fallback
        [JsonPropertyName("domain")]
        public string DomainFallback
        {
            set { if (string.IsNullOrWhiteSpace(Domain)) Domain = value; }
        }

        [JsonPropertyName("jd_domain")] public string JdDomain { get; set; } = "";

        // ── Domain switch ─────────────────────────────────────────────────
        [JsonPropertyName("domain_switch")] public bool DomainSwitch { get; set; }
        [JsonPropertyName("domain_switch_from")] public string DomainSwitchFrom { get; set; } = "";

        // Derived in controller — Python does not send this
        [JsonIgnore] public string DomainSwitchLabel { get; set; } = "";

        // ── Career break (experienced) ────────────────────────────────────
        [JsonPropertyName("career_break")] public bool CareerBreak { get; set; }
        [JsonPropertyName("career_break_detail")] public string CareerBreakDetail { get; set; } = "";

        // ── Employment gap (fresher) ──────────────────────────────────────
        [JsonPropertyName("employment_gap")] public bool EmploymentGap { get; set; }
        [JsonPropertyName("employment_gap_detail")] public string EmploymentGapDetail { get; set; } = "";

        // ── Education & practical experience (fresher) ────────────────────
        [JsonPropertyName("education")] public string Education { get; set; } = "";
        [JsonPropertyName("internship")] public string Internship { get; set; } = "";
        [JsonPropertyName("projects")] public string Projects { get; set; } = "";

        // ── Locations ─────────────────────────────────────────────────────
        // Python sends "staying_location" — home/residential location
        [JsonPropertyName("staying_location")]
        public string StayingLocation { get; set; } = "";

        // Python sends "work_locations" — office/work city history
        [JsonPropertyName("work_locations")]
        public string Locations { get; set; } = "";

        // Fallback alias
        [JsonPropertyName("locations")]
        public string LocationsFallback
        {
            set { if (string.IsNullOrWhiteSpace(Locations)) Locations = value; }
        }

        // ── Companies ─────────────────────────────────────────────────────
        // Python sends "companies"
        [JsonPropertyName("companies")]
        public int CompanyCount { get; set; }

        // Fallback alias
        [JsonPropertyName("company_count")]
        public int CompanyCountFallback
        {
            set { if (CompanyCount == 0) CompanyCount = value; }
        }

        [JsonPropertyName("company_count_estimated")] public bool CompanyCountEstimated { get; set; }

        // ── Skills ────────────────────────────────────────────────────────
        [JsonPropertyName("matched_skills")] public List<string> MatchedSkills { get; set; } = new();
        [JsonPropertyName("missing_skills")] public List<string> MissingSkills { get; set; } = new();
        [JsonPropertyName("skill_coverage_pct")] public int SkillCoveragePct { get; set; }
        [JsonPropertyName("total_jd_skills")] public int TotalJdSkills { get; set; }
        [JsonPropertyName("skill_gap_detail")] public string SkillGapDetail { get; set; } = "";
        [JsonPropertyName("affinda_skills")] public List<string> AffindaSkills { get; set; } = new();

        // ── Flags ─────────────────────────────────────────────────────────
        [JsonPropertyName("near_miss")] public bool NearMiss { get; set; }
        [JsonPropertyName("domain_mismatch")] public bool DomainMismatch { get; set; }
        [JsonPropertyName("affinda_available")] public bool AffindaAvailable { get; set; }

        // Python does NOT send "wrong_profile" — always false
        [JsonIgnore] public bool WrongProfile { get; set; } = false;

        // ── Feedback ──────────────────────────────────────────────────────
        // Python sends recommendation text under key "recommendation"
        [JsonPropertyName("recommendation")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("not_matched_feedback")] public NotMatchedFeedback? NotMatchedFeedback { get; set; }
        [JsonPropertyName("near_miss_detail")] public NearMissDetail? NearMissDetail { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    // ── Batch API response ────────────────────────────────────────────────
    public class BatchApiResponse
    {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("matched")] public int Matched { get; set; }
        [JsonPropertyName("not_matched")] public int NotMatched { get; set; }
        [JsonPropertyName("near_misses")] public int NearMisses { get; set; }
        [JsonPropertyName("domain_mismatches")] public int DomainMismatches { get; set; }

        // Python does NOT send "wrong_profiles" — default 0
        [JsonIgnore] public int WrongProfiles { get; set; } = 0;

        [JsonPropertyName("tier_summary")] public Dictionary<string, int> TierSummary { get; set; } = new();
        [JsonPropertyName("threshold")] public int Threshold { get; set; }
        [JsonPropertyName("results")] public List<ResumeResult> Results { get; set; } = new();
    }

    // ── Form input ────────────────────────────────────────────────────────
    public class ResumeMatcherInput
    {
        public string JdText { get; set; } = "";
        public int Threshold { get; set; } = 40;

        /// <summary>Files uploaded directly via the browser file-picker.</summary>
        public List<IFormFile> Resumes { get; set; } = new();

        /// <summary>
        /// Pipe-separated full server paths for resumes selected from the
        /// database dropdown in the UI.
        /// Example: "C:\Resumes\john.pdf|C:\Resumes\jane.docx"
        /// Populated by the hidden &lt;input name="DbFilePaths" /&gt; field.
        /// </summary>
        public string DbFilePaths { get; set; } = "";
    }

    // ── View model ────────────────────────────────────────────────────────
    public class ResumeMatcherViewModel
    {
        public string JdText { get; set; } = "";
        public int Threshold { get; set; } = 40;
        public BatchApiResponse? ApiResult { get; set; }
        public string? ErrorMsg { get; set; }
    }

    // ── DTO returned by GET /ResumeMatcher/GetDbResumes ───────────────────
    /// <summary>
    /// Shape consumed by the front-end dropdown.
    /// Resumes are stored as byte[] blobs in Details/InfoTable —
    /// no FilePath needed. The front-end sends back the Id when selected.
    /// </summary>
    public class DbResumeDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";   // ResumeFileName from Details
        public string UploadedBy { get; set; } = "";   // not in Details — always empty
        public DateTime? UploadedAt { get; set; }         // not in Details — always null
    }
}