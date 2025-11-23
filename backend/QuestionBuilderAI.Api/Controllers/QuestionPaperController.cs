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

        public QuestionPaperController(QuestionPaperService service)
        {
            _service = service;
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

            // 1) Send to Python OCR+AI service
            using var httpClient = new HttpClient();

            // NOTE: adjust URL if needed
            var pythonServiceUrl = "http://127.0.0.1:8000/ocr/parse-question-paper";

            using var form = new MultipartFormDataContent();

            // Add simple fields
            form.Add(new StringContent(schoolName), "schoolName");
            form.Add(new StringContent(examTitle), "examTitle");
            form.Add(new StringContent(@class), "className");
            form.Add(new StringContent(subject), "subject");
            form.Add(new StringContent(maxMarks.ToString()), "maxMarks");
            form.Add(new StringContent(duration), "duration");

            // Add files
            foreach (var file in files)
            {
                if (file.Length <= 0)
                    continue;

                var streamContent = new StreamContent(file.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                // "files" must match FastAPI parameter name
                form.Add(streamContent, "files", file.FileName);
            }

            HttpResponseMessage ocrResponse;
            try
            {
                ocrResponse = await httpClient.PostAsync(pythonServiceUrl, form);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calling OCR service: {ex.Message}");
            }

            if (!ocrResponse.IsSuccessStatusCode)
            {
                var errorText = await ocrResponse.Content.ReadAsStringAsync();
                return StatusCode((int)ocrResponse.StatusCode,
                    $"OCR service error: {ocrResponse.StatusCode} {errorText}");
            }

            var json = await ocrResponse.Content.ReadAsStringAsync();

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

            // 2) Generate DOCX using existing service
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
