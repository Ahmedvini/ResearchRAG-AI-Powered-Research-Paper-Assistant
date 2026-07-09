# ResearchRAG

ResearchRAG is a production-oriented MVP for an academic research assistant. It supports authenticated workspaces, PDF upload, document processing, hybrid retrieval, citation-aware chat, dashboard metrics, admin operations, Docker deployment, and prepared extension points for advanced research workflows.

## Stack

- Frontend: React, TypeScript, Tailwind CSS, React Query, React Router, Recharts
- Backend: ASP.NET Core 9, layered application services, EF Core, JWT
- Worker: Python, PyMuPDF, SQLAlchemy, Qdrant client
- Data: MySQL and Qdrant

## Quick Start

```bash
cp .env.example .env
docker compose up --build
```

Services:

- Frontend: http://localhost:5173
- Backend Swagger: http://localhost:8080/swagger
- Qdrant: http://localhost:6333/dashboard

## Local Development

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Worker:

```bash
cd worker
python3 -m venv .venv
. .venv/bin/activate
pip install -e ".[runtime,dev]"
pytest
```

Backend requires .NET 9 SDK:

```bash
cd backend
dotnet restore
dotnet test
dotnet run --project ResearchRag.Api
```

## Seed Users

The API seeds:

- Admin: `admin@researchrag.local` / `Admin123!`
- User: `user@researchrag.local` / `User123!`

Change seeded credentials before production use.

## Database Schema

The backend applies EF Core migrations on startup (`Persistence/Migrations`). Databases created by older builds via `EnsureCreated` are not migration-compatible; drop the `mysql-data` volume once when upgrading. After changing entities, add a migration from `backend/`:

```bash
dotnet ef migrations add <Name> --project ResearchRag.Infrastructure --startup-project ResearchRag.Api --output-dir Persistence/Migrations
```

## Email

Verification and password-reset emails are sent through SMTP when `SMTP_HOST` is configured. Without it, the backend logs the full email (including the link) instead — check the backend log during development. Links point at `Email:FrontendBaseUrl` (`FRONTEND_ORIGIN` in compose), which serves the `/verify-email` and `/reset-password` pages.

## MVP Boundaries

The MVP implements the core RAG loop and keeps advanced features behind explicit interfaces. Literature reviews, paper comparison, research gap analysis, quizzes, flashcards, and knowledge graphs can be added without changing the workspace, document, retrieval, and chat foundations.

## AI Providers

The app runs locally without external credentials using `echo`, `hash`, and `passthrough` providers. For real model-backed behavior, configure:

- `CHAT_PROVIDER=openai` with `OPENAI_API_KEY`, or `CHAT_PROVIDER=ollama` with a local Ollama server.
- `EMBEDDING_PROVIDER=openai` or `EMBEDDING_PROVIDER=ollama`.
- `RERANKER_PROVIDER=cohere` with `COHERE_API_KEY`.

The backend (query embedding) and worker (document embedding) must use the same embedding provider and model, or vectors will not match. Both follow `EMBEDDING_PROVIDER`; the model comes from `OPENAI_EMBEDDING_MODEL` or `OLLAMA_EMBEDDING_MODEL` unless `EMBEDDING_MODEL` is set as an explicit override for the worker. Changing the provider or model requires re-uploading documents (the Qdrant collection dimension is fixed at creation).

When running via Docker, `OLLAMA_BASE_URL` should point at `http://host.docker.internal:11434` (mapped to the host via `extra_hosts`); use `http://localhost:11434` only for processes running directly on the host.

Advanced research tools are available in the app under Research Tools and expose literature review, paper comparison, research gap analysis, knowledge graph, flashcard, and quiz generation endpoints.
