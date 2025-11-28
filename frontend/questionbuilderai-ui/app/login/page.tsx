"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";

const HARDCODED_PASSWORD = "teacher123"; // change if you like

export default function LoginPage() {
  const router = useRouter();
  const [password, setPassword] = useState<string>("");
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const loggedIn = window.localStorage.getItem("qbai_isLoggedIn");
    if (loggedIn === "true") {
      router.replace("/dashboard");
    }
  }, [router]);

  const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    if (password === HARDCODED_PASSWORD) {
      window.localStorage.setItem("qbai_isLoggedIn", "true");
      router.push("/dashboard");
    } else {
      setError("Incorrect password.");
    }
  };

  return (
    <main className="min-h-screen flex items-center justify-center">
      <div className="w-full max-w-md bg-white shadow-md rounded-xl p-6 mx-4">
        <h1 className="text-2xl font-bold text-slate-800 mb-2 text-center">
          QuestionBuilderAI Portal
        </h1>
        <p className="text-sm text-slate-500 mb-4 text-center">
          Login to access teacher tools.
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700">
              Password
            </label>
            <input
              type="password"
              className="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>

          {error && (
            <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-md px-3 py-2">
              {error}
            </div>
          )}

          <button
            type="submit"
            className="w-full rounded-md bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-700"
          >
            Login
          </button>

        </form>
      </div>
    </main>
  );
}
