using Microsoft.AspNetCore.Mvc;
using QuestionBuilderAI.Api.Models;
using QuestionBuilderAI.Api.Services;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace QuestionBuilderAI.Api1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionPaperController : ControllerBase
    {
        private readonly QuestionPaperService _service;
        private readonly OcrClient _ocrClient;

        public QuestionPaperController(QuestionPaperService service, OcrClient ocrClient)
        {
            _service = service;
            _ocrClient = ocrClient;

        }

        [HttpPost("generate")]
        public IActionResult Generate([FromBody] ExamPaperModel model)
        {
            if (model == null || model.Sections == null || model.Sections.Count == 0)
                return BadRequest("Invalid exam paper model.");

            var bytes = _service.GenerateQuestionPaperDocx(model);
            var fileName = $"QuestionPaper_{model.Subject}_{DateTime.UtcNow:yyyyMMddHHmm}.docx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
        [HttpPost("from-image")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> GenerateFromImage(
    [FromForm] string schoolName,
    [FromForm] string examTitle,
    [FromForm(Name = "class")] string @class,
    [FromForm] string subject,
    [FromForm] int maxMarks,
    [FromForm] string duration,
    [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files were uploaded.");

            string json;
            try
            {
                json = await _ocrClient.ParseQuestionPaperAsync(files.ToArray());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calling OCR service: {ex.Message}");
            }

            ExamPaperModel? model;
            try
            {
                // ✅ Normalize maxMarks + question marks before deserialization
                var normalizedJson = NormalizeOcrJson(json, maxMarks, duration);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                model = JsonSerializer.Deserialize<ExamPaperModel>(normalizedJson, options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to parse OCR service response: {ex.Message}");
            }

            if (model == null || model.Sections == null || model.Sections.Count == 0)
            {
                return StatusCode(500, "OCR service returned empty or invalid exam model.");
            }

            // Override meta with trusted form values
            model.SchoolName = schoolName;
            model.ExamTitle = examTitle;
            model.Class = @class;
            model.Subject = subject;
            model.MaxMarks = maxMarks;
            model.Duration = duration;

            var fileBytes = _service.GenerateQuestionPaperDocx(model);

            var safeClass = (model.Class ?? @class ?? "Class").Replace(" ", "_");
            var safeSubject = (model.Subject ?? subject ?? "Subject").Replace(" ", "_");

            var fileName = $"{safeClass}_{safeSubject}_QuestionPaper.docx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );

        }



        // [HttpPost("from-image-hindi-krutidev")]
        //     [DisableRequestSizeLimit]
        //     public async Task<IActionResult> GenerateFromImageHindiKrutiDev(
        // [FromForm] string schoolName,
        // [FromForm] string examTitle,
        // [FromForm(Name = "class")] string @class,
        // [FromForm] string subject,
        // [FromForm] int maxMarks,
        // [FromForm] string duration,
        // [FromForm] List<IFormFile> files)
        //     {
        //         if (files == null || files.Count == 0)
        //             return BadRequest("No files were uploaded.");

        //         // 1) Call OCR service (same as English flow)
        //         string json;
        //         try
        //         {
        //             json = await _ocrClient.ParseQuestionPaperAsync(files.ToArray());
        //         }
        //         catch (Exception ex)
        //         {
        //             return StatusCode(500, $"Error calling OCR service: {ex.Message}");
        //         }

        //         // 2) Deserialize
        //         ExamPaperModel? model;
        //         try
        //         {
        //             model = JsonSerializer.Deserialize<ExamPaperModel>(json, new JsonSerializerOptions
        //             {
        //                 PropertyNameCaseInsensitive = true
        //             });
        //         }
        //         catch (Exception ex)
        //         {
        //             return StatusCode(500, $"Failed to parse OCR service response: {ex.Message}");
        //         }

        //         if (model == null || model.Sections == null || model.Sections.Count == 0)
        //         {
        //             return StatusCode(500, "OCR service returned empty or invalid exam model.");
        //         }

        //         // 3) Override exam meta
        //         model.SchoolName = schoolName;
        //         model.ExamTitle = examTitle;
        //         model.Class = @class;
        //         model.Subject = subject;
        //         model.MaxMarks = maxMarks;
        //         model.Duration = duration;

        //         // 4) Generate DOCX using special Hindi + KrutiDev flow
        //         var fileBytes = _service.GenerateHindiKrutiDevPaper(model);

        //         var safeSubject = (model.Subject ?? subject ?? "Hindi").Replace(" ", "_");
        //         var fileName = $"QuestionPaper_Hindi_KrutiDev_{safeSubject}.docx";

        //         return File(
        //             fileBytes,
        //             "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        //             fileName
        //         );
        //     }

        private static string NormalizeOcrJson(string json, int maxMarksFromForm, string durationFromForm)
        {
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null)
                throw new InvalidOperationException("OCR JSON root is null.");

            // Always trust form values for maxMarks & duration
            root["maxMarks"] = maxMarksFromForm;
            root["duration"] = durationFromForm;

            // Normalize question marks: sections[*].questions[*].marks
            if (root.TryGetPropertyValue("sections", out var sectionsNode) &&
                sectionsNode is JsonArray sectionsArray)
            {
                foreach (var sectionNode in sectionsArray)
                {
                    if (sectionNode is not JsonObject sectionObj) continue;

                    if (!sectionObj.TryGetPropertyValue("questions", out var questionsNode) ||
                        questionsNode is not JsonArray questionsArray)
                        continue;

                    foreach (var qNode in questionsArray)
                    {
                        if (qNode is not JsonObject qObj) continue;

                        if (!qObj.TryGetPropertyValue("marks", out var marksNode) || marksNode is null)
                        {
                            // If no marks given, default 0
                            qObj["marks"] = 0;
                            continue;
                        }

                        // Convert whatever is there into a clean int
                        var marksStr = marksNode.ToString().Trim();

                        // Try direct int parse
                        if (!int.TryParse(marksStr, out var marksInt))
                        {
                            // Fallback: extract first number from string ("2 marks" -> 2)
                            var match = Regex.Match(marksStr, @"\d+");
                            if (match.Success && int.TryParse(match.Value, out var extracted))
                            {
                                marksInt = extracted;
                            }
                            else
                            {
                                marksInt = 0; // last-resort default
                            }
                        }

                        qObj["marks"] = marksInt;
                    }
                }
            }

            return root.ToJsonString();
        }
    }

}
