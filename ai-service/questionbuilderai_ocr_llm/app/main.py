import os
import json
from typing import List, Dict, Any

from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from dotenv import load_dotenv
from google import genai
from google.genai import types

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
    schoolName: str = Form(...),
    examTitle: str = Form(...),
    className: str = Form(...),
    subject: str = Form(...),
    maxMarks: int = Form(...),
    duration: str = Form(...),
    files: List[UploadFile] = File(...),
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
The questions can be in Hindi, English, Sanskrit, or a mix.

Your task:
1. Read all the questions and any visible sections/instructions.
2. Group questions into sections (Section A, B, etc.) if possible.
3. Infer marks for each question if they are clearly written, otherwise default to 2.
4. Try to detect the language of each question: "Hindi", "English", "Sanskrit" or "Mixed".
5. Return ONLY valid JSON that matches this schema (no comments, no extra text):

{{
  "schoolName": "string",
  "examTitle": "string",
  "class": "string",
  "subject": "string",
  "maxMarks": number,
  "duration": "string",
  "sections": [
    {{
      "name": "string",
      "instructions": "string",
      "questions": [
        {{
          "number": number,
          "text": "string",
          "marks": number,
          "language": "Hindi" | "English" | "Sanskrit" | "Mixed"
        }}
      ]
    }}
  ]
}}

Use these defaults if the information is not present on the paper:
- schoolName: "{schoolName}"
- examTitle: "{examTitle}"
- class: "{className}"
- subject: "{subject}"
- maxMarks: {maxMarks}
- duration: "{duration}"

Very important:
- Respond with JSON ONLY. No explanation text.
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

    # Ensure metadata is filled even if Gemini omits it
    data.setdefault("schoolName", schoolName)
    data.setdefault("examTitle", examTitle)
    data.setdefault("class", className)
    data.setdefault("subject", subject)
    data.setdefault("maxMarks", maxMarks)
    data.setdefault("duration", duration)

    return data
