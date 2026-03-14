# QuestionBuilderAI

QuestionBuilderAI is an **AI-powered backend platform** that converts handwritten question paper photos into clean, structured, printable DOCX exam papers.

The system combines **OCR, LLM extraction, and distributed job processing** to automate exam paper creation and eliminate manual typing for teachers.

The architecture uses **ASP.NET Core APIs, Azure Service Bus, worker services, and FastAPI-based AI processing** to handle long-running document extraction tasks asynchronously.

---

# ✨ Key Features

- Upload handwritten or printed question paper images (multi-page supported)
- AI-powered **OCR + LLM question extraction**
- Automatic **question numbering and structuring**
- Clean **DOCX exam paper generation**
- English exam formatting
- Experimental **Hindi (KrutiDev) formatting**
- **Asynchronous job processing** for large scans
- Teacher-friendly UI built with **Next.js + TailwindCSS**

---

# 🧱 System Architecture

The platform uses an **asynchronous distributed processing pipeline** to handle long-running OCR and AI tasks.

```
Frontend (Next.js)
        |
        v
ASP.NET Core API
        |
        v
Azure Service Bus Queue
        |
        v
Worker Service
        |
        v
FastAPI OCR + LLM Service
        |
        v
MongoDB
```

---

# 🔁 Workflow Overview

1. Teacher uploads exam paper images through the frontend.
2. ASP.NET Core API receives the request and creates a processing job.
3. The job is published to **Azure Service Bus**.
4. A **Worker Service** consumes the message and processes the task.
5. Images are sent to the **FastAPI OCR + LLM service**.
6. Gemini Vision extracts structured questions and returns JSON.
7. The backend generates a formatted **DOCX exam paper**.
8. The user downloads the final exam paper.

This architecture ensures:

- Non-blocking uploads
- Scalable background processing
- Fault-tolerant job handling

---

# 🛠 Tech Stack

## Frontend
- Next.js
- React
- TailwindCSS

## Backend API
- ASP.NET Core (.NET 8)
- OpenXML SDK
- Azure Service Bus
- MongoDB

## AI Processing Service
- Python
- FastAPI
- Google Gemini Vision API
- Pydantic
- Uvicorn

## Distributed Processing
- Azure Service Bus
- Worker Services
- Asynchronous job pipelines

## Deployment
- Vercel (Frontend)
- Render / Cloud hosting (API + AI services)

---

# ⚙️ Reliability Design

The system includes several reliability mechanisms for safe distributed processing:

- **Idempotent job processing using SHA-256 job hashing**
- **Atomic job claiming** to prevent duplicate work
- **Azure Service Bus lock/complete semantics**
- **At-least-once delivery safe processing**

These safeguards ensure correct results even when workers retry tasks or fail mid-processing.

---

# 🚀 Local Development

## 1 Clone repository

```bash
git clone https://github.com/ShishirPathak/QuestionBuilderAI.git
cd QuestionBuilderAI
```

---

## 2 Setup AI OCR Service

```bash
cd ai-service/questionbuilderai_ocr_llm
python -m venv venv
source venv/bin/activate   # macOS/Linux
# venv\Scripts\activate    # Windows

pip install -r requirements.txt
```

Create `.env`

```
GEMINI_API_KEY=your_gemini_api_key_here
```

Run service

```bash
uvicorn app.main:app --reload --port 8001
```

---

## 3 Setup .NET Backend API

```bash
cd backend/QuestionBuilderAI.Api
dotnet restore
dotnet run
```

Set OCR service URL

```
OCR_BASE_URL=http://localhost:8001
```

---

## 4 Setup Frontend

```bash
cd frontend
npm install
npm run dev
```

Open the app:

```
http://localhost:3000
```

---

# 🇮🇳 Hindi KrutiDev Support (Experimental)

- Separate API endpoint for Hindi papers
- Generates DOCX using **KrutiDev fonts**
- Supports Hindi question formatting

Future improvements:

- Unicode ↔ KrutiDev conversion
- Better OCR accuracy for Indian scripts

---

# 🔮 Future Enhancements

- Question editing before DOCX generation
- AI-generated answer key creation
- Multiple exam templates per subject
- Save and reuse teacher templates
- Mobile-friendly uploads
- Improved multilingual OCR

---

# 🤝 Contributing

This project was built to help teachers save time.

Contributions are welcome, especially around:

- Improving OCR accuracy for Indian languages
- KrutiDev / Devanagari conversions
- New exam templates
- UI/UX improvements

---

# 📬 Author

**Shishir Kumar Pathak**

LinkedIn  
https://www.linkedin.com/in/shishirkrpathak/

GitHub  
https://github.com/ShishirPathak
