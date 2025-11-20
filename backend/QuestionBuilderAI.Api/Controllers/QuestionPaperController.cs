using Microsoft.AspNetCore.Mvc;
using QuestionBuilderAI.Api.Models;
using QuestionBuilderAI.Api.Services;

namespace QuestionBuilderAI.Api.Controllers
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
    }
}
