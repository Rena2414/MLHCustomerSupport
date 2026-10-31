# Leaply Support Agent Policy

Version: 2.0.0
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
| `05_account_and_technical.md` | Login, multiple accounts, lost progress, bugs, plan guidance | Retrieved by `get_policy` |
| `06_sensitive_cases.md` | Health, chargeback, unrecognized charge, data deletion | Always, system prompt |
| `07_tools_and_errors.md` | Tool selection and structured error handling | Always, system prompt |
| `08_confirmation_escalation_state.md` | Confirmation, escalation, handoff, case facts | Always, system prompt |
| `09_copilot_and_qa.md` | Copilot output and conversation quality review | Loaded per mode |
| `10_examples.md` | Reference conversations, including vague requests | Few-shot, system prompt |

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
3. Topic retrieved by `get_policy(topic)` for regional or plan-specific terms
4. Other files in this directory
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
