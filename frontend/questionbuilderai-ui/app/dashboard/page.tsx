"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

type Tool = {
  id: string;
  title: string;
  description: string;
  icon: string;
  href: string;
};

export default function DashboardPage() {
  const router = useRouter();

  useEffect(() => {
    const loggedIn = window.localStorage.getItem("qbai_isLoggedIn");
    if (loggedIn !== "true") {
      router.replace("/login");
    }
  }, [router]);

  const tools: Tool[] = [
    {
      id: "question-paper",
      title: "Question Paper Generator",
      description:
        "Create formatted question papers from typed text or images using AI.",
      icon: "📄",
      href: "/tools/question-paper",
    },
    // Add future tools here
  ];

  return (
    <main className="min-h-screen px-4 py-8 flex justify-center">
      <div className="w-full max-w-4xl">
        <header className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">
              QuestionBuilderAI – Teacher Tools
            </h1>
            <p className="text-sm text-slate-500">
              Welcome! Choose a tool below.
            </p>
          </div>
          <button
            onClick={() => {
              window.localStorage.removeItem("qbai_isLoggedIn");
              router.replace("/login");
            }}
            className="text-xs text-slate-500 hover:text-red-500 underline"
          >
            Logout
          </button>
        </header>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {tools.map((tool) => (
            <button
              key={tool.id}
              onClick={() => router.push(tool.href)}
              className="flex flex-col items-start text-left p-4 bg-white rounded-xl shadow hover:shadow-md transition-shadow"
            >
              <div className="text-3xl mb-2">{tool.icon}</div>
              <h2 className="text-lg font-semibold text-slate-800">
                {tool.title}
              </h2>
              <p className="text-sm text-slate-500 mt-1">
                {tool.description}
              </p>
            </button>
          ))}
        </div>
      </div>
    </main>
  );
}
