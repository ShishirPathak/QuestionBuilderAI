using System.Net.Http.Headers;

namespace QuestionBuilderAI.Api.Services
{
    public class OcrClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _ocrBaseUrl;

        public OcrClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            _ocrBaseUrl =
                Environment.GetEnvironmentVariable("OCR_BASE_URL")
                ?? config["Ocr:BaseUrl"]
                ?? "http://localhost:8001"; // fallback
        }

        public async Task<string> ParseQuestionPaperAsync(IFormFile[] files)
        {
            using var form = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                form.Add(content, "files", file.FileName);
            }

            var response = await _httpClient.PostAsync($"{_ocrBaseUrl}/ocr/parse-question-paper", form);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
