# Tutoring Platform: Implementation Plan

Last updated: 2026-10-31
Audience: the team and coding agents
Behavior spec: `docs/TUTORING_AGENT_POLICY.md`
Current system: `docs/ARCHITECTURE.md` (Leaply help-guide assistant on `main`, commit `d2b6312`)

This document explains what we are changing, why, and exactly where in the current code each change goes.

---

## 1. What Changes

The product changes from Leaply (a wellness app with one adult customer type) to an online tutoring platform with several audiences:

| Before (Leaply) | After (tutoring platform) |
|---|---|
| One audience: adult customer | Students of different ages, parents, grandparents, teachers, curators, client managers |
| Help-guide Q&A: the orchestrator answers how-to questions through `ask_help_guide` | Support operations: the agent investigates complaints, writes to curators, hands cancellations to client managers, explains salaries |
| One tone | Tone profiles per audience, learned over time |
| No analytics | Complaint and salary analytics, decision-quality dashboards |
| No feedback loop | A reviewer sub-agent scores every reply and learns which decision works best |

---

## 2. Team Priorities

These are the capabilities the team committed to. Each one maps to rules in the policy and to concrete code below.

| ID | Capability | Policy | Main code changes |
|---|---|---|---|
| PRIO-01 | Many students say a teacher does not suit them, so the curator gets a request to talk with the teacher | CHILD-20 to CHILD-23 | `ComplaintInvestigatorAgent`, `get_complaints`, `create_curator_request`, `CuratorRequest` entity |
| PRIO-02 | One student repeats the request: (a) a plan to improve test results, (b) a letter to the curator, based on how the student talks, about the kind of teacher they need | CHILD-10 to CHILD-13 | `LearningAdvisorAgent`, `CuratorLetterAgent`, `get_test_results`, `send_curator_letter`, `CuratorLetter` entity |
| PRIO-03 | A parent wants to cancel: summarize the reasons, offer the client manager, hand over the summary | CANCEL-01 to CANCEL-07 | `connect_client_manager`, `ClientManagerHandoff` entity, client manager queue screen |
| PRIO-04 | Complaint validity check | CHILD-05 to CHILD-09 | `ComplaintInvestigatorAgent` with lesson, test and complaint tools |
| PRIO-05 | Tone of voice by audience, learned per category, new categories such as grandparents | Section 5 | `ToneProfilerAgent`, `AudienceProfile` entity, profile examples in prompts |
| PRIO-06 | Analytics: complaint counts by period, salary review | Section 12 | Analytics endpoints and dashboards |
| PRIO-07 | Reviewer sub-agent learns which decision resolves problems best | Section 13 | `ReplyReviewerAgent`, `ReviewRecord` entity, `get_decision_ranking` |
| PRIO-08 | Teacher salary and workload questions | Section 11 | `get_teacher_salary`, salary and workload tickets |

Decisions recorded in the policy that the team should know about:

1. "Tutor" in the team notes means the student's curator, not the teacher. The policy uses `curator` and `teacher` as two roles.
2. Safety comes before the validity check: if a child describes humiliation, threats, requests for photos or secrets, the agent escalates immediately without checking the child's history (HARD-04, section 10).
3. The agent never cancels and never makes retention offers itself; the client manager does. If the parent refuses the connection, the agent still gives the self-service cancellation steps so cancellation is never blocked (CANCEL-05).
4. Learning changes rankings, examples and tone wording only. Rules, limits and safeguarding change only with a support lead's approval (HARD-09, LEARN-06).

---

## 3. Target Architecture

The structure stays the same as on `main`: Angular → .NET API → Python agent service → tools → .NET API, with PostgreSQL behind the API. The orchestrator keeps calling sub-agents as tools, the same pattern `OrchestratorAgent` already uses for `ask_help_guide`.

```mermaid
flowchart TD
    U[Student / Parent / Teacher<br/>chat UI] --> API[.NET API]
    S[Curator / Client manager / Support lead<br/>staff UI] --> API
    API -->|POST /chat with role and user ids| O[OrchestratorAgent]
    O --> TP[ToneProfilerAgent]
    O --> HG[DocsReaderAgent<br/>ask_help_guide]
    O --> CI[ComplaintInvestigatorAgent]
    O --> LA[LearningAdvisorAgent]
    O --> CL[CuratorLetterAgent]
    O -->|data and write tools| T[/api/tools/*]
    T --> API
    O -->|after each turn| RR[ReplyReviewerAgent]
    RR -->|record_review| T
    AN[AnalyticsAgent] -->|build_dashboard| T
    API --> DB[(PostgreSQL)]
```

---

## 4. Changes by Component

### 4.1 Agent service (`agents/`)

| File | Change |
|---|---|
| `app/prompts/orchestrator.md` | Rewrite for the tutoring platform: roles, priorities, tone profiles, when to call each sub-agent, hard rules. Source: policy sections 2 to 10 |
| `app/prompts/complaint_investigator.md` | New. Checks lessons, tests, teacher metrics and similar complaints; returns the structured finding from policy 8.2 |
| `app/prompts/learning_advisor.md` | New. Reads test results and homework, returns 2 to 3 concrete steps (CHILD-10) |
| `app/prompts/curator_letter.md` | New. Infers the student's need from their wording and writes the letter in the format from policy 8.4 |
| `app/prompts/tone_profiler.md` | New. Returns one profile from policy 5.1 and the approved phrasing examples for it |
| `app/prompts/reply_reviewer.md` | New. Scores a turn against LEARN-01 and writes the decision record |
| `app/prompts/analytics.md` | New. Builds the dashboards from policy section 12 |
| `app/agents/complaint_investigator.py`, `learning_advisor.py`, `curator_letter.py`, `tone_profiler.py`, `reply_reviewer.py`, `analytics.py` | New sub-agents, same shape as `DocsReaderAgent`: own prompt, own `ToolRegistry`, `run_agent` |
| `app/agents/orchestrator.py` | Register the sub-agent tools next to `ask_help_guide`; build the tool set from the session role; run the reviewer after each turn |
| `app/tools/backend_api.py` | New. `httpx` client for `/api/tools/*` that maps HTTP and API errors to `ToolError` categories |
| `app/tools/*.py` | New data and write tools from policy section 14, each a `Tool` with a clear description of when to use it and when not to |
| `app/api/chat.py` | `ChatRequest` gets `role`, `user_id`, `student_id`; `ChatResponse` gets `handoff` and `actions` |
| `app/core/conversation_store.py` | Store case facts (audience profile, verified flag, open issues, finding, pending confirmation) next to the history |
| `docs/SYSTEM.md` | Replace the Leaply help guide with the tutoring platform help guide in the same XML Q&A format, because `DocsReaderAgent` reads it |

### 4.2 Backend (`backend/`)

New domain entities in `MlhSupport.Domain`, mapped in `AppDbContext` with configurations and one EF migration (`dotnet ef migrations add TutoringPlatform`):

| Entity | Key fields |
|---|---|
| `Family` | Id, parent name, email, preferred language, curator updates enabled |
| `Student` | Id, family, first name, birth year (age band), level, curator |
| `Teacher` | Id, name, subjects, availability, max weekly load, pay rate |
| `Curator` | Id, name, students |
| `Lesson` | Id, student, teacher, start, duration, attendance, teacher notes, recording |
| `TestResult` | Id, student, subject, topic, score, date |
| `Complaint` | Id, student, teacher, category, summary, finding, created |
| `CuratorRequest` | Id, teacher, evidence (anonymized), status, created |
| `CuratorLetter` | Id, student, teacher, inferred need, body, status, sent |
| `ClientManagerHandoff` | Id, family, reasons, facts, recommendation, status |
| `SalaryLine` | Teacher, period, lessons delivered, rate, deductions, payout date |
| `SalaryTicket`, `WorkloadTicket` | Teacher, lessons, status |
| `AudienceProfile` | Name, tone rules, approved examples, status (active or proposed) |
| `ReviewRecord` | Conversation, issue type, profile, decision, scores, outcome |
| `SafeguardingFlag`, `AuditEntry` | As in the policy |

API endpoints:

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/tools/{tool}` | One entry point for agent tools. Enforces the role gate (students cannot run family-account actions), confirmation for writes, safeguarding protective steps, and audit |
| GET | `/api/analytics/complaints?from&to&groupBy=day\|week\|month&teacherId&category` | DASH-01, DASH-02 |
| GET | `/api/analytics/salary?period&teacherId` | DASH-03 |
| GET | `/api/analytics/decisions?issueType&from&to` | DASH-06 |
| GET | `/api/curator-requests`, `/api/curator-letters` | Curator inbox |
| GET | `/api/client-manager/handoffs` | Client manager queue |
| GET | `/api/audience-profiles`, POST `/api/audience-profiles/{id}/approve` | TONE-22 approvals |

`Conversation` gets `Role`, `UserId` and `StudentId` so the API can tell the agent who is talking.

Seed data in `SeedData.cs`: 3 families (one with a grandparent writer), 6 students of different ages, 4 teachers (one above the complaint threshold), 2 curators, October lessons, test results with clear weak topics, complaints, and salary lines with one missing lesson.

### 4.3 Frontend (`frontend/`)

| Screen | Purpose |
|---|---|
| Chat with a demo role switcher | Talk as a student, parent, grandparent or teacher from the seed data |
| Curator inbox | Curator requests and letters |
| Client manager queue | Cancellation handoffs with reasons and facts |
| Complaints dashboard | Counts by period, category and teacher; teacher watchlist |
| Salary review | Salary per teacher and period, open tickets |
| Decision quality | Reviewer scores and best decisions per issue type; proposed audience profiles |

### 4.4 Tests

| Level | What |
|---|---|
| Agent scenario tests (`agents/tests`, with the existing `ScriptedModel` fakes) | E01 to E06 from the policy: expected sub-agent calls, tools, letters, requests and handoffs |
| Agent unit tests | Role-based tool sets, threshold logic, tone profile selection, reviewer record format |
| Backend tests | Role gate, confirmation, complaint threshold queries, analytics grouping, salary calculation |
| Safety test | A safeguarding message never triggers `investigate_complaint` first |

---

## 5. Build Order

| Step | Work | Result |
|---|---|---|
| 1 | Domain entities, migration, seed data | Data for every scenario |
| 2 | `/api/tools/*` read tools and `backend_api.py` | The agent can look up lessons, tests, complaints, salary |
| 3 | Orchestrator prompt and tone profiler | Correct tone for each audience |
| 4 | Complaint investigator, `record_complaint`, `create_curator_request` | PRIO-01, PRIO-04 |
| 5 | Learning advisor and curator letter writer | PRIO-02 |
| 6 | `connect_client_manager` and the client manager queue | PRIO-03 |
| 7 | Complaints and salary analytics endpoints and dashboards | PRIO-06 |
| 8 | Reply reviewer, review records, decision ranking, profile proposals | PRIO-07, PRIO-05 learning |
| 9 | Salary and workload tickets | PRIO-08 |
| 10 | Replace the help guide in `docs/SYSTEM.md`, update `docs/ARCHITECTURE.md` and `CLAUDE.md` | Documentation matches the product |

For a demo with little time, steps 1 to 6 with one dashboard (step 7, complaints only) cover the main story: a child's complaint, a curator letter, a teacher pattern, and a parent cancellation handed to a client manager.

---

## 6. What Can Be Reused

| Existing piece | Where | Reuse |
|---|---|---|
| Agent loop, `ToolRegistry`, `ToolError` | `agents/app/core/` on `main` | As is |
| Sub-agent-as-tool pattern | `OrchestratorAgent` with `DocsReaderAgent` | Template for every new sub-agent |
| Streaming chat | `/chat/stream`, Angular SSE parser | As is |
| PostgreSQL with EF migrations | `backend/` on `main` | Add entities and a migration |
| Code-enforced gates (verification, confirmation from the latest user message, Red-tier block, idempotency, audit) and case facts | Branch `ksenia/agent-policy-loop` | Port into `/api/tools/*` instead of rewriting; not merged |
| Policy format with stable rule IDs | `policy/` | Same format for the tutoring policy |

---

## 7. Open Questions for the Team

| Question | Current draft value |
|---|---|
| Platform name | Not set |
| Teacher pattern threshold | 3 distinct students in 30 days, or complaint rate 2x average |
| Repeated requests from one student | 2 or more in 30 days |
| Can a curator letter be sent without the parent's consent | Only if the family enabled curator updates |
| Curator and client manager response times | 3 working days and 1 working day |
| Who approves salary corrections | Staff, within 3 working days |
| Parent verification | One-time code |
| Do teachers chat with the same agent or a separate one | Same agent, `teacher_chat` mode |
| When the Leaply policy in `policy/` is retired | After step 10 |
