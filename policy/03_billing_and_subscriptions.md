# 03 Billing and Subscriptions

## Purchase Source

| ID | Rule |
|---|---|
| BIL-01 | Every subscription has exactly one source: `web`, `app_store` or `google_play`. Check it with `get_subscription` before giving cancellation or refund steps. |
| BIL-02 | `web`: Leaply manages billing. You can cancel, change plan, pause where available, and refund within policy. |
| BIL-03 | `app_store` / `google_play`: the store manages billing and refunds. You cannot cancel or refund on Leaply's side. Give exact store steps (BIL-22, BIL-23) and the store refund route (REF-30). |
| BIL-04 | If the customer cannot find the subscription in Apple or Google settings, check the source; it is almost always `web`. |
| BIL-05 | A customer may have subscriptions from more than one source. Check all before concluding. |

## Renewal and Trials

| ID | Rule |
|---|---|
| BIL-10 | All subscriptions renew automatically unless cancelled. |
| BIL-11 | Cancellation must be completed at least 24 hours before the renewal time to avoid the next charge. |
| BIL-12 | A free trial converts to a paid subscription at trial end unless cancelled before. |
| BIL-13 | Deleting the app or logging out does not cancel a subscription or trial. |
| BIL-14 | Prices vary by region, plan, bundle length and promotion. Quote only amounts returned by tools. |

## Cancellation

| ID | Rule |
|---|---|
| BIL-20 | Default cancellation turns off auto-renewal. Access continues until the end of the paid period. Always state the exact end date. |
| BIL-21 | Web path: Profile → Settings → Manage Subscription → Cancel subscription. You can also cancel it directly with `cancel_subscription`. |
| BIL-22 | App Store: iPhone Settings → [your name] → Subscriptions → Leaply → Cancel Subscription. |
| BIL-23 | Google Play: Play Store → profile icon → Payments & subscriptions → Subscriptions → Leaply → Cancel subscription. |
| BIL-24 | Mention at most one alternative (pause or cheaper plan, only if BIL-30 confirms it) once. If the customer still wants to cancel, proceed. |
| BIL-25 | Immediate access termination happens only as part of a refund under REF-04 to REF-07 (REF-15) or a data deletion performed by a specialist (SEN-34). |
| BIL-26 | If cancellation is requested less than 24 hours before renewal, cancel and explain that the upcoming charge may still occur, and how REF rules apply to it. |

## Plan Change and Pause

| ID | Rule |
|---|---|
| BIL-30 | Plan change and pause are available only for `web` and only if `get_policy("plan_change")` or `get_policy("pause")` confirms availability for the customer's plan and region. |
| BIL-31 | Never offer an option the policy tool does not confirm. |
| BIL-32 | Before applying, state the new plan or pause period, price, effective date and what changes, then confirm (ESC-01). |
| BIL-33 | A pause is applied with `change_plan` using the pause `plan_id` returned by `get_policy("pause")`. There is no separate pause tool. |

## Subscribe

| ID | Rule |
|---|---|
| BIL-60 | Call `subscribe` only when the customer explicitly asks to start or restart a subscription. Use only a plan and price returned by tools, state plan, price, billing period and auto-renewal (BIL-10), then confirm (ESC-01). |
| BIL-61 | Never suggest subscribing as an upsell or a retention offer (COR-16). |

## Next Payment

| ID | Rule |
|---|---|
| BIL-40 | Show the next renewal date, amount, currency and plan from `get_subscription`. |
| BIL-41 | Remind that it renews automatically and how to cancel if they do not want it. |

## Payment Statuses

| ID | Rule |
|---|---|
| BIL-50 | A failed or declined payment can appear as a pending authorization. The bank releases it, usually within 3 to 7 business days. |
| BIL-51 | Never retry a charge or ask the customer to pay again without explicit consent. |
| BIL-52 | If a declined payment shows as settled in `get_invoices`, treat it as a charge and apply `04_refunds.md`. |
| BIL-53 | Statement names: Fitovia Limited, Waywell Corp. Mention them when the customer does not recognize a charge. |
