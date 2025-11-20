namespace QuestionBuilderAI.Api.Models
{
    public class ExamPaperModel
    {
        public string SchoolName { get; set; } = "";
        public string ExamTitle { get; set; } = "";
        public string Class { get; set; } = "";
        public string Subject { get; set; } = "";
        public int MaxMarks { get; set; }
        public string Duration { get; set; } = "";
        public List<SectionModel> Sections { get; set; } = new();
    }

    public class SectionModel
    {
        public string Name { get; set; } = "";            // e.g. "Section A"
        public string Instructions { get; set; } = "";    // e.g. "Answer any five..."
        public List<QuestionModel> Questions { get; set; } = new();
    }

    public class QuestionModel
    {
        public int Number { get; set; }
        public string Text { get; set; } = "";            // Hindi / English / Sanskrit
        public int Marks { get; set; }
        public string? Language { get; set; }             // optional, if you want to tag
    }
}
