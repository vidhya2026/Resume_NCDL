#nullable enable
using Microsoft.AspNetCore.Mvc;
using Resume_NCDL.Models;
using System.Text.Json;

namespace Resume_NCDL.Controllers
{
    public class ResumeMatcherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ResumeMatcherController> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ResumeMatcherController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<ResumeMatcherController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ResumeMatcherViewModel { Threshold = 40 });
        }

        [HttpPost]
        [RequestSizeLimit(104_857_600)]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        public async Task<IActionResult> Analyze(ResumeMatcherInput input)
        {
            if (string.IsNullOrWhiteSpace(input.JdText))
                return ErrorView(input, "Job Description is required.");

            if (input.Resumes == null || input.Resumes.Count == 0)
                return ErrorView(input, "Please upload at least one resume.");

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(input.JdText), "jd_text");
            form.Add(new StringContent(input.Threshold.ToString()), "threshold");

            foreach (var file in input.Resumes)
            {
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
                {
                    _logger.LogWarning("Skipping unsupported file: {Name}", file.FileName);
                    continue;
                }

                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    ext == ".pdf" ? "application/pdf" :
                    ext == ".docx" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                   : "text/plain");

                form.Add(sc, "files", file.FileName);
            }

            var baseUrl = _config["FlaskApi:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return ErrorView(input,
                    "Python API URL is not configured. Check FlaskApi:BaseUrl in appsettings.json.");

            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.PostAsync($"{baseUrl}/api/analyze-batch", form);
                var rawJson = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Python API raw response: {Json}", rawJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Python API {Code}: {Body}",
                        (int)response.StatusCode, rawJson);
                    return ErrorView(input,
                        $"Python API error ({(int)response.StatusCode}): " +
                        TryExtractErrorMessage(rawJson));
                }

                var apiResult = JsonSerializer.Deserialize<BatchApiResponse>(rawJson, _jsonOptions);

                if (apiResult == null)
                    return ErrorView(input,
                        "Received an empty or unparseable response from the API.");

                foreach (var r in apiResult.Results)
                {
                    r.Name = NullOrEmpty(r.Name) ? "Name Not Found" : r.Name;
                    r.CandidateType = NullOrEmpty(r.CandidateType) ? "Unknown" : r.CandidateType;
                    r.Domain = NullOrEmpty(r.Domain) ? "Unknown" : r.Domain;
                    r.JdDomain = NullOrEmpty(r.JdDomain) ? "Unknown" : r.JdDomain;
                    r.Experience = NullOrEmpty(r.Experience) ? "Not determinable" : r.Experience;
                    r.Tier = NullOrEmpty(r.Tier) ? "❌ Not Matched" : r.Tier;
                    r.TierDescription = NullOrEmpty(r.TierDescription) ? "" : r.TierDescription;
                    r.DomainSwitchLabel = NullOrEmpty(r.DomainSwitchLabel) ? (r.DomainSwitch ? "Yes" : "No") : r.DomainSwitchLabel;
                    r.DomainSwitchFrom = r.DomainSwitchFrom ?? "";
                    r.CareerBreakDetail = NullOrEmpty(r.CareerBreakDetail) ? "No career break detected" : r.CareerBreakDetail;
                    r.Locations = NullOrEmpty(r.Locations) ? "Not mentioned" : r.Locations;
                    r.SkillGapDetail = r.SkillGapDetail ?? "";
                    r.Summary = r.Summary ?? "";
                    r.MatchedSkills ??= new List<string>();
                    r.MissingSkills ??= new List<string>();

                    // Status: domain mismatch overrides everything; otherwise use numeric score
                    if (NullOrEmpty(r.Status))
                    {
                        r.Status = (r.DomainMismatch || r.WrongProfile)
                            ? "NOT MATCHED (DOMAIN MISMATCH)"
                            : (r.RawScore >= input.Threshold ? "MATCHED" : "NOT MATCHED");
                    }
                }

                return View("Index", new ResumeMatcherViewModel
                {
                    JdText = input.JdText,
                    Threshold = input.Threshold,
                    ApiResult = apiResult
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling Python API.");
                return ErrorView(input,
                    $"Could not reach the Python API. Make sure the HuggingFace Space is running. Detail: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Python API timed out.");
                return ErrorView(input,
                    "Request timed out. The API may still be loading models. Please wait and try again.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parse error.");
                return ErrorView(input, $"JSON parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Analyze.");
                return ErrorView(input, $"Unexpected error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> HealthCheck()
        {
            var baseUrl = _config["FlaskApi:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return Json(new { status = "error", message = "FlaskApi:BaseUrl not set" });
            try
            {
                var client = _httpClientFactory.CreateClient("PythonApi");
                var response = await client.GetAsync($"{baseUrl}/api/health");
                var body = await response.Content.ReadAsStringAsync();
                return Json(new
                {
                    status = response.IsSuccessStatusCode ? "ok" : "error",
                    apiResponse = body,
                    baseUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message, baseUrl });
            }
        }

        private IActionResult ErrorView(ResumeMatcherInput input, string msg) =>
            View("Index", new ResumeMatcherViewModel
            {
                JdText = input.JdText,
                Threshold = input.Threshold,
                ErrorMsg = msg
            });

        private static bool NullOrEmpty(string? s) => string.IsNullOrWhiteSpace(s);

        private static string TryExtractErrorMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return err.GetString() ?? json;
            }
            catch { }
            return json.Length > 300 ? json[..300] + "…" : json;
        }
    }
}