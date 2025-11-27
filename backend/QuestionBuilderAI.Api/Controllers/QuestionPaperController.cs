using Microsoft.AspNetCore.Mvc;
using QuestionBuilderAI.Api.Models;
using QuestionBuilderAI.Api.Services;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

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

            // 1) Call OCR service
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
                // Parse as JsonNode so we can fix bad maxMarks
                var root = JsonNode.Parse(json)?.AsObject();
                if (root is null)
                {
                    return StatusCode(500, "OCR service returned empty JSON.");
                }

                // Overwrite maxMarks with a clean numeric value from UI
                // (ignore whatever OCR sent)
                root["maxMarks"] = maxMarks;

                // Optional: also normalize duration if you want
                // root["duration"] = duration;

                var safeJson = root.ToJsonString();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                model = JsonSerializer.Deserialize<ExamPaperModel>(safeJson, options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to parse OCR service response: {ex.Message}");
            }

            if (model == null || model.Sections == null || model.Sections.Count == 0)
            {
                return StatusCode(500, "OCR service returned empty or invalid exam model.");
            }

            // 2) Override meta fields from form (always trust UI values)
            model.SchoolName = schoolName;
            model.ExamTitle = examTitle;
            model.Class = @class;
            model.Subject = subject;
            model.MaxMarks = maxMarks;
            model.Duration = duration;

            // 3) Generate DOCX
            var fileBytes = _service.GenerateQuestionPaperDocx(model);

            var safeSubject = (model.Subject ?? subject ?? "Subject").Replace(" ", "_");
            var fileName = $"QuestionPaper_FromImage_{safeSubject}.docx";

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
    }

}
