# QuestionBuilderAI

QuestionBuilderAI is an AI-powered tool that helps teachers convert handwritten question paper photos into clean, printable, formatted DOCX exam papers. It supports English and experimental Hindi (KrutiDev) formats and was built to reduce the manual typing workload for school teachers.

## ✨ Features

- Upload handwritten or printed question paper photos (supports multiple pages)
- AI-powered OCR + LLM extraction using Google Gemini Vision
- Auto-detection of question numbering and ordering
- Automatic DOCX generation using .NET + OpenXML SDK
- English exam format ready
- Hindi KrutiDev exam format (experimental)
- Teacher-friendly UI built with Next.js + TailwindCSS
- Cloud-ready multi-service architecture (Frontend + API + OCR AI Service)

## 🧱 Project Structure

QuestionBuilderAI/
  Dockerfile
  backend/QuestionBuilderAI.Api/        → ASP.NET Core API for DOCX generation
  frontend/                             → Next.js frontend
  ai-service/questionbuilderai_ocr_llm/ → FastAPI + Gemini Vision OCR/LLM service

## 🛠 Tech Stack

Frontend:
- Next.js, React, TailwindCSS

Backend API:
- ASP.NET Core (.NET 8+)
- OpenXML SDK
- Custom exam templates for English & Hindi

AI OCR Service:
- Python 3, FastAPI
- Google Gemini Vision API (OCR + LLM extraction)
- Uvicorn, Pydantic

Deployment:
- Render (Python OCR + .NET API)
- Vercel (Next.js frontend)

## 🚀 Getting Started (Local Development)

### 1. Clone the repository
git clone https://github.com/ShishirPathak/QuestionBuilderAI.git
cd QuestionBuilderAI

### 2. Setup the AI OCR Service (Python)
cd ai-service/questionbuilderai_ocr_llm
python -m venv venv
source venv/bin/activate  # macOS/Linux
# venv\Scripts\activate   # Windows
pip install -r requirements.txt

Create a .env file:
GEMINI_API_KEY=your_gemini_api_key_here

Run the service:
uvicorn app.main:app --reload --port 8001

### 3. Setup the .NET Backend API
cd backend/QuestionBuilderAI.Api
dotnet restore
dotnet run

Set OCR service URL:
export OCR_BASE_URL=http://localhost:8001

### 4. Setup the Next.js Frontend
cd frontend
npm install

Create .env.local:
NEXT_PUBLIC_API_BASE_URL=http://localhost:5196

Run frontend:
npm run dev

Open the app:
http://localhost:3000

## 🔁 Workflow Overview

1. Teacher uploads images in the frontend.
2. Frontend sends images + exam details to .NET API.
3. .NET API forwards images to Python FastAPI OCR/LLM service.
4. Gemini Vision extracts structured questions and returns JSON.
5. .NET API generates a clean, formatted DOCX file.
6. User downloads the final question paper.

## 🇮🇳 Hindi KrutiDev Support (Experimental)

- Separate API endpoint for Hindi
- Generates DOCX using KrutiDev font
- Supports Hindi question formatting
- Future improvement: Unicode ↔ KrutiDev text conversion for perfect accuracy

## 🔮 Future Enhancements

- Question editing UI before DOCX generation
- Answer key generation using AI
- Better Hindi support and font mapping
- Multiple templates per Class/Subject
- Save and reuse exam templates
- Mobile-friendly uploads

## 🤝 Contributing

This project was built to help teachers save time. Community contributions are welcome, especially around:
- Improving OCR accuracy for Indian languages
- KrutiDev/Devanagari conversions
- New exam templates
- UI/UX improvements

## 📬 Contact

Created by **Shishir Kumar Pathak**  
LinkedIn: *https://www.linkedin.com/in/shishirkrpathak/*

