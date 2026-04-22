#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resume_NCDL.Data;
using Resume_NCDL.Models;
using System.Text.Json;

namespace Resume_NCDL.Controllers
{
    public class ResumeMatcherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ResumeMatcherController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationdbContext _db;   // ← your DbContext

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ResumeMatcherController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<ResumeMatcherController> logger,
            IWebHostEnvironment env,
            ApplicationdbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
            _env = env;
            _db = db;
        }

        // ═════════════════════════════════════════════════════════════════
        //  GET  /ResumeMatcher
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Index()
        {
            return View(new ResumeMatcherViewModel { Threshold = 40 });
        }

        // ═════════════════════════════════════════════════════════════════
        //  GET  /ResumeMatcher/GetDbResumes
        //  Called by the front-end dropdown via fetch() on page load.
        //  Returns: [{ id, fileName }]  — no file path needed (bytes in DB)
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetDbResumes()
        {
            // Read from InfoTable (Details entity)
            // We only need Id and FileName for the dropdown list
            var list = _db.InfoTable
                .Where(r => r.ResumeData != null && r.ResumeFileName != null)
                .OrderByDescending(r => r.Id)
                .Select(r => new DbResumeDto
                {
                    Id = r.Id,
                    FileName = r.ResumeFileName,
                    // FilePath is not applicable — bytes stored in DB
                    // UploadedBy / UploadedAt not in your Details entity — leave blank
                })
                .ToList();

            return Json(list);
        }

        // ═════════════════════════════════════════════════════════════════
        //  GET  /ResumeMatcher/AllResults
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> AllResults()
        {
            var results = await _db.ResumeMatchResults
                .OrderByDescending(r => r.AnalyzedAt)
                .ToListAsync();

            return View(results);
        }

        // ═════════════════════════════════════════════════════════════════
        //  POST  /ResumeMatcher/DeleteResult   (optional — single delete)
        // ═════════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var r = await _db.ResumeMatchResults.FindAsync(id);
            if (r != null) { _db.ResumeMatchResults.Remove(r); await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(AllResults));
        }

        // ═════════════════════════════════════════════════════════════════
        //  POST  /ResumeMatcher/Analyze
        // ═════════════════════════════════════════════════════════════════
        [HttpPost]
        [RequestSizeLimit(104_857_600)]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        public async Task<IActionResult> Analyze(ResumeMatcherInput input)
        {
            if (string.IsNullOrWhiteSpace(input.JdText))
                return ErrorView(input, "Job Description is required.");

            // Must have at least one source — browser file OR DB selection
            bool hasBrowserFiles = input.Resumes != null &&
                                   input.Resumes.Any(f => f != null && f.Length > 0);

            // DbFilePaths now carries pipe-separated DB record IDs (not file paths)
            // e.g.  "3|7|12"
            bool hasDbIds = !string.IsNullOrWhiteSpace(input.DbFilePaths);

            if (!hasBrowserFiles && !hasDbIds)
                return ErrorView(input,
                    "Please upload at least one resume or select one from the database.");

            // ── Ensure uploads folder exists ──────────────────────────────
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(input.JdText), "jd_text");
            form.Add(new StringContent(input.Threshold.ToString()), "threshold");

            // Track: original filename → (unique saved filename, public URL)
            var savedFiles = new Dictionary<string, (string SavedName, string PublicUrl)>(
                StringComparer.OrdinalIgnoreCase);

            // ── 1. Process browser-uploaded files ─────────────────────────
            foreach (var file in (input.Resumes ?? new List<IFormFile>()))
            {
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
                {
                    _logger.LogWarning("Skipping unsupported browser file: {Name}", file.FileName);
                    continue;
                }

                var safeBase = Path.GetFileNameWithoutExtension(file.FileName)
                                     .Replace(" ", "_").Replace("..", "_");
                var uniqueName = $"{safeBase}_{Guid.NewGuid():N}{ext}";
                var savePath = Path.Combine(uploadsFolder, uniqueName);

                await using (var fs = new FileStream(savePath, FileMode.Create))
                    await file.CopyToAsync(fs);

                var publicUrl = Url.Content($"~/uploads/{uniqueName}");
                savedFiles[file.FileName] = (uniqueName, publicUrl!);

                _logger.LogInformation("Saved (browser): {File} → {Path}", file.FileName, savePath);

                var sc = new StreamContent(System.IO.File.OpenRead(savePath));
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(MimeType(ext));
                form.Add(sc, "files", file.FileName);
            }

            // ── 2. Process DB-sourced files (bytes from InfoTable) ─────────
            if (hasDbIds)
            {
                // Parse the pipe-separated record IDs sent from the hidden input
                var ids = input.DbFilePaths
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                    .Where(n => n > 0)
                    .Distinct()
                    .ToList();

                if (ids.Count > 0)
                {
                    // Fetch only the selected rows from DB
                    var dbRecords = await _db.InfoTable
                        .Where(r => ids.Contains(r.Id) &&
                                    r.ResumeData != null &&
                                    r.ResumeFileName != null)
                        .ToListAsync();

                    foreach (var record in dbRecords)
                    {
                        if (record.ResumeData == null || record.ResumeData.Length == 0)
                        {
                            _logger.LogWarning("DB record {Id} has empty ResumeData — skipping.", record.Id);
                            continue;
                        }

                        var dbFileName = record.ResumeFileName ?? $"resume_{record.Id}";
                        var ext = Path.GetExtension(dbFileName).ToLowerInvariant();

                        // Infer extension from content type if filename has none
                        if (string.IsNullOrWhiteSpace(ext))
                        {
                            ext = (record.ResumeContentType ?? "").ToLowerInvariant() switch
                            {
                                "application/pdf" => ".pdf",
                                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                                "text/plain" => ".txt",
                                _ => ".pdf"   // default assumption
                            };
                            dbFileName += ext;
                        }

                        if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
                        {
                            _logger.LogWarning("Skipping unsupported DB file: {Name}", dbFileName);
                            continue;
                        }

                        // Write bytes to uploads folder so it gets a public URL
                        var safeBase = Path.GetFileNameWithoutExtension(dbFileName)
                                             .Replace(" ", "_").Replace("..", "_");
                        var uniqueName = $"{safeBase}_{Guid.NewGuid():N}{ext}";
                        var savePath = Path.Combine(uploadsFolder, uniqueName);

                        await System.IO.File.WriteAllBytesAsync(savePath, record.ResumeData);

                        var publicUrl = Url.Content($"~/uploads/{uniqueName}");
                        // Use a unique key in case two records share the same filename
                        var dictKey = $"{record.Id}_{dbFileName}";
                        savedFiles[dictKey] = (uniqueName, publicUrl!);

                        _logger.LogInformation(
                            "Saved (db id={Id}): {File} → {Path}", record.Id, dbFileName, savePath);

                        // Forward to Python API
                        var sc = new StreamContent(System.IO.File.OpenRead(savePath));
                        sc.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(MimeType(ext));
                        // Use the original filename so Python can log it correctly
                        form.Add(sc, "files", dbFileName);
                    }
                }
            }

            // ── Guard: nothing valid was processed ────────────────────────
            if (savedFiles.Count == 0)
                return ErrorView(input,
                    "No valid files were found to process (PDF / DOCX / TXT only).");

            // ── 3. Call Python analysis API ───────────────────────────────
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

                // ── 4. Normalise every result ─────────────────────────────
                foreach (var r in apiResult.Results)
                {
                    // Try to match by original filename first
                    if (!savedFiles.TryGetValue(r.File, out var fi))
                    {
                        // For DB files, the key was "{id}_{filename}" — find by filename suffix
                        var match = savedFiles.FirstOrDefault(kv =>
                            kv.Key.EndsWith("_" + r.File, StringComparison.OrdinalIgnoreCase));
                        fi = match.Value;

                        if (string.IsNullOrEmpty(fi.SavedName))
                        {
                            // Last resort fallback
                            r.SavedFileName = r.File;
                            r.PublicUrl = Url.Content($"~/uploads/{r.File}") ?? "#";
                        }
                    }

                    if (!string.IsNullOrEmpty(fi.SavedName))
                    {
                        r.SavedFileName = fi.SavedName;
                        r.PublicUrl = fi.PublicUrl;
                    }

                    // ── Normalise all string fields ───────────────────────
                    r.Name = NullOrEmpty(r.Name) ? "Name Not Found" : r.Name;
                    r.Email = NullOrEmpty(r.Email) ? "Not mentioned" : r.Email;
                    r.Phone = NullOrEmpty(r.Phone) ? "Not mentioned" : r.Phone;
                    r.CandidateType = NullOrEmpty(r.CandidateType) ? "Unknown" : r.CandidateType;
                    r.Domain = NullOrEmpty(r.Domain) ? "Unknown" : r.Domain;
                    r.JdDomain = NullOrEmpty(r.JdDomain) ? "Unknown" : r.JdDomain;
                    r.Experience = NullOrEmpty(r.Experience) ? "Not determinable" : r.Experience;
                    r.Education = NullOrEmpty(r.Education) ? "Not mentioned" : r.Education;
                    r.Internship = NullOrEmpty(r.Internship) ? "Not mentioned" : r.Internship;
                    r.Projects = NullOrEmpty(r.Projects) ? "Not mentioned" : r.Projects;
                    r.StayingLocation = NullOrEmpty(r.StayingLocation) ? "Not mentioned" : r.StayingLocation;
                    r.Locations = NullOrEmpty(r.Locations) ? "Not mentioned" : r.Locations;
                    r.Tier = NullOrEmpty(r.Tier) ? "❌ Not Matched" : r.Tier;
                    r.TierDescription = NullOrEmpty(r.TierDescription) ? "" : r.TierDescription;
                    r.CareerBreakDetail = NullOrEmpty(r.CareerBreakDetail)
                        ? "No career break detected" : r.CareerBreakDetail;
                    r.EmploymentGapDetail = NullOrEmpty(r.EmploymentGapDetail)
                        ? "No employment gap detected" : r.EmploymentGapDetail;

                    r.DomainSwitchLabel = r.DomainSwitch
                        ? $"Yes ({r.DomainSwitchFrom} → {r.Domain})"
                        : "No";
                    r.DomainSwitchFrom = r.DomainSwitchFrom ?? "";

                    r.WrongProfile = false;   // Python never sends this
                    r.MatchedSkills ??= new List<string>();
                    r.MissingSkills ??= new List<string>();
                    r.AffindaSkills ??= new List<string>();
                    r.SkillGapDetail = r.SkillGapDetail ?? "";
                    r.Summary = r.Summary ?? "";

                    if (NullOrEmpty(r.Status))
                    {
                        r.Status = (r.DomainMismatch || r.WrongProfile)
                            ? "NOT MATCHED (DOMAIN MISMATCH)"
                            : (r.RawScore >= input.Threshold ? "MATCHED" : "NOT MATCHED");
                    }
                }

                // ── 5. Persist every result to DB ────────────────────────────────────
                var jdSnippet = input.JdText.Length > 200
                    ? input.JdText[..200] + "…"
                    : input.JdText;

                var toSave = apiResult.Results.Select(r =>
                {
                    double expYears = 0;
                    if (!string.Equals(r.CandidateType, "Fresher",
                            StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(r.Experience))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(
                            r.Experience, @"(\d+(?:\.\d+)?)");
                        if (m.Success)
                            double.TryParse(m.Value,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out expYears);
                    }

                    return new ResumeMatchResult
                    {
                        AnalyzedAt = DateTime.UtcNow,
                        OriginalFileName = r.File,
                        SavedFileName = r.SavedFileName,
                        PublicUrl = r.PublicUrl,
                        CandidateName = r.Name,
                        Email = r.Email,
                        Phone = r.Phone,
                        CandidateType = r.CandidateType,
                        Experience = r.Experience,
                        ExpYears = expYears,
                        RawScore = r.RawScore,
                        DisplayScore = r.NumericScore,
                        IsScoreSuppressed = r.IsScoreSuppressed,
                        Threshold = input.Threshold,
                        Status = r.Status,
                        Tier = r.Tier,
                        TierDescription = r.TierDescription,
                        Domain = r.Domain,
                        JdDomain = r.JdDomain,
                        DomainSwitch = r.DomainSwitch,
                        DomainSwitchFrom = r.DomainSwitchFrom,
                        DomainMismatch = r.DomainMismatch,
                        NearMiss = r.NearMiss,
                        CareerBreak = r.CareerBreak,
                        CareerBreakDetail = r.CareerBreakDetail,
                        EmploymentGap = r.EmploymentGap,
                        EmploymentGapDetail = r.EmploymentGapDetail,
                        MatchedSkills = string.Join(",", r.MatchedSkills ?? new()),
                        MissingSkills = string.Join(",", r.MissingSkills ?? new()),
                        SkillCoveragePct = r.SkillCoveragePct,
                        TotalJdSkills = r.TotalJdSkills,
                        StayingLocation = r.StayingLocation,
                        WorkLocations = r.Locations,
                        CompanyCount = r.CompanyCount,
                        Education = r.Education,
                        Internship = r.Internship,
                        Projects = r.Projects,
                        JdSnippet = jdSnippet,
                        Summary = r.Summary
                    };
                }).ToList();

                _db.ResumeMatchResults.AddRange(toSave);
                await _db.SaveChangesAsync();

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
                    $"Could not reach the Python API. Is the Flask server running? Detail: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Python API timed out.");
                return ErrorView(input,
                    "Request timed out. The API may still be loading models — please wait and try again.");
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

        // ═════════════════════════════════════════════════════════════════
        //  GET  /ResumeMatcher/DownloadResume
        // ═════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult DownloadResume(string savedFileName, string originalName)
        {
            if (string.IsNullOrWhiteSpace(savedFileName))
                return BadRequest("Filename is required.");

            var safeName = Path.GetFileName(savedFileName);
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", safeName);

            if (!System.IO.File.Exists(uploadsPath))
            {
                _logger.LogWarning("Download requested for missing file: {File}", safeName);
                return NotFound($"File '{safeName}' not found. Please re-run the analysis.");
            }

            var ext = Path.GetExtension(safeName).ToLowerInvariant();
            var downloadName = string.IsNullOrWhiteSpace(originalName)
                ? safeName : Path.GetFileName(originalName);

            return PhysicalFile(uploadsPath, MimeType(ext), downloadName);
        }

        // ═════════════════════════════════════════════════════════════════
        //  GET  /ResumeMatcher/HealthCheck
        // ═════════════════════════════════════════════════════════════════
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

        // ═════════════════════════════════════════════════════════════════
        //  Private helpers
        // ═════════════════════════════════════════════════════════════════

        private IActionResult ErrorView(ResumeMatcherInput input, string msg) =>
            View("Index", new ResumeMatcherViewModel
            {
                JdText = input.JdText,
                Threshold = input.Threshold,
                ErrorMsg = msg
            });

        private static bool NullOrEmpty(string? s) => string.IsNullOrWhiteSpace(s);

        private static string MimeType(string ext) => ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        private static string TryExtractErrorMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return err.GetString() ?? json;
            }
            catch { /* ignore */ }
            return json.Length > 300 ? json[..300] + "…" : json;
        }
    }
}