import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "QuestionBuilderAI - Teacher Tools",
  description: "AI-powered tools for teachers by Shishir",
};

type RootLayoutProps = {
  children: React.ReactNode;
};

export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html lang="en">
      <body className="bg-slate-100">{children}</body>
    </html>
  );
}
