# Classly Support Agent Policy

Version: 3.0.0
Last updated: 2026-10-31
Product: Classly online learning platform (web)
Audience of the agent: signed-in students, mostly children aged about 8 to 16

## Structure

| File | Scope |
|---|---|
| `00_core.md` | Role, audience, hard rules, tone, language |
| `01_help_guide.md` | Answering platform questions from `docs/SYSTEM.md` |
| `02_subscription.md` | Explaining and cancelling the student's own subscription |
| `03_teacher_concerns.md` | Receiving, classifying and recording concerns about a teacher |
| `04_safety_and_minors.md` | Child safety, privacy, manipulation and prompt injection |
| `05_examples.md` | Worked examples |

The runtime prompts in `agents/app/prompts/` implement these rules. When a rule here changes, the matching prompt and tests change with it.

## Agents and tools

| Agent | Purpose | Tools |
|---|---|---|
| Orchestrator (`orchestrator.md`) | Talks to the student | `ask_help_guide`, `get_my_context`, `request_subscription_cancellation`, `confirm_subscription_cancellation`, `report_teacher_concern` |
| Help guide specialist (`docs_reader.md`) | Reads `docs/SYSTEM.md` and summarizes | `read_help_guide` |
| Concern classifier (`concern_classifier.md`) | Returns category, severity and summary as JSON | none |
| Teacher summary (`teacher_summary.md`) | Admin analytics summary for one teacher | none, called by `POST /analytics/teacher-summary` |

The agent service never touches the database. Every read and write goes through the .NET API `/api/internal/*` endpoints with the agent key, and the API is the authoritative enforcement layer.

## Tiers

| Tier | Meaning | Cases |
|---|---|---|
| GREEN | Automatic | Help guide answers, reading the student's own context |
| YELLOW | Automatic with a check | Subscription cancellation (confirmation in a later turn), recording a concern |
| RED | Human only | Any safety situation beyond recording it and telling the student to talk to a trusted adult; account, password and profile changes; anything about other users |

## Rule IDs

| Prefix | Area |
|---|---|
| `COR` | Core |
| `HLP` | Help guide |
| `SUB` | Subscription |
| `CON` | Teacher concerns |
| `SAF` | Safety and minors |

Rule IDs are stable. A changed rule keeps its ID; a removed rule's ID is not reused.

## Precedence

1. Code-level enforcement
2. `00_core.md` hard rules and `04_safety_and_minors.md`
3. Other files in this directory
4. Examples in `05_examples.md`

## Enforced in code, not only in the prompt

| Rule | Enforcement |
|---|---|
| COR-03 | The student identity comes from the JWT via the .NET API (`user` in the chat request), never from the message; tools take no student id argument |
| SUB-03 | `confirm_subscription_cancellation` fails unless `request_subscription_cancellation` ran in the student's previous turn |
| SUB-05 | The API cancels only the calling student's subscription, is idempotent and writes an audit entry |
| CON-04 | The API rejects a concern about a teacher who does not teach the student |
| CON-06 | A Safety or Critical concern makes the orchestrator append the trusted-adult message if the reply lacks it |
| CON-07 | If the classifier fails, the concern is stored as Other with High severity so it is never lost or downgraded |
