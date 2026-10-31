# Leaply Support Agent Policy

Version: 2.1.0
Last updated: 2026-10-31
Product: Leaply (web, iOS, Android)
Owner: Support Operations

## Structure

| File | Scope | Loaded |
|---|---|---|
| `00_core.md` | Role, modes, hard rules, tone, language | Always, system prompt |
| `01_intent_understanding.md` | Reading clear and vague messages, clarification, intent, priority, risk | Always, system prompt |
| `02_identity_and_security.md` | Verification, privacy, manipulation and prompt injection | Always, system prompt |
| `03_billing_and_subscriptions.md` | Purchase source, renewal, trials, cancellation, plan change, payment status | Always, system prompt |
| `04_refunds.md` | Refund eligibility and limits | Always, system prompt |
| `05_account_and_technical.md` | Login, multiple accounts, lost progress, bugs, plan guidance | Always, system prompt |
| `06_sensitive_cases.md` | Health, chargeback, unrecognized charge, data deletion | Always, system prompt |
| `07_tools_and_errors.md` | Tool selection and structured error handling | Always, system prompt |
| `08_confirmation_escalation_state.md` | Confirmation, escalation, handoff, case facts | Always, system prompt |
| `09_copilot_and_qa.md` | Copilot output and conversation quality review | Loaded per mode |
| `10_examples.md` | Reference conversations, including vague requests | Few-shot, system prompt |

Regional and plan-specific terms are not files in this directory. They are returned by `get_policy(topic)` for the topics listed in TOOL-08.

## Sub-Agent Routing

Every sub-agent loads the base set: `00_core.md`, `01_intent_understanding.md`, `02_identity_and_security.md`, `07_tools_and_errors.md`, `08_confirmation_escalation_state.md`, plus the current case facts. The orchestrator routes each issue by its `category` (`01_intent_understanding.md`) and adds the files below. An issue with several categories gets the union of their files. An issue with any `risk_signals` (SEN or SEC trigger, including chargeback and legal threats) also gets `06_sensitive_cases.md`, whatever its category.

| Category | Domain files | Main rules | `get_policy` topics |
|---|---|---|---|
| `billing` | `03` | BIL | `plan_change`, `pause` |
| `refund` | `03`, `04` | BIL, REF | `goodwill`, `refund_eu_uk`, `refund_<country_code>` |
| `cancellation` | `03`, `04` | BIL-20 to BIL-26, REF-15 | |
| `plan_change` | `03` | BIL-30 to BIL-32 | `plan_change`, `pause` |
| `account_access` | `05` | ACC-01 to ACC-22 | |
| `technical` | `05` | ACC-30 to ACC-33 | |
| `content_guidance` | `05` | ACC-40 to ACC-42, COR-05 | |
| `health_safety` | `06` | SEN-01 to SEN-06 | |
| `data_privacy` | `06`, `03` | SEN-30 to SEN-36, BIL-20 | |
| `fraud_security` | `06`, `03`, `04` | SEN-10 to SEN-24, SEC-20 to SEC-25 | |
| `feedback` | `05` | ACC-43 | |
| `other` | none | ESC-10c | |

Every routed sub-agent also gets the matching examples from `10_examples.md`. `copilot` and `qa` sub-agents also load `09_copilot_and_qa.md`. Escalation always uses the handoff in `08_confirmation_escalation_state.md`, and `get_policy("response_time")` is available to every sub-agent for ESC-21.

## Rule IDs

| Prefix | Area |
|---|---|
| `COR` | Core |
| `INT` | Intent understanding |
| `SEC` | Identity and security |
| `BIL` | Billing and subscriptions |
| `REF` | Refunds |
| `ACC` | Account and technical |
| `SEN` | Sensitive cases |
| `TOOL` | Tools and errors |
| `ESC` | Confirmation, escalation, state |
| `COP` | Copilot |
| `QA` | Quality review |

Rule IDs are stable. A changed rule keeps its ID; a removed rule's ID is not reused. Handoffs, logs and QA reviews reference rules by ID.

## Precedence

1. Code-level enforcement (gates, hooks, limits)
2. `00_core.md` hard rules
3. Result of `get_policy(topic)` for a TOOL-08 topic, only within that topic's scope
4. Other files in this directory, all at the same level
5. Examples in `10_examples.md`

Examples illustrate rules and never override them.

## Enforced in code, not only in prompt

| Rule | Enforcement |
|---|---|
| SEC-01 | Write tools reject calls without a verified session |
| REF-10 | `process_refund` rejects amounts above 100 USD without human approval |
| ESC-01 | Write tools require a confirmation token from the last customer turn |
| TOOL-05 | Duplicate write calls with the same idempotency key are rejected |
| SEC-12 | Every write and every `flag_security` call is logged |
