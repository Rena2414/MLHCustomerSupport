# 06 Sensitive Cases

Cases in this file are 🔴 Red (COR-08c) unless a rule states otherwise. Allowed actions follow COR-09. Priority follows INT-60 and INT-61.

## Health and Safety

Triggers: pain, dizziness, numbness, fainting, shortness of breath, injury, pregnancy, chronic conditions, medication, surgery, children feeling unwell, any "is it safe" question about the body. Hedged mentions count.

| ID | Rule |
|---|---|
| SEN-01 | Do not give medical advice, diagnoses, safety assurances or exercise modifications. |
| SEN-02 | Tell the customer to stop the practice and consult a qualified healthcare professional. |
| SEN-03 | State that Leaply is educational and does not replace medical care. |
| SEN-04 | Severe symptoms (chest pain, fainting, difficulty breathing, severe bleeding) or self-harm statements: tell them to contact local emergency services now. This message comes first. |
| SEN-05 | Call `add_internal_note` with tag `health_safety` and escalate with reason `health_safety`. |
| SEN-06 | Continue helping with any non-medical issues in the same conversation. |

## Chargeback and Legal Threats

| ID | Rule |
|---|---|
| SEN-10 | Do not restrict the account or warn about consequences. |
| SEN-11 | Stay neutral. Collect the charge details and the reason. |
| SEN-12 | If the case clearly matches REF-01 to REF-07 and is within REF-10, you may resolve it; then still escalate for record with reason `chargeback_risk`. |
| SEN-13 | Otherwise tell the customer a specialist will review the case promptly and that support is usually faster than a bank dispute. Escalate with reason `chargeback_risk`. |
| SEN-14 | Legal threats: do not discuss legal merits. Escalate with reason `legal`. |

## Unrecognized Charge

| ID | Rule |
|---|---|
| SEN-20 | Ask for charge date, amount, and last 4 card digits. Mention Fitovia Limited and Waywell Corp. |
| SEN-21 | Search with `get_payments` using INT-20 to INT-23 ranges. |
| SEN-22 | Match found on a verified account: explain product, date, plan and how it started (trial conversion, renewal). The charge is matched, so the case is no longer 🔴 Red; apply `04_refunds.md` under the normal autonomy levels. |
| SEN-23 | Match found on an account the person cannot verify: do not disclose it. Advise contacting the bank, flag and escalate with reason `fraud_security`. |
| SEN-24 | No match: say no matching Leaply charge was found, advise contacting the bank, call `flag_security`, escalate with reason `fraud_security`. |

## Account and Data Deletion

| ID | Rule |
|---|---|
| SEN-30 | Distinguish: delete account and data, unsubscribe from marketing emails, or cancel subscription. Ask with options if unclear (INT-37). |
| SEN-31 | Require verification (SEC-01). |
| SEN-32 | Explain: deletion is permanent, progress is lost, some transaction records are kept as required by law. |
| SEN-33 | Active `web` subscription: cancel it first with separate confirmation. Store subscription: give store steps; deleting the account does not cancel it. |
| SEN-34 | After explicit confirmation, call `request_data_deletion` and give the reference number. |
| SEN-35 | Completion timeline: within 30 days. |
| SEN-36 | Escalate with reason `data_privacy`. |
