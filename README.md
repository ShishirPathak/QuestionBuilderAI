# QuestionBuilderAI

QuestionBuilderAI is a simple AI tool that converts handwritten or printed question paper images into a clean DOCX file using Gemini Vision. The system has three parts: a Next.js frontend for uploading images and downloading the final file, a .NET backend that formats the extracted content into DOCX, and a Python FastAPI service that sends images to Gemini 2.0 Flash for OCR + structure extraction.

### Setup
- **AI Service:** `cd ai-service/questionbuilderai_ocr_llm` → create venv → install requirements → add `.env` with `GEMINI_API_KEY` → run `uvicorn app.main:app --reload --port 8000`
- **Backend:** `cd backend/QuestionBuilderAI.Api` → `dotnet run`
- **Frontend:** `cd frontend/questionbuilderai-ui` → `npm install` → `npm run dev`

Frontend runs on `localhost:3000`, backend on `localhost:5196`, AI service on `localhost:8000`.

### How it Works
User uploads one or more question-paper images → .NET sends them to Python → Python sends them to Gemini Vision → Gemini returns structured JSON (sections, questions, marks, languages) → .NET generates a clean DOCX → frontend downloads it.

### Output JSON Example
```json
{"schoolName":"","examTitle":"","class":"","subject":"","maxMarks":80,"duration":"3 Hours","sections":[{"name":"Section A","instructions":"","questions":[{"number":1,"text":"Sample question","marks":2,"language":"English"}]}]}
