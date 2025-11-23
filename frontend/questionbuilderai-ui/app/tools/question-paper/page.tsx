"use client";

import { ChangeEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";

type Question = {
  number: number;
  text: string;
  marks: number;
  language?: string;
};

type Section = {
  name: string;
  instructions: string;
  questions: Question[];
};

type ExamPaperModel = {
  schoolName: string;
  examTitle: string;
  class: string;
  subject: string;
  maxMarks: number;
  duration: string;
  sections: Section[];
};

export default function QuestionPaperToolPage() {
  const router = useRouter();

  const [schoolName, setSchoolName] = useState<string>(
    "ABC Public School, Ranchi"
  );
  const [examTitle, setExamTitle] = useState<string>(
    "Half-Yearly Examination – 2025"
  );
  const [className, setClassName] = useState<string>("9");
  const [subject, setSubject] = useState<string>("Sanskrit");
  const [maxMarks, setMaxMarks] = useState<number>(80);
  const [duration, setDuration] = useState<string>("3 Hours");

  const [sectionName, setSectionName] = useState<string>("Section A");
  const [instructions, setInstructions] =
    useState<string>("उत्तर लिखिए।");

  const [questionsText, setQuestionsText] = useState<string>(
    "योगः कर्मसु कौशलम् का अर्थ लिखिए।\nExplain the law of gravitation."
  );

  const [files, setFiles] = useState<File[]>([]);
  const [loadingManual, setLoadingManual] = useState<boolean>(false);
  const [loadingImage, setLoadingImage] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const loggedIn = window.localStorage.getItem("qbai_isLoggedIn");
    if (loggedIn !== "true") {
      router.replace("/login");
    }
  }, [router]);

  const apiBase = process.env
    .NEXT_PUBLIC_API_BASE_URL as string | undefined;

  const buildExamModelFromQuestionsText = (): ExamPaperModel => {
    const lines = questionsText
      .split("\n")
      .map((l) => l.trim())
      .filter((l) => l.length > 0);

    if (lines.length === 0) {
      throw new Error("Please enter at least one question.");
    }

    const questions: Question[] = lines.map((text, index) => ({
      number: index + 1,
      text,
      marks: 2, // default for now
      language: "",
    }));

    return {
      schoolName,
      examTitle,
      class: className,
      subject,
      maxMarks: Number(maxMarks),
      duration,
      sections: [
        {
          name: sectionName,
          instructions,
          questions,
        },
      ],
    };
  };

  const downloadBlobAsFile = (blob: Blob, filename: string) => {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    window.URL.revokeObjectURL(url);
  };

  const handleGenerateFromManual = async () => {
    setError("");
    setLoadingManual(true);

    try {
      if (!apiBase) {
        throw new Error("API base URL is not configured.");
      }

      const model = buildExamModelFromQuestionsText();

      const response = await fetch(`${apiBase}/api/QuestionPaper/generate`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(model),
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(`API error: ${response.status} ${text}`);
      }

      const blob = await response.blob();
      const safeSubject = subject.replace(/\s+/g, "_");
      downloadBlobAsFile(blob, `QuestionPaper_${safeSubject}.docx`);
    } catch (err: any) {
      console.error(err);
      setError(err.message || "Something went wrong.");
    } finally {
      setLoadingManual(false);
    }
  };

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selected = Array.from(e.target.files ?? []);
    setFiles(selected);
  };

  const handleGenerateFromImage = async () => {
    setError("");

    if (files.length === 0) {
      setError("Please select at least one image file.");
      return;
    }

    setLoadingImage(true);

    try {
      if (!apiBase) {
        throw new Error("API base URL is not configured.");
      }

      const formData = new FormData();
      formData.append("schoolName", schoolName);
      formData.append("examTitle", examTitle);
      formData.append("class", className);
      formData.append("subject", subject);
      formData.append("maxMarks", String(maxMarks));
      formData.append("duration", duration);

      files.forEach((file) => {
        formData.append("files", file);
      });

      // /from-image endpoint not implemented yet
      const response = await fetch(
        `${apiBase}/api/QuestionPaper/from-image`,
        {
          method: "POST",
          body: formData,
        }
      );

      if (!response.ok) {
        const text = await response.text();
        throw new Error(`API error: ${response.status} ${text}`);
      }

      const blob = await response.blob();
      const safeSubject = subject.replace(/\s+/g, "_");
      downloadBlobAsFile(
        blob,
        `QuestionPaper_FromImage_${safeSubject}.docx`
      );
    } catch (err: any) {
      console.error(err);
      setError(
        err.message ||
          "Something went wrong while generating from image. (Endpoint may not be implemented yet.)"
      );
    } finally {
      setLoadingImage(false);
    }
  };

  return (
    <main className="min-h-screen px-4 py-6 flex justify-center">
      <div className="w-full max-w-5xl">
        <button
          onClick={() => router.push("/dashboard")}
          className="text-xs text-slate-500 hover:text-sky-600 mb-2"
        >
          ← Back to dashboard
        </button>

        <h1 className="text-2xl font-bold text-slate-800 mb-1">
          Question Paper Generator
        </h1>
        <p className="text-sm text-slate-500 mb-4">
          Generate formatted question papers from typed questions or scanned
          images.
        </p>

        {error && (
          <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md px-3 py-2">
            {error}
          </div>
        )}

        {/* Exam details */}
        <section className="mb-6 bg-white rounded-xl shadow p-4">
          <h2 className="text-sm font-semibold text-slate-700 mb-3">
            Exam Details
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-medium text-slate-700">
                School Name
              </label>
              <input
                type="text"
                value={schoolName}
                onChange={(e) => setSchoolName(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Exam Title
              </label>
              <input
                type="text"
                value={examTitle}
                onChange={(e) => setExamTitle(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Class
              </label>
              <input
                type="text"
                value={className}
                onChange={(e) => setClassName(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Subject
              </label>
              <input
                type="text"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Max Marks
              </label>
              <input
                type="number"
                value={maxMarks}
                onChange={(e) =>
                  setMaxMarks(Number(e.target.value) || 0)
                }
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Duration
              </label>
              <input
                type="text"
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
          </div>
        </section>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {/* Manual mode */}
          <section className="bg-white rounded-xl shadow p-4">
            <h2 className="text-sm font-semibold text-slate-700 mb-3">
              Mode A – Typed / Pasted Questions
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-3">
              <div>
                <label className="block text-xs font-medium text-slate-700">
                  Section Name
                </label>
                <input
                  type="text"
                  value={sectionName}
                  onChange={(e) => setSectionName(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-slate-700">
                  Instructions
                </label>
                <input
                  type="text"
                  value={instructions}
                  onChange={(e) => setInstructions(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700">
                Questions (one per line)
              </label>
              <textarea
                rows={8}
                value={questionsText}
                onChange={(e) => setQuestionsText(e.target.value)}
                className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                placeholder={`Type each question on a new line.\nExample:\nप्रकृति के नियम समझाइए।\nExplain the law of gravitation.`}
              />
              <p className="mt-1 text-xs text-slate-400">
                For now, each question gets default 2 marks. We can add
                per-question marks later.
              </p>
            </div>

            <button
              onClick={handleGenerateFromManual}
              disabled={loadingManual}
              className="mt-3 inline-flex items-center justify-center rounded-md bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-700 disabled:opacity-60"
            >
              {loadingManual
                ? "Generating..."
                : "Generate DOCX from Typed Questions"}
            </button>
          </section>

          {/* Image mode */}
          <section className="bg-white rounded-xl shadow p-4">
            <h2 className="text-sm font-semibold text-slate-700 mb-3">
              Mode B – Upload Question Paper Image (AI)
            </h2>
            <p className="text-xs text-slate-500 mb-3">
              Upload scanned or photographed question papers (handwritten or
              printed). The system will use OCR + AI to extract questions and
              generate a formatted DOCX. (Backend endpoint still needs to be
              implemented.)
            </p>

            <div>
              <label className="block text-xs font-medium text-slate-700">
                Upload Images
              </label>
              <input
                type="file"
                accept="image/*"
                multiple
                onChange={handleFileChange}
                className="mt-1 block w-full text-sm"
              />
              {files.length > 0 && (
                <p className="mt-1 text-xs text-slate-500">
                  Selected {files.length} file(s).
                </p>
              )}
            </div>

            <button
              onClick={handleGenerateFromImage}
              disabled={loadingImage}
              className="mt-3 inline-flex items-center justify-center rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {loadingImage
                ? "Processing images..."
                : "Generate DOCX from Image (AI)"}
            </button>

            <p className="mt-2 text-xs text-amber-500">
              Note: The <code>/api/QuestionPaper/from-image</code> endpoint is
              not implemented yet. We'll add the OCR + AI backend next.
            </p>
          </section>
        </div>
      </div>
    </main>
  );
}
