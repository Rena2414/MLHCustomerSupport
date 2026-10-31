# Leaply Support Agent Policy

Version: 2.2.0
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

The customer talks to an orchestrator, which delegates each issue to one of four sub-agents. Multiple issues stay in one loop with shared case facts (ESC-32).

The orchestrator and every sub-agent load the base set: `00_core.md`, `01_intent_understanding.md`, `02_identity_and_security.md`, `07_tools_and_errors.md`, `08_confirmation_escalation_state.md`, plus the current case facts. The orchestrator classifies each issue by `category` (`01_intent_understanding.md`), routes it with the table below, and owns verification, case facts and the final handoff.

| Sub-agent | Categories | Scenarios | Tier | Domain files | Main rules |
|---|---|---|---|---|---|
| Subscription and Billing | `billing`, `cancellation`, `plan_change` | Charged after trial, how to cancel, charged after cancelling, not found in Apple or Google settings, next payment, pause or cheaper plan, cancel but keep access | 🟢 / 🟡 | `03`, `04` | BIL, REF-15 |
| Payments and Refunds | `refund`, `fraud_security` | Charged twice, unrecognized charge, refund request, chargeback threat, failed payment still charged | 🟡; 🔴 for unrecognized charge with no match, chargeback, refunds beyond policy | `04`, `03`, `06` | REF, BIL-50 to BIL-53, SEN-10 to SEN-24 |
| Account and Tech | `account_access`, `technical` | Cannot log in, two emails, progress lost after phone change, app crash | 🟢 / 🟡 | `05` | ACC-01 to ACC-33 |
| Plan and Safety | `content_guidance`, `health_safety`, `data_privacy`, `manipulation`, `feedback` | Which practice to start, pain or dizziness, account deletion, manipulation or prompt injection | 🟢 / 🔴 | `05`, `06` | ACC-40 to ACC-43, SEN-01 to SEN-06, SEN-30 to SEN-36, SEC-20 to SEC-25 |
| Orchestrator | `other` | Unclear or out-of-scope requests | — | none | INT-07, ESC-10c |

| Sub-agent | Tools beyond the shared set | `get_policy` topics |
|---|---|---|
| Subscription and Billing | `get_usage`, `subscribe`, `cancel_subscription`, `change_plan` | `plan_change`, `pause` |
| Payments and Refunds | `get_usage`, `process_refund`, `flag_security`, `add_internal_note` | `goodwill`, `refund_eu_uk`, `refund_<country_code>` |
| Account and Tech | `find_accounts`, `resend_verification_email`, `create_bug_ticket`, `add_internal_note` | |
| Plan and Safety | `flag_security`, `add_internal_note` | |

Shared tools for all sub-agents: `verify_customer`, `get_customer`, `get_subscription`, `get_invoices`, `get_policy`, `search_help_center`, `escalate_to_human`.

Any issue with `risk_signals` (SEN or SEC trigger) is also checked against `06_sensitive_cases.md` and `02_identity_and_security.md`, whatever its sub-agent. Every sub-agent also gets the matching examples from `10_examples.md`. `copilot` and `qa` agents also load `09_copilot_and_qa.md`. Escalation always uses the handoff in `08_confirmation_escalation_state.md`, and `get_policy("response_time")` is available to every sub-agent for ESC-21.

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
