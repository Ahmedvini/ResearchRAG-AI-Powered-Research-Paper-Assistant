# ResearchRAG

ResearchRAG is a production-oriented MVP for an academic research assistant. It supports authenticated workspaces, PDF upload, document processing, hybrid retrieval, citation-aware chat, dashboard metrics, admin operations, Docker deployment, and prepared extension points for advanced research workflows.

## Stack

- Frontend: React, TypeScript, Tailwind CSS, React Query, React Router, Recharts
- Backend: ASP.NET Core 9, Clean Architecture, CQRS-style application services, MediatR, EF Core, JWT
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

## MVP Boundaries

The MVP implements the core RAG loop and keeps advanced features behind explicit interfaces. Literature reviews, paper comparison, research gap analysis, quizzes, flashcards, and knowledge graphs can be added without changing the workspace, document, retrieval, and chat foundations.
