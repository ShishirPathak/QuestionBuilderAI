using Microsoft.AspNetCore.Mvc;
using QuestionBuilderAI.Api.Models;
using QuestionBuilderAI.Api.Services;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

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
        // ✨ NEW ENDPOINT: Upload Images + Generate DOCX (STUB)
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

            // 1) Call OCR + LLM service via injected OcrClient
            string json;
            try
            {
                // OcrClient handles OCR_BASE_URL and multipart form creation
                json = await _ocrClient.ParseQuestionPaperAsync(files.ToArray());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calling OCR service: {ex.Message}");
            }

            // 2) Deserialize returned JSON into our ExamPaperModel
            ExamPaperModel? model;
            try
            {
                model = JsonSerializer.Deserialize<ExamPaperModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to parse OCR service response: {ex.Message}");
            }

            if (model == null || model.Sections == null || model.Sections.Count == 0)
            {
                return StatusCode(500, "OCR service returned empty or invalid exam model.");
            }

            // 3) Override exam meta from form (class/subject etc.) so template selection works
            model.SchoolName = schoolName;
            model.ExamTitle = examTitle;
            model.Class = @class;
            model.Subject = subject;
            model.MaxMarks = maxMarks;
            model.Duration = duration;

            // 4) Generate DOCX
            var fileBytes = _service.GenerateQuestionPaperDocx(model);

            var safeSubject = (model.Subject ?? subject ?? "Subject").Replace(" ", "_");
            var fileName = $"QuestionPaper_FromImage_{safeSubject}.docx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );
        }

    }

}
