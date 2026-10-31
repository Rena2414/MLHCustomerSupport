# 04 Refunds

## Eligibility

Refunds through Leaply apply only to `web` purchases. Evaluate in this order and apply the first matching rule.

| ID | Case | Rule |
|---|---|---|
| REF-01 | Duplicate charge | Same account, same plan, same billing period charged more than once. Refund each extra charge in full, any time. |
| REF-02 | Charge after cancellation | Cancellation completed at least 24 hours before the charge per `get_subscription`. Refund in full, any time. |
| REF-03 | Technical billing error | Charge amount differs from the plan price shown at purchase, or charge on a wrong plan. Refund the difference, any time. |
| REF-04 | Money-Back Guarantee | Request within 30 days of the initial purchase, first subscription period only. Full refund of the subscription; one-time add-ons excluded. |
| REF-05 | Trial converted to paid | The first charge after a trial is the initial purchase for REF-04. |
| REF-06 | EU / UK resident | 14-day withdrawal right from the first purchase or trial start. Use `get_policy("refund_eu_uk")` for the amount if the service was used. |
| REF-07 | California resident | Full refund if cancelled before midnight of the fifth business day after purchase. |
| REF-08 | Other regions with statutory rights | Use `get_policy("refund_<country_code>")`. |
| REF-09 | Anything else | Not refundable through self-service. Offer to turn off auto-renewal. If the customer insists, escalate with reason `exceeds_authority` and a recommendation. |

## Limits

| ID | Rule |
|---|---|
| REF-10 | Autonomous refund limit: 100 USD per charge, or local currency equivalent. Above it, escalate with reason `exceeds_authority`, recommended action and draft reply. |
| REF-11 | Never refund more than the original charge. |
| REF-12 | Never refund the same charge twice. Check `get_payments` for existing refunds first. |
| REF-13 | Low or no usage alone does not create eligibility. Include `get_usage` summary in escalations as context. |
| REF-14 | Offer no credits, free months, discounts or partial goodwill refunds unless `get_policy("goodwill")` returns an allowed option. |
| REF-15 | A refund under REF-04 to REF-07 also ends access and auto-renewal. State this before confirming. |
| REF-16 | Refunds under REF-01 to REF-03 do not affect access. |

## Processing

| ID | Rule |
|---|---|
| REF-20 | Refunds return to the original payment method only. |
| REF-21 | Processing time: up to 15 business days; up to 14 days for EU / UK. Bank posting can take additional days. |
| REF-22 | Before `process_refund`, confirm: charge date, amount, currency, payment method (last 4), effect on access (REF-15, REF-16). |
| REF-23 | After success, give the refund reference and timeline. |

## Store Purchases

| ID | Rule |
|---|---|
| REF-30 | App Store: reportaproblem.apple.com → sign in → "Request a refund" → select Leaply. Apple decides. |
| REF-31 | Google Play: play.google.com → Account → Order history → Leaply → "Report a problem" / "Request a refund". Within 48 hours of purchase Google may refund automatically; after that Google decides or forwards to the developer. |
| REF-32 | If the store refers the customer back to Leaply, escalate with reason `policy_gap`. |

## Decision Summary

```
source != web                    → REF-30 / REF-31
duplicate / post-cancel / error  → REF-01..03 → refund (check REF-10)
within 30 days of first purchase → REF-04 → refund (check REF-10)
regional statutory right          → REF-06..08
otherwise                         → REF-09
```
