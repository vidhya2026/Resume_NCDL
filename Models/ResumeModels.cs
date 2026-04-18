#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resume_NCDL.Models
{
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

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("tier")]
        public string Tier { get; set; } = "";

        [JsonPropertyName("tier_description")]
        public string TierDescription { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("candidate_type")]
        public string CandidateType { get; set; } = "";

        [JsonPropertyName("experience")]
        public string Experience { get; set; } = "";

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "";

        [JsonPropertyName("jd_domain")]
        public string JdDomain { get; set; } = "";

        [JsonPropertyName("domain_switch")]
        public bool DomainSwitch { get; set; }

        [JsonPropertyName("domain_switch_label")]
        public string DomainSwitchLabel { get; set; } = "";

        [JsonPropertyName("domain_switch_from")]
        public string DomainSwitchFrom { get; set; } = "";

        [JsonPropertyName("career_break")]
        public bool CareerBreak { get; set; }

        [JsonPropertyName("career_break_detail")]
        public string CareerBreakDetail { get; set; } = "";

        [JsonPropertyName("locations")]
        public string Locations { get; set; } = "";

        [JsonPropertyName("company_count")]
        public int CompanyCount { get; set; }

        [JsonPropertyName("company_count_estimated")]
        public bool CompanyCountEstimated { get; set; }

        [JsonPropertyName("matched_skills")]
        public List<string> MatchedSkills { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("skill_gap_detail")]
        public string SkillGapDetail { get; set; } = "";

        [JsonPropertyName("near_miss")]
        public bool NearMiss { get; set; }

        [JsonPropertyName("domain_mismatch")]
        public bool DomainMismatch { get; set; }

        [JsonPropertyName("wrong_profile")]
        public bool WrongProfile { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

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

    //public class ErrorViewModel
    //{
    //    public string? RequestId { get; set; }
    //    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    //}
}