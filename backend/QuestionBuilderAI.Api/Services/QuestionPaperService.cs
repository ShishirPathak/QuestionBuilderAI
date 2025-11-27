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
        public byte[] GenerateGenericPaper(ExamPaperModel model)
        {
            using var ms = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                       ms, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // Header block
                if (!string.IsNullOrWhiteSpace(model.SchoolName))
                    body.Append(CreateParagraph(model.SchoolName, bold: true, center: true));

                if (!string.IsNullOrWhiteSpace(model.ExamTitle))
                    body.Append(CreateParagraph(model.ExamTitle, bold: true, center: true));

                body.Append(CreateParagraph("")); // blank line

                var line1 = $"Class: {model.Class}    Subject: {model.Subject}";
                body.Append(CreateParagraph(line1));

                var line2 = $"Max Marks: {model.MaxMarks}    Duration: {model.Duration}";
                body.Append(CreateParagraph(line2));

                body.Append(CreateParagraph(new string('-', 60)));

                // Sections & questions
                foreach (var section in model.Sections)
                {
                    // Group marks: take from first question (if present)
                    int groupMarks = 0;
                    if (section.Questions != null && section.Questions.Any() && section.Questions[0].Marks > 0)
                        groupMarks = section.Questions[0].Marks;

                    var sectionTitle = section.Name ?? string.Empty;

                    // 👉 Section header with marks on the right
                    if (groupMarks > 0)
                        body.Append(CreateSectionHeaderWithMarks(sectionTitle, groupMarks));
                    else if (!string.IsNullOrWhiteSpace(sectionTitle))
                        body.Append(CreateParagraph(sectionTitle, bold: true));

                    // Optional instructions under section title
                    if (!string.IsNullOrWhiteSpace(section.Instructions))
                        body.Append(CreateParagraph(section.Instructions, italic: true));

                    // Questions: compact spacing, NO per-question marks
                    int qIndex = 1;
                    foreach (var q in section.Questions)
                    {
                        var qText = $"Q{qIndex}. {q.Text}";
                        body.Append(CreateParagraph(qText, compact: true));
                        qIndex++;
                    }

                    // Small gap after each section
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
            bool center = false,
            bool compact = false)   // 👈 new flag
        {
            var runProps = new RunProperties();

            if (bold)
                runProps.Append(new Bold());
            if (italic)
                runProps.Append(new Italic());

            var run = new Run();
            run.Append(runProps);
            run.Append(new Text(text ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            var paraProps = new ParagraphProperties();

            if (center)
                paraProps.Justification = new Justification { Val = JustificationValues.Center };

            // ✅ Control vertical spacing: smaller After for “compact” lines (questions)
            var spacing = new SpacingBetweenLines
            {
                Line = "240",                  // 1.0 line
                LineRule = LineSpacingRuleValues.Auto,
                After = compact ? "0" : "120"  // 0 = no extra space, 120 = ~6pt
            };
            paraProps.Append(spacing);

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

        public byte[] GenerateHindiKrutiDevPaper(ExamPaperModel model)
        {
            using var ms = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                       ms, WordprocessingDocumentType.Document, true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                // Header – same structure as generic, but font = KrutiDev
                if (!string.IsNullOrWhiteSpace(model.SchoolName))
                    body.Append(CreateParagraphWithFont(model.SchoolName, "Kruti Dev 010", bold: true, center: true));

                if (!string.IsNullOrWhiteSpace(model.ExamTitle))
                    body.Append(CreateParagraphWithFont(model.ExamTitle, "Kruti Dev 010", bold: true, center: true));

                body.Append(CreateParagraphWithFont("", "Kruti Dev 010"));

                var line1 = $"Class: {model.Class}    Subject: {model.Subject}";
                body.Append(CreateParagraphWithFont(line1, "Kruti Dev 010"));

                var line2 = $"Max Marks: {model.MaxMarks}    Duration: {model.Duration}";
                body.Append(CreateParagraphWithFont(line2, "Kruti Dev 010"));

                body.Append(CreateParagraphWithFont(new string('-', 60), "Kruti Dev 010", center: true));

                // Sections & questions
                foreach (var section in model.Sections)
                {
                    if (!string.IsNullOrWhiteSpace(section.Name))
                        body.Append(CreateParagraphWithFont(section.Name, "Kruti Dev 010", bold: true));

                    if (!string.IsNullOrWhiteSpace(section.Instructions))
                        body.Append(CreateParagraphWithFont(section.Instructions, "Kruti Dev 010", italic: true));

                    foreach (var q in section.Questions)
                    {
                        var qText = $"Q{q.Number}. {q.Text}   [{q.Marks} Marks]";
                        body.Append(CreateParagraphWithFont(qText, "Kruti Dev 010"));
                    }

                    body.Append(CreateParagraphWithFont("", "Kruti Dev 010"));
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return ms.ToArray();
        }
        private static Paragraph CreateParagraphWithFont(
    string text,
    string fontName,
    bool bold = false,
    bool italic = false,
    bool center = false)
        {
            var runProps = new RunProperties();

            runProps.Append(new RunFonts
            {
                Ascii = fontName,
                HighAnsi = fontName,
                ComplexScript = fontName
            });

            if (bold)
                runProps.Append(new Bold());
            if (italic)
                runProps.Append(new Italic());

            var run = new Run();
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

        private static Paragraph CreateSectionHeaderWithMarks(string title, int marks)
        {
            // Paragraph props with a right tab stop near the right margin
            var paraProps = new ParagraphProperties();

            var tabs = new Tabs();
            tabs.Append(new TabStop
            {
                Val = TabStopValues.Right,
                Position = 9000   // position in twentieths of a point (~6.25 in)
            });

            paraProps.Append(tabs);

            var spacing = new SpacingBetweenLines
            {
                Line = "240",
                LineRule = LineSpacingRuleValues.Auto,
                After = "120"
            };
            paraProps.Append(spacing);

            var paragraph = new Paragraph();
            paragraph.Append(paraProps);

            // Left text: section title
            var runTitle = new Run(new Text(title ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });
            runTitle.RunProperties = new RunProperties(new Bold());

            // Tab to the right
            var runTab = new Run(new TabChar());

            // Right text: marks
            var runMarks = new Run(new Text(marks.ToString())
            {
                Space = SpaceProcessingModeValues.Preserve
            });
            runMarks.RunProperties = new RunProperties(new Bold());

            paragraph.Append(runTitle);
            paragraph.Append(runTab);
            paragraph.Append(runMarks);

            return paragraph;
        }




    }
}
