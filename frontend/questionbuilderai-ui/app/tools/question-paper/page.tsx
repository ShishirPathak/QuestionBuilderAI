"use client";

import { ChangeEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";

export default function QuestionPaperToolPage() {
  const router = useRouter();

  const [schoolName, setSchoolName] = useState<string>(
    "Indira Gandhi Memorial Public School"
  );
  const [examTitle, setExamTitle] = useState<string>(
    "1st Terminal Examination 2025–2026"
  );
  const [className, setClassName] = useState<string>("V");
  const [subject, setSubject] = useState<string>("English II");
  const [maxMarks, setMaxMarks] = useState<number>(100);
  const [duration, setDuration] = useState<string>("2 Hours");

  const [files, setFiles] = useState<File[]>([]);
  const [previewUrls, setPreviewUrls] = useState<string[]>([]);
  const [loadingImage, setLoadingImage] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const loggedIn = window.localStorage.getItem("qbai_isLoggedIn");
    if (loggedIn !== "true") {
      router.replace("/login");
    }
  }, [router]);

  const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL as string | undefined;

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

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const selected = Array.from(e.target.files ?? []);
    setFiles(selected);

    // Clean up old previews
    previewUrls.forEach((u) => URL.revokeObjectURL(u));

    const urls = selected.map((file) => URL.createObjectURL(file));
    setPreviewUrls(urls);
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
      // Backend expects "class"
      formData.append("class", className);
      formData.append("subject", subject);
      formData.append("maxMarks", String(maxMarks));
      formData.append("duration", duration);

      files.forEach((file) => {
        formData.append("files", file);
      });

      const response = await fetch(`${apiBase}/api/QuestionPaper/from-image`, {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(`API error: ${response.status} ${text}`);
      }

      const blob = await response.blob();
      const safeSubject = subject.replace(/\s+/g, "_");
      downloadBlobAsFile(blob, `QuestionPaper_FromImage_${safeSubject}.docx`);
    } catch (err: any) {
      console.error(err);
      setError(
        err.message || "Something went wrong while generating from image."
      );
    } finally {
      setLoadingImage(false);
    }
  };

  return (
    <main className="min-h-screen px-4 py-6 flex justify-center bg-slate-50">
      <div className="w-full max-w-5xl">
        <button
          onClick={() => router.push("/dashboard")}
          className="text-xs text-slate-500 hover:text-sky-600 mb-2"
        >
          ← Back to dashboard
        </button>

        <header className="mb-4">
          <h1 className="text-2xl font-bold text-slate-900 mb-1">
            Question Paper Builder (AI)
          </h1>
          <p className="text-sm text-slate-500">
            Upload handwritten question paper photos and get a clean, printable
            DOCX in your school&apos;s format.
          </p>
        </header>

        {error && (
          <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md px-3 py-2">
            {error}
          </div>
        )}

        {/* Exam details */}
        <section className="mb-6 bg-white rounded-2xl shadow-sm border border-slate-100 p-4 md:p-5">
          <h2 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
            <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-sky-100 text-sky-700 text-xs font-bold">
              1
            </span>
            Exam details
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
                className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
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
                className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-slate-600">
                Class
              </label>
              <input
                type="text"
                value={className}
                onChange={(e) => setClassName(e.target.value)}
                placeholder="e.g., V, VI, VII, Std 5, etc."
                className="rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-emerald-500"
              />
            </div>

            {/* Subject (Text Field) */}
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-slate-600">
                Subject
              </label>
              <input
                type="text"
                value={subject}
                onChange={(e) => setSubject(e.target.value)}
                placeholder="e.g., English II, Hindi, Math, EVS"
                className="rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-emerald-500"
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700">
                Max Marks
              </label>
              <input
                type="number"
                value={maxMarks}
                onChange={(e) => setMaxMarks(Number(e.target.value) || 0)}
                className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
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
                className="mt-1 block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
            </div>
          </div>
        </section>

        {/* Image upload + preview */}
        <section className="bg-white rounded-2xl shadow-sm border border-slate-100 p-4 md:p-5">
          <h2 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
            <span className="inline-flex h-6 w-6 items-center justify-center rounded-full bg-emerald-100 text-emerald-700 text-xs font-bold">
              2
            </span>
            Upload handwritten question paper photos
          </h2>

          <p className="text-xs text-slate-500 mb-3">
            You can select multiple pages at once (e.g., Page 1, Page 2, Page
            3...). The system will read them using AI and generate a single
            question paper in your school&apos;s format.
          </p>

          {/* Dropzone-style input */}
          <label className="mt-2 flex flex-col items-center justify-center border-2 border-dashed border-slate-300 hover:border-emerald-400 transition-colors rounded-xl px-4 py-8 cursor-pointer bg-slate-50">
            <input
              type="file"
              accept="image/*"
              multiple
              onChange={handleFileChange}
              className="hidden"
            />
            <span className="inline-flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 text-emerald-700 mb-2">
              📄
            </span>
            <span className="text-sm font-medium text-slate-800">
              Click to upload question paper photos
            </span>
            <span className="text-xs text-slate-500 mt-1">
              JPG, PNG, HEIC – multiple pages allowed
            </span>
            {files.length > 0 && (
              <span className="mt-2 text-xs text-emerald-600 font-medium">
                {files.length} file(s) selected
              </span>
            )}
          </label>

          {/* Preview grid */}
          {previewUrls.length > 0 && (
            <div className="mt-4">
              <h3 className="text-xs font-semibold text-slate-700 mb-2">
                Preview
              </h3>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                {previewUrls.map((url, idx) => (
                  <div
                    key={idx}
                    className="relative rounded-lg overflow-hidden border border-slate-200 bg-slate-100"
                  >
                    <img
                      src={url}
                      alt={`Uploaded page ${idx + 1}`}
                      className="h-40 w-full object-cover"
                    />
                    <div className="absolute bottom-0 left-0 right-0 bg-black/40 text-[10px] text-white px-2 py-1">
                      Page {idx + 1}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="mt-5 flex justify-end">
            <button
              onClick={handleGenerateFromImage}
              disabled={loadingImage || files.length === 0}
              className="inline-flex items-center justify-center rounded-md bg-emerald-600 px-4 py-2 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {loadingImage
                ? "Processing with AI..."
                : "Generate DOCX from Images (AI)"}
            </button>
          </div>
        </section>
      </div>
    </main>
  );
}
