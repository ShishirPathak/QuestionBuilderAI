"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function DashboardPage() {
  const router = useRouter();

  useEffect(() => {
    const loggedIn = window.localStorage.getItem("qbai_isLoggedIn");
    if (loggedIn !== "true") {
      router.replace("/login");
    }
  }, [router]);

  const handleSignOut = () => {
    window.localStorage.removeItem("qbai_isLoggedIn");
    router.replace("/login");
  };

  const tools = [
    {
      id: "question-paper",
      name: "Question Paper Builder",
      description:
        "Upload handwritten question paper photos and get a clean, printable DOCX in your school’s format.",
      path: "/tools/question-paper",
      icon: "📄",
      badge: "AI-powered",
    },
  ];

  return (
    <main className="min-h-screen bg-slate-50 px-4 py-6 flex justify-center">
      <div className="w-full max-w-6xl">
        {/* Top bar */}
        <div className="flex items-center justify-between mb-4">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">
              Teacher Dashboard
            </h1>
            <p className="text-sm text-slate-500">
              Manage your tools and generate question papers easily.
            </p>
          </div>

          {/* Sign out button */}
          <button
            onClick={handleSignOut}
            className="text-xs font-medium bg-red-100 text-red-700 hover:bg-red-200 px-3 py-1.5 rounded"
          >
            Sign Out
          </button>
        </div>

        {/* Highlight card */}
        <section className="mb-6">
          <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-emerald-500 via-sky-500 to-blue-600 text-white shadow-md">
            <div className="p-5 md:p-6 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
              <div>
                <p className="text-xs uppercase tracking-wider font-semibold text-emerald-100">
                  New • QuestionBuilder AI
                </p>
                <h2 className="mt-1 text-lg md:text-xl font-bold">
                  Turn handwritten questions into printable papers.
                </h2>
                <p className="mt-1 text-sm text-emerald-50 max-w-xl">
                  Upload question paper photos and let AI extract and format
                  everything automatically.
                </p>

                {/* Changed arrow → now smoother */}
                <button
                  onClick={() => router.push("/tools/question-paper")}
                  className="mt-3 inline-flex items-center gap-2 rounded-md bg-white/95 px-4 py-2 text-xs md:text-sm font-medium text-emerald-700 shadow-sm hover:bg-white group cursor-pointer"
                >
                  Open Question Paper Builder
                  <span className="transform transition-transform group-hover:translate-x-1.5">
                    →
                  </span>
                </button>
              </div>

              <div className="hidden md:flex flex-col items-end text-right text-xs text-emerald-50">
                <span className="inline-flex items-center gap-1 rounded-full bg-white/10 px-3 py-1 mb-2">
                  <span className="text-[10px]">⚡</span>
                  <span className="uppercase tracking-wide font-semibold">
                    Teacher friendly
                  </span>
                </span>
                <p>• Supports multiple pages</p>
                <p>• Auto Q.No ordering</p>
                <p>• Works with handwritten text</p>
              </div>
            </div>
          </div>
          <div className="h-4" />
        </section>

        {/* Main layout: tools + tips */}
        <div className="grid grid-cols-1 lg:grid-cols-[2fr,1fr] gap-4">
          {/* Tools grid */}
          <section>
            <h3 className="text-sm font-semibold text-slate-700 mb-3">
              Your tools
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {tools.map((tool) => (
                <button
                  key={tool.id}
                  onClick={() => router.push(tool.path)}
                  className="group h-full text-left rounded-xl border border-slate-200 bg-white hover:border-emerald-400 hover:shadow-sm transition-all p-4 flex flex-col justify-between"
                >
                  <div className="flex items-start gap-3">
                    <div className="text-2xl">{tool.icon}</div>
                    <div>
                      <div className="flex items-center gap-2">
                        <h4 className="text-sm font-semibold text-slate-800">
                          {tool.name}
                        </h4>
                        {tool.badge && (
                          <span className="inline-flex items-center rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-medium text-emerald-700">
                            {tool.badge}
                          </span>
                        )}
                      </div>
                      <p className="mt-1 text-xs text-slate-500">
                        {tool.description}
                      </p>
                    </div>
                  </div>

                  <div className="mt-3 flex items-center justify-between text-[11px] text-slate-400 cursor-pointer">
                    <span className="group-hover:text-emerald-600 flex items-center gap-1">
                      Open tool
                      <span className="transform transition-transform group-hover:translate-x-1">
                        →
                      </span>
                    </span>
                    <span>Last used: just now</span>
                  </div>
                </button>
              ))}
            </div>
          </section>

          {/* Side info */}
          <aside className="space-y-4">
            <div className="bg-white rounded-xl border border-slate-200 p-4">
              <h3 className="text-sm font-semibold text-slate-700 mb-2">
                Quick tips
              </h3>
              <ul className="space-y-2 text-xs text-slate-500">
                <li>• Use good lighting when clicking question paper photos.</li>
                <li>• Try to keep the full page visible in each image.</li>
                <li>
                  • Upload all pages of the question paper together; AI will
                  reorder them.
                </li>
              </ul>
            </div>

            
          </aside>
        </div>
      </div>
    </main>
  );
}
