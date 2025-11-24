using System.IO;
using System.Linq;
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

        public enum QuestionPaperTemplate
        {
            Generic,
            Std5English2
        }

        // ENTRY POINT USED BY CONTROLLERS
        public byte[] GenerateQuestionPaperDocx(ExamPaperModel model)
        {
            var template = ResolveTemplate(model);

            return template switch
            {
                QuestionPaperTemplate.Std5English2 => GenerateStd5English2Paper(model),
                _ => GenerateGenericPaper(model),
            };
        }

        // -----------------------------
        // GENERIC PAPER (EXISTING LOGIC)
        // -----------------------------
        private byte[] GenerateGenericPaper(ExamPaperModel model)
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

        // -----------------------------------------
        // STD V ENGLISH II – SPECIAL FORMATTED PAPER
        // -----------------------------------------
        private byte[] GenerateStd5English2Paper(ExamPaperModel model)
        {
            using var ms = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                       ms, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // HEADER

                // Line 1: Class – V   Date: ________   Subject – English II
                body.Append(CreateParagraph(
                    $"Class – {model.Class}     Date: __________     Subject – {model.Subject}",
                    bold: true));

                // School name centered
                if (!string.IsNullOrWhiteSpace(model.SchoolName))
                    body.Append(CreateParagraph(model.SchoolName, bold: true, center: true));

                // Address line (hard-coded for now; later you can put in model)
                body.Append(CreateParagraph("Satbarwa, Palamau, Jharkhand", center: true));

                // Exam title centered
                if (!string.IsNullOrWhiteSpace(model.ExamTitle))
                    body.Append(CreateParagraph(model.ExamTitle, bold: true, center: true));

                // Time & Full Marks
                body.Append(CreateParagraph(
                    $"Time: {model.Duration}     Full Marks: {model.MaxMarks}",
                    bold: true));

                // Separator
                body.Append(CreateParagraph("---------------------------------------------", center: true));

                // QUESTION GROUPS (Q.No. style)
                foreach (var group in model.Sections)
                {
                    // Group title (e.g., "Q.No.1 Answer in one word.")
                    string groupTitle = group.Name ?? string.Empty;

                    int groupMarks = 0;
                    if (group.Questions.Any() && group.Questions[0].Marks > 0)
                        groupMarks = group.Questions[0].Marks;

                    // "Q.No.1 Answer in one word.   (10)"
                    body.Append(CreateParagraph(
                        $"{groupTitle}    ({groupMarks})",
                        bold: true));

                    // Sub questions: (a), (b), (c) etc.
                    foreach (var q in group.Questions)
                    {
                        char label = (char)('a' + (q.Number - 1)); // 1 -> 'a', 2 -> 'b', etc.
                        body.Append(CreateParagraph($"{label}) {q.Text}"));
                    }

                    // Blank line after each group
                    body.Append(CreateParagraph(""));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return ms.ToArray();
        }

        // -----------------------------
        // HELPER: CREATE PARAGRAPH
        // -----------------------------
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

            run.Append(new Text(text ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            var paraProps = new ParagraphProperties();
            if (center)
                paraProps.Justification = new Justification { Val = JustificationValues.Center };

            var paragraph = new Paragraph();
            paragraph.Append(paraProps);
            paragraph.Append(run);

            return paragraph;
        }

        // -----------------------------
        // TEMPLATE RESOLUTION LOGIC
        // -----------------------------
        private QuestionPaperTemplate ResolveTemplate(ExamPaperModel model)
        {
            var cls = (model.Class ?? string.Empty).Trim().ToLowerInvariant();
            var subject = (model.Subject ?? string.Empty).Trim().ToLowerInvariant();

            var isStd5 =
                cls == "v" ||
                cls == "5" ||
                cls == "std v" ||
                cls == "standard v";

            var isEnglish2 =
                subject == "english ii" ||
                subject == "english 2" ||
                subject == "english second";

            if (isStd5 && isEnglish2)
            {
                return QuestionPaperTemplate.Std5English2;
            }

            return QuestionPaperTemplate.Generic;
        }
    }
}
