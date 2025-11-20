using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QuestionBuilderAI.Api.Models;

namespace QuestionBuilderAI.Api.Services
{
    public class QuestionPaperService
    {
        public QuestionPaperService(IWebHostEnvironment env)
        {
            // Keeping the constructor in case we later want templates from disk
        }

        public byte[] GenerateQuestionPaperDocx(ExamPaperModel model)
        {
            using var ms = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                       ms, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // School name (centered, bold)
                if (!string.IsNullOrWhiteSpace(model.SchoolName))
                    body.Append(CreateParagraph(model.SchoolName, bold: true, center: true));

                // Exam title (centered, bold)
                if (!string.IsNullOrWhiteSpace(model.ExamTitle))
                    body.Append(CreateParagraph(model.ExamTitle, bold: true, center: true));

                // Blank line
                body.Append(CreateParagraph(""));

                // Class / Subject
                var line1 = $"Class: {model.Class}    Subject: {model.Subject}";
                body.Append(CreateParagraph(line1));

                // Max marks / Duration
                var line2 = $"Max Marks: {model.MaxMarks}    Duration: {model.Duration}";
                body.Append(CreateParagraph(line2));

                // Separator
                body.Append(CreateParagraph(new string('-', 60)));

                // Sections & Questions
                foreach (var section in model.Sections)
                {
                    // Section name
                    if (!string.IsNullOrWhiteSpace(section.Name))
                        body.Append(CreateParagraph(section.Name, bold: true));

                    // Section instructions
                    if (!string.IsNullOrWhiteSpace(section.Instructions))
                        body.Append(CreateParagraph(section.Instructions, italic: true));

                    // Questions
                    foreach (var q in section.Questions)
                    {
                        var qText = $"Q{q.Number}. {q.Text}   [{q.Marks} Marks]";
                        body.Append(CreateParagraph(qText));
                    }

                    // Blank line after section
                    body.Append(CreateParagraph(""));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return ms.ToArray();
        }

        private static Paragraph CreateParagraph(
            string text,
            bool bold = false,
            bool italic = false,
            bool center = false)
        {
            var runProps = new RunProperties();
            if (bold)
                runProps.Append(new Bold());
            if (italic)
                runProps.Append(new Italic());

            var run = new Run();
            if (runProps.HasChildren)
                run.Append(runProps);
            run.Append(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });

            var paraProps = new ParagraphProperties();
            if (center)
                paraProps.Justification = new Justification { Val = JustificationValues.Center };

            var paragraph = new Paragraph();
            paragraph.Append(paraProps);
            paragraph.Append(run);

            return paragraph;
        }
    }
}
