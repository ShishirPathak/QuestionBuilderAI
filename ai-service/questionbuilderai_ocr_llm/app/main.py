import os
import json
from typing import List, Dict, Any, Optional

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from dotenv import load_dotenv
from google import genai
from google.genai import types
import re   # ← ADD THIS

# Load .env (GEMINI_API_KEY must be set there)
load_dotenv()

app = FastAPI(
    title="QuestionBuilderAI OCR + LLM Service",
    description="Extracts questions from question paper images and returns structured JSON.",
    version="0.3.0",
)

# CORS (for local dev – you can restrict later)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configure Gemini client – it reads GEMINI_API_KEY from environment
GEMINI_API_KEY = os.getenv("GEMINI_API_KEY")
if not GEMINI_API_KEY:
    raise RuntimeError("GEMINI_API_KEY is not set in .env")

client = genai.Client(api_key=GEMINI_API_KEY)

# Use a supported multimodal model
VISION_MODEL_NAME = "gemini-2.0-flash"


@app.post("/ocr/parse-question-paper")
async def parse_question_paper(
    files: List[UploadFile] = File(...),              # required
    schoolName: Optional[str] = Form(None),
    examTitle: Optional[str] = Form(None),
    className: Optional[str] = Form(None),
    subject: Optional[str] = Form(None),
    maxMarks: Optional[int] = Form(None),
    duration: Optional[str] = Form(None),
) -> Dict[str, Any]:
    """
    Takes uploaded question paper images (handwritten/printed),
    calls Gemini Vision to extract questions, and returns ExamPaperModel-shaped JSON.
    """

    if not files:
        raise HTTPException(status_code=400, detail="No files provided.")

    # Prepare image parts for Gemini (inline bytes)
    image_parts: List[types.Part] = []
    for f in files:
        content = await f.read()
        if not content:
            continue

        mime_type = f.content_type or "image/jpeg"
        image_parts.append(
            types.Part.from_bytes(
                data=content,
                mime_type=mime_type,
            )
        )

    if not image_parts:
        raise HTTPException(
            status_code=400,
            detail="Could not read any image content from uploaded files.",
        )

    # Prompt for Gemini Vision
    prompt = f"""
You are helping a school teacher digitize their exam question papers.

You are given one or more images of a handwritten or printed question paper.
The questions are for school students and may be in Hindi, English, or Sanskrit.

Your job:
- Read all the questions and instructions.
- Group questions into logical blocks that match how teachers write "Q.No.1", "Q.No.2", etc.
- For each block, extract:
  - qno (question number, like 1, 2, 3...)
  - a short title/heading (e.g., "Answer in one word", "Answer the following questions", "Fill in the blanks")
  - total marks for that Q.No group if visible
  - the sub-questions inside that group (a, b, c, ...)

Return data in this EXACT JSON shape (no extra keys, no comments, no explanation):

{{
  "schoolName": "string",
  "examTitle": "string",
  "class": "string",
  "subject": "string",
  "maxMarks": number,
  "duration": "string",
  "sections": [
    {{
      "name": "string",               // e.g. "Q.No.1 Answer in one word."
      "instructions": "string",       // any hint line, or "" if none
      "questions": [
        {{
          "number": number,           // 1,2,3,... (will later become (a),(b),(c))
          "text": "string",           // full question text
          "marks": number,            // total marks for this GROUP (same for all in the group)
          "language": "Hindi" | "English" | "Sanskrit" | "Mixed"
        }}
      ]
    }}
  ]
}}

Important formatting rules:
- Each major question group like "Q.No.1", "Q.No.2" MUST become one element in "sections".
- The "name" property should start with "Q.No." followed by the number and the title.
  Example: "Q.No.1 Answer in one word."
- If the total marks for a group are given (e.g. (10)), set "marks" for EACH question in that group to that total value.
  (The backend will only read the marks from the first question.)
- "number" for questions should be 1,2,3,... in the order they appear within that group.

Use these defaults if the information is missing on the paper:
- schoolName: "{schoolName}"
- examTitle: "{examTitle}"
- class: "{className}"
- subject: "{subject}"
- maxMarks: {maxMarks}
- duration: "{duration}"

Very important:
- Respond with VALID JSON ONLY. No ``` fences. No natural language explanation.
"""


    # Call Gemini Vision
    try:
        response = client.models.generate_content(
            model=VISION_MODEL_NAME,
            contents=[prompt] + image_parts,
        )
    except Exception as e:
        # This is what caused your previous 500 with 404 model error
        raise HTTPException(
            status_code=500,
            detail=f"Error calling Gemini Vision: {e}",
        )

    raw_text = (response.text or "").strip()

    # Sometimes models wrap JSON in ```json ... ```
    if raw_text.startswith("```"):
        raw_text = raw_text.strip("`")
        if raw_text.lower().startswith("json"):
            raw_text = raw_text[4:].strip()

    # DEBUG (optional): print first part of Gemini output
    # print("GEMINI RAW RESPONSE START ======")
    # print(raw_text[:500])
    # print("GEMINI RAW RESPONSE END   ======")

    try:
        data = json.loads(raw_text)
    except json.JSONDecodeError as e:
        raise HTTPException(
            status_code=500,
            detail=f"Failed to parse JSON from Gemini response: {e}; raw_text={raw_text[:300]}",
        )

    if "sections" not in data or not data.get("sections"):
        raise HTTPException(
            status_code=500,
            detail="Gemini returned no sections/questions.",
        )

    # --- NEW: sort sections by Q.No. extracted from section["name"] ---

    def extract_qno(section: Dict[str, Any]) -> int:
        name = section.get("name", "") or ""
        # Try to find patterns like Q.No.1, Q.1, Q1 etc.
        m = re.search(r"q\.?\s*no\.?\s*(\d+)", name, flags=re.IGNORECASE)
        if not m:
            m = re.search(r"q\.?\s*(\d+)", name, flags=re.IGNORECASE)
        if m:
            return int(m.group(1))
        # fallback if we can't parse → push to end
        return 9999

    sections = data.get("sections", [])

    # Sort sections by Q.No.
    sections.sort(key=extract_qno)

    # Also sort questions inside each section by their "number"
    for s in sections:
        qs = s.get("questions", [])
    try:
        qs.sort(key=lambda q: int(q.get("number", 0)))
    except Exception:
        pass
    
    s["questions"] = qs

    # Ensure metadata is filled even if Gemini omits it
    data.setdefault("schoolName", schoolName)
    data.setdefault("examTitle", examTitle)
    data.setdefault("class", className)
    data.setdefault("subject", subject)
    data.setdefault("maxMarks", maxMarks)
    data.setdefault("duration", duration)

    return data


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=int(os.getenv("PORT", 8001))  # 8001 for local, $PORT on Render
    )