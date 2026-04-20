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
        private readonly IWebHostEnvironment _env;  // ← NEW: to get wwwroot path

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
            ILogger<ResumeMatcherController> logger,
            IWebHostEnvironment env)   // ← NEW
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _env = env;
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

            // ── Ensure uploads directory exists under wwwroot ──────────────────
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(input.JdText), "jd_text");
            form.Add(new StringContent(input.Threshold.ToString()), "threshold");

            // Track saved file info: original filename → (saved filename, public URL)
            var savedFiles = new Dictionary<string, (string SavedName, string PublicUrl)>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in input.Resumes)
            {
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
                {
                    _logger.LogWarning("Skipping unsupported file: {Name}", file.FileName);
                    continue;
                }

                // ── Save to wwwroot/uploads with a unique name to avoid collisions ──
                var safeOriginal = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace("..", "_");
                var uniqueName = $"{safeOriginal}_{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadsFolder, uniqueName);

                await using (var fs = new FileStream(savePath, FileMode.Create))
                    await file.CopyToAsync(fs);

                // Public URL: /uploads/uniqueName
                var publicUrl = Url.Content($"~/uploads/{uniqueName}");
                savedFiles[file.FileName] = (uniqueName, publicUrl!);

                _logger.LogInformation("Saved resume: {File} → {Path}", file.FileName, savePath);

                // ── Also forward to Python API for analysis ──────────────────────
                var sc = new StreamContent(System.IO.File.OpenRead(savePath));
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    ext == ".pdf" ? "application/pdf" :
                    ext == ".docx" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                   : "text/plain");
                form.Add(sc, "files", file.FileName);  // keep original filename for Python
            }

            if (savedFiles.Count == 0)
                return ErrorView(input, "No valid files were found to upload (PDF/DOCX/TXT only).");

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
                    // ── Attach the local saved filename & public URL to each result ──
                    if (savedFiles.TryGetValue(r.File, out var fi))
                    {
                        r.SavedFileName = fi.SavedName;
                        r.PublicUrl = fi.PublicUrl;
                    }
                    else
                    {
                        // Fallback: try to match by original name without extension
                        r.SavedFileName = r.File;
                        r.PublicUrl = Url.Content($"~/uploads/{r.File}") ?? "#";
                    }

                    // ── Normalise all other fields ────────────────────────────────
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
                    r.EmploymentGapDetail = NullOrEmpty(r.EmploymentGapDetail) ? "No employment gap detected" : r.EmploymentGapDetail;
                    r.Locations = NullOrEmpty(r.Locations) ? "Not mentioned" : r.Locations;
                    r.SkillGapDetail = r.SkillGapDetail ?? "";
                    r.Summary = r.Summary ?? "";
                    r.MatchedSkills ??= new List<string>();
                    r.MissingSkills ??= new List<string>();
                    r.LowMatchReasons ??= new List<string>();
                    r.AffindaSkills ??= new List<string>();

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
                    $"Could not reach the Python API. Make sure the server is running. Detail: {ex.Message}");
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

        /// <summary>
        /// Download a resume that was saved locally in wwwroot/uploads.
        /// The savedFileName is the unique name we generated at upload time.
        /// </summary>
        [HttpGet]
        public IActionResult DownloadResume(string savedFileName, string originalName)
        {
            if (string.IsNullOrWhiteSpace(savedFileName))
                return BadRequest("Filename is required.");

            // Strip any path traversal attempts
            var safeName = Path.GetFileName(savedFileName);
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", safeName);

            if (!System.IO.File.Exists(uploadsPath))
            {
                _logger.LogWarning("Download requested for missing file: {File}", safeName);
                return NotFound($"File '{safeName}' not found. Please re-run analysis.");
            }

            var ext = Path.GetExtension(safeName).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            // Use the original filename for the download dialog
            var downloadName = string.IsNullOrWhiteSpace(originalName)
                ? safeName : Path.GetFileName(originalName);

            return PhysicalFile(uploadsPath, contentType, downloadName);
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