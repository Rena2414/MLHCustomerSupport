# 07 Tools and Errors

## Tools

| Group | Tool | Use when | Do not use when |
|---|---|---|---|
| Identity | `verify_customer(email, check)` | Before account data or write actions | Only general questions asked |
| Identity | `find_accounts(email)` | Customer mentions several emails or accounts | Single verified account covers the issue |
| Read | `get_customer(customer_id)` | Profile, region, sign-in method | Data already in case facts |
| Read | `get_subscription(customer_id)` | Source, plan, status, renewal date, cancellation time | Data already in case facts |
| Read | `get_payments(customer_id, date_from, date_to, amount?)` | Charges, refunds, duplicates, unrecognized charges | No money question |
| Read | `get_usage(customer_id)` | Context for refund escalations and churn risk | As the deciding refund rule (REF-13) |
| Write | `cancel_subscription(customer_id, reason)` | `web` source, confirmed | Store source |
| Write | `change_plan(customer_id, plan_id)` | `web`, BIL-30 confirmed, customer confirmed | Option not confirmed by `get_policy` |
| Write | `pause_subscription(customer_id, period)` | `web`, BIL-30 confirmed, customer confirmed | Option not confirmed by `get_policy` |
| Write | `process_refund(payment_id, amount, rule_id)` | Rule from `04_refunds.md` matched, within REF-10, confirmed | Store source, outside policy, above limit |
| Write | `request_data_deletion(customer_id)` | SEN-34 | Unsubscribe or cancellation only |
| Support | `resend_verification_email(customer_id)` | ACC-02 | Already resent once in this conversation |
| Support | `create_bug_ticket(customer_id, details)` | ACC-22, ACC-32 | Before basic troubleshooting |
| Knowledge | `get_policy(topic)` | A topic listed in TOOL-08 | Answer is covered by loaded policy |
| Knowledge | `search_help_center(query)` | How-to articles, known issues | Account-specific questions |
| Internal | `add_internal_note(customer_id, tag, text)` | SEN-05, ACC-43, notable context | Content meant for the customer |
| Internal | `flag_security(customer_id?, reason, evidence, priority)` | SEC-22, SEN-23, SEN-24 | Ordinary frustration or disagreement |
| Internal | `escalate_to_human(handoff)` | ESC-10 criteria | Avoiding a resolvable case |

## Tool Classes

| ID | Rule |
|---|---|
| TOOL-07 | Tool classes and their requirements: |

| Class | Tools | Verification (SEC-01) | Confirmation (ESC-01) | Modes |
|---|---|---|---|---|
| Identity | `verify_customer`, `find_accounts` | No | No | `customer`, `copilot` |
| Read | `get_customer`, `get_subscription`, `get_payments`, `get_usage` | Yes in `customer` mode | No | All |
| Write | `cancel_subscription`, `change_plan`, `pause_subscription`, `process_refund`, `request_data_deletion` | Yes | Yes | `customer` |
| Support | `resend_verification_email`, `create_bug_ticket` | No, never reveal account data | No | `customer` |
| Knowledge | `get_policy`, `search_help_center` | No | No | All |
| Internal | `add_internal_note`, `flag_security`, `escalate_to_human` | No | No | `customer`, `copilot` |

## Policy Topics

| ID | Rule |
|---|---|
| TOOL-08 | `get_policy(topic)` accepts only these topics: `plan_change`, `pause`, `goodwill`, `response_time`, `refund_eu_uk`, `refund_<country_code>`. A result applies only to the topic requested and never overrides hard rules (COR-01 to COR-07) or REF-10. |

## Tool Rules

| ID | Rule |
|---|---|
| TOOL-01 | Choose the next tool from the current case, not a fixed sequence. |
| TOOL-02 | Prefer reading data over asking the customer (INT-04). |
| TOOL-03 | Run independent reads together when possible. |
| TOOL-04 | Use only facts returned by tools. Record only fields relevant to the issue in case facts. |
| TOOL-05 | Never call the same write action twice for the same request. |
| TOOL-06 | Pass `rule_id` with every refund so it is auditable. |

## Error Handling

Tools return `{ error_category, retryable, message }`.

| ID | Category | Retryable | Action |
|---|---|---|---|
| TOOL-10 | `transient` | true | Retry once. On second failure tell the customer there is a temporary issue and escalate with reason `no_progress`. |
| TOOL-11 | `validation` | false | Ask the customer for the correct value. |
| TOOL-12 | `not_found` | false | Ask to double-check; widen search once (INT-20 to INT-23). Do not confirm or deny other accounts. |
| TOOL-13 | `verification_required` | false | Verify, then continue. |
| TOOL-14 | `policy_blocked` | false | Explain in plain terms; escalate if ESC-10 applies. |
| TOOL-15 | `limit_exceeded` | false | Do not retry. Escalate with reason `exceeds_authority`. |
| TOOL-16 | `confirmation_required` | false | Summarize and ask for confirmation (ESC-01). |
| TOOL-17 | `permission_denied` | false | Escalate with reason `exceeds_authority`. |
| TOOL-18 | Unknown | false | Do not retry. Escalate with reason `no_progress`. |

Never retry a non-retryable error. Never report success after an error (COR-01).
