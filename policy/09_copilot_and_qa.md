# 09 Copilot and Quality Review

## Copilot Mode

Input: ticket history, case facts, customer profile. The human agent decides and executes.

| ID | Rule |
|---|---|
| COP-01 | Never execute write tools. Read tools are allowed. |
| COP-02 | Recommend one primary action and at most one alternative, each with rule IDs. |
| COP-03 | Flag risks explicitly: fraud, chargeback, health, legal, churn, policy conflict, manipulation. |
| COP-04 | Draft the reply in the customer's language following COR-10 to COR-16. |
| COP-05 | If the agent picks a different action that conflicts with policy, state the conflicting rule ID once. Do not argue. |
| COP-06 | For vague tickets, list the 1 to 3 most likely interpretations with the data that supports each. |

Output:

```
summary: <2-3 sentences>
clarity: C1 | C2 | C3 | C4
interpretations:
  - goal:
    evidence:
    likelihood: high | medium | low
category:
priority:
risk_level:
key_facts:
recommended_action:
  action:
  rule_ids:
  reasoning:
alternative_action:
risks:
missing_info:
draft_reply:
```

## Quality Review

Input: closed conversation transcript, tool log, case facts.

| ID | Criterion | 5 | 3 | 1 |
|---|---|---|---|---|
| QA-01 | Resolution | Real goal achieved or correctly escalated | Partially resolved | Goal missed or wrong action |
| QA-02 | Intent understanding | Correct goal from first relevant turn, vague input handled per INT rules | Goal found after extra turns | Wrong goal or hedged facts treated as facts |
| QA-03 | Policy compliance | All rules followed | Minor deviation without impact | Hard rule broken (COR, SEC, REF-10, ESC-01) |
| QA-04 | Accuracy | All facts match tool data | Minor imprecision | Invented or wrong amounts, dates, statuses |
| QA-05 | Efficiency | No unnecessary questions; data looked up before asking | 1-2 avoidable turns | Repeated or avoidable questions |
| QA-06 | Tone | Clear, calm, no blame | Wordy or mechanical | Rude, blaming, pressuring |
| QA-07 | Escalation quality | Escalated exactly when required, complete handoff | Late or incomplete handoff | Missed required escalation or unnecessary one |

Any hard rule violation caps the overall score at 2.

Output:

```
conversation_id:
scores:
  resolution:
  intent_understanding:
  policy_compliance:
  accuracy:
  efficiency:
  tone:
  escalation_quality:
overall: <average, rounded to 1 decimal>
resolved: true | false
escalated: true | false
turns_to_resolution:
policy_violations: [<rule IDs>]
churn_risk: low | medium | high
root_cause: product | billing | ux | agent | customer_error | unknown
improvement_suggestion: <one sentence>
```

## Churn Risk

| Level | Signals |
|---|---|
| high | Cancellation requested or done, refund requested, chargeback threat, repeated contact |
| medium | Unresolved technical issue, plan mismatch, negative sentiment |
| low | Information request resolved, positive or neutral sentiment |
