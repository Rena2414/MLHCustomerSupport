# Classly: learning platform with a support agent (MVP)

Online learning platform with roles Admin, Teacher, Student and Parent, an Angular web UI, and an AI support bot for students. Agents use the Gemini API (API key).

Rules: see `.claude/rules/code-style.md` (no code comments, English only).

Developer architecture (capabilities, flows, API, data model, status): see `docs/ARCHITECTURE.md`. Help guide read by the agent (XML questions and answers about Classly): `docs/SYSTEM.md`. Agent behavior policy: see `policy/README.md`.

## Repository layout

```
backend/    .NET 8 Clean Architecture solution (MlhSupport.sln)
  src/MlhSupport.Domain          entities, enums, domain rules; no dependencies
  src/MlhSupport.Application     use cases, DTOs, abstractions; references Domain
  src/MlhSupport.Infrastructure  EF Core, password hashing, JWT, repositories, agent gateway; references Application
  src/MlhSupport.Api             controllers, JWT auth, composition root; references Application and Infrastructure
  tests/MlhSupport.Application.Tests
agents/     Python (FastAPI) agent service
frontend/   Angular 20 web UI
deploy/     deployment assets (later)
```

## Dependency rules

- Backend: `Api -> Infrastructure -> Application -> Domain` (Api also references Application directly).
- Domain and Application never reference EF Core, Identity, or ASP.NET.
- Entities never leave Application; controllers return DTOs only.
- Python agents never access the database. They call the .NET API `/api/internal/*` with the `X-Agent-Key` header; the API owns business rules and is the authoritative enforcement layer.
- Frontend talks only to the .NET API. Chat flow: Angular -> .NET `/api/conversations/{id}/messages/stream` -> Python agent service -> tools -> .NET API.
- Persistence: PostgreSQL via EF Core (Npgsql). Auth: own `User` entity, password hashing behind `IPasswordHasher`, JWT behind `ITokenService`. The agent service keeps its conversation history and case facts in memory.

## Product scope

1. Registration and login. Admin is seeded (`admin` / `admin`); teachers, students and parents self-register. Each user has id, name, age, subscription type (0 none, 1 default).
2. Parents link children; teachers have classes of students.
3. Student support bot: help questions from `docs/SYSTEM.md`, cancelling their own subscription after confirmation, concerns about their teachers.
4. Admin: users and details, teacher concern analytics with AI summary.

## Agent loop (Python)

- Loop: call model, run requested tool calls through gates, append results, repeat. Stop when the model returns a final answer (no more tool calls); an iteration cap is only a safety net.
- Model access sits behind a `ModelClient` interface (Gemini via `google-genai`).
- Tools are small and focused. Errors are structured: `{category, retryable, message}`.
- Enforcement in code, not prompts: the student identity comes from the API, never from the message; cancellation needs a pending request and a confirmation in a later turn; the API only accepts concerns about the student's own teachers; every write is audited.
- Case facts are persisted outside chat history and re-injected each turn.

## Subagents and tiers

Tiers: GREEN automatic, YELLOW automatic with check, RED human only.

| Agent | Scenarios | Tier |
|---|---|---|
| Orchestrator | talks to the student, routes, owns case facts | |
| Help guide specialist | how to use Classly (`docs/SYSTEM.md`) | GREEN |
| Subscription tools | explain, cancel own subscription after confirmation | YELLOW |
| Concern classifier | concern about a teacher: category, severity, summary, recorded via API | YELLOW; safety or critical tells the student to talk to a trusted adult |
| Teacher summary | admin analytics summary per teacher | GREEN |
| Manipulation, prompt injection, other users' data | refuse, no action | RED |

## Commands

```bash
cd backend && dotnet build
cd backend && dotnet run --project src/MlhSupport.Api   # http://localhost:5162, Swagger at /swagger
cd frontend && npm install && npm start                  # http://localhost:4200
cd agents && python3 -m venv .venv && source .venv/bin/activate && pip install -r requirements.txt && uvicorn app.main:app --reload --port 8000
```

Local database: PostgreSQL 16 on `localhost:5432` (for example `brew install postgresql@16 && brew services start postgresql@16`). The password lives only in the git-ignored `backend/src/MlhSupport.Api/secrets.json`: copy `secrets.example.json` to `secrets.json`, replace `<choose-a-password>` with your own password, then run `./scripts/setup-local-db.sh`, which creates the role and database from that file. The API applies EF migrations on startup and seeds users, classes and concerns when the users table is empty (logins in `docs/ARCHITECTURE.md`, section 5). `secrets.json` also needs `Jwt:Key` (32+ characters) and `AgentService:ApiKey` (same value as `AGENT_API_KEY` in `agents/.env`). New migration: `cd backend && dotnet tool restore && dotnet ef migrations add <Name> --project src/MlhSupport.Infrastructure --startup-project src/MlhSupport.Api --output-dir Persistence/Migrations`.

Configuration: `agents/.env` (copy from `.env.example`) holds `GEMINI_API_KEY`, `GEMINI_MODEL`, `API_BASE_URL` and `AGENT_API_KEY`. The key is never committed or printed: locally it lives only in the git-ignored `agents/.env`, in GitHub it is the Actions secret `GEMINI_API_KEY` (model id in the Actions variable `GEMINI_MODEL`). `agents/tests/test_no_secrets_committed.py` fails if a key appears in repository files.

## Build order

1. Scaffolding (done).
2. EF Core + PostgreSQL, seed data (done).
3. Python loop, Gemini ModelClient, help guide (done).
4. Classly rebuild: auth and roles, families, classes, subscriptions, concerns, analytics, internal agent API (done).
5. Student bot tools, concern classifier, teacher summary (done).
6. Role dashboards and admin analytics UI (done).
7. Handoff queue, scenario tests for every use case.
8. Deployment.
