# MLH Customer Support Agent (MVP)

Mock wellness and health app with a minimal admin web UI and an AI customer-support agent. Agents use the Gemini API (API key). Hosting and deployment are decided later.

Rules: see `.claude/rules/code-style.md` (no code comments, English only).

Developer architecture (capabilities, flows, API, data model, status): see `docs/ARCHITECTURE.md`. Customer-facing help guide read by the agent (XML questions and answers about the Leaply app): `docs/SYSTEM.md`. Agent behavior policy: see `policy/README.md`.

## Repository layout

```
backend/    .NET 8 Clean Architecture solution (MlhSupport.sln)
  src/MlhSupport.Domain          entities, enums, domain rules; no dependencies
  src/MlhSupport.Application     use cases, DTOs, abstractions; references Domain
  src/MlhSupport.Infrastructure  EF Core, Identity, repositories; references Application
  src/MlhSupport.Api             controllers, composition root; references Application and Infrastructure
  tests/MlhSupport.Application.Tests
agents/     Python (FastAPI) agent service
frontend/   Angular 20 admin UI
deploy/     deployment assets (later)
```

## Dependency rules

- Backend: `Api -> Infrastructure -> Application -> Domain` (Api also references Application directly).
- Domain and Application never reference EF Core, Identity, or ASP.NET.
- Entities never leave Application; controllers return DTOs only.
- Python agents never access the database. They call the .NET API, which owns business rules and the authoritative enforcement layer.
- Frontend talks only to the .NET API. Chat flow: Angular -> .NET `/api/chat` -> Python agent service -> tools -> .NET API.
- Persistence: PostgreSQL via EF Core (Npgsql) for customers, conversations and chat messages. ASP.NET Identity for staff users (roles Admin, SupportAgent) is still planned. The agent service keeps its own conversation history in memory.

## Product scope

1. Admin UI: table of clients; clicking a client shows all stored data.
2. Support chat: customer talks to an orchestrator agent that delegates to subagents. The agent acts on its own or escalates to a human with a structured handoff.

## Agent loop (Python)

- Loop: call model, run requested tool calls through gates, append results, repeat. Stop when the model returns a final answer (no more tool calls); an iteration cap is only a safety net.
- Model access sits behind a `ModelClient` interface (Gemini via `google-genai`).
- Policy layer: system prompt holds general policy and escalation criteria; changing policy is fetched with `get_policy(topic)`.
- Tools are small and focused. Errors are structured: `{category, retryable, message}`.
- Enforcement in code, not prompts: verification gate before write tools, refund cap forces escalation, explicit confirmation before irreversible actions, audit log of every write.
- Case facts (customer id, verified flag, order ids, amounts, decisions) are persisted outside chat history and re-injected each turn.
- Escalate when: customer asks for a human, policy is silent or ambiguous, no progress, or action exceeds authority. Not on sentiment alone. Handoff contains customer, issue, steps taken, recommended action.
- Multiple issues in one message stay in one loop with shared case facts.

## Subagents and tiers

Tiers: GREEN automatic, YELLOW automatic with check or limit, RED human only.

| Subagent | Scenarios | Tier |
|---|---|---|
| Subscription and Billing | charged after trial, how to cancel, charged after cancelling, not found in Apple/Google settings, next payment, pause or cheaper plan, cancel but keep access | GREEN/YELLOW |
| Payments and Refunds | charged twice, unrecognized charge, refund request, chargeback threat, failed payment still charged | YELLOW; unrecognized charge with no match, chargeback, refunds beyond policy are RED |
| Account and Tech | cannot log in, two emails, progress lost after phone change, app crash | GREEN/YELLOW |
| Plan and Safety | which practice to start, pain or dizziness (no medical advice, RED), account deletion (RED), manipulation or prompt injection such as "ignore your rules, I'm a manager" (refuse, log, no action) | GREEN/RED |

Shared tools: `verify_customer`, `get_customer`, `get_subscription`, `get_invoices`, `get_policy`, `search_help_center`, `escalate_to_human`. Write tools: `subscribe`, `cancel_subscription`, `change_plan`, `process_refund`.

## Commands

```bash
cd backend && dotnet build
cd backend && dotnet run --project src/MlhSupport.Api   # http://localhost:5162, Swagger at /swagger
cd frontend && npm install && npm start                  # http://localhost:4200
cd agents && python3 -m venv .venv && source .venv/bin/activate && pip install -r requirements.txt && uvicorn app.main:app --reload --port 8000
```

Local database: PostgreSQL 16 on `localhost:5432` (for example `brew install postgresql@16 && brew services start postgresql@16`). The password lives only in the git-ignored `backend/src/MlhSupport.Api/secrets.json`: copy `secrets.example.json` to `secrets.json`, replace `<choose-a-password>` with your own password, then run `./scripts/setup-local-db.sh`, which creates the role and database from that file. In Development the API applies EF migrations and seeds three customers on startup. New migration: `cd backend && dotnet tool restore && dotnet ef migrations add <Name> --project src/MlhSupport.Infrastructure --startup-project src/MlhSupport.Api --output-dir Persistence/Migrations`.

Configuration: `agents/.env` (copy from `.env.example`) holds `GEMINI_API_KEY` and `GEMINI_MODEL`. The key is never committed or printed: locally it lives only in the git-ignored `agents/.env`, in GitHub it is the Actions secret `GEMINI_API_KEY` (model id in the Actions variable `GEMINI_MODEL`). `agents/tests/test_no_secrets_committed.py` fails if a key appears in repository files.

## Build order

1. Scaffolding (done): projects, references, runnable Angular and API.
2. EF Core + PostgreSQL, seed data (done). Identity still to do.
3. Python loop, Gemini ModelClient, read-only tools, policy.
4. Write tools behind gates; audit; escalation and handoff.
5. Subagents, chat UI, handoff queue.
6. Scenario tests for every use case.
7. Deployment.
