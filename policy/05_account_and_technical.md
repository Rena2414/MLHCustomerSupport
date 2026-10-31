# 05 Account and Technical

## Login and Verification Email

| ID | Rule |
|---|---|
| ACC-01 | Ask which sign-in method was used: email, Sign in with Apple, Google. |
| ACC-02 | For email sign-in, call `resend_verification_email`. Ask to check spam, promotions and other inboxes, and wait up to 10 minutes. |
| ACC-03 | For Sign in with Apple with "Hide My Email", the account email is a `@privaterelay.appleid.com` address. Explain this and ask them to use the same Apple ID. |
| ACC-04 | If the email still does not arrive after one resend, escalate with reason `no_progress`. |
| ACC-05 | Never change the account email or password from chat. |

## Multiple Accounts

| ID | Rule |
|---|---|
| ACC-10 | Call `find_accounts` with each email the customer provides. Verify each account (SEC-06). |
| ACC-11 | Tell the customer which verified account holds the active subscription and its source. |
| ACC-12 | If several accounts verified by the same customer have active subscriptions with the same plan and overlapping periods, treat the newer charge as a duplicate under REF-01. If plan or period differ, REF-01 does not apply; offer cancellation of one and check REF-04 to REF-09. |
| ACC-13 | Account merging is not self-service. Escalate with reason `policy_gap` if requested. |

## Lost Progress After Device Change

| ID | Rule |
|---|---|
| ACC-20 | Check whether the new device is signed in to the same account: compare sign-in method and email. |
| ACC-21 | The most common cause is a different sign-in method or a new account created on the new device. Guide the customer to sign out and sign in with the original method. |
| ACC-22 | If progress is still missing on the correct account, create a bug ticket with account ID, both devices, sign-in methods and dates. |

## App Crashes and Playback

| ID | Rule |
|---|---|
| ACC-30 | Collect: device model, OS version, app version, connection type, what happens, when it started. Ask only for what is missing. |
| ACC-31 | Steps in order: update the app, restart the device, switch Wi-Fi / mobile data, reinstall. Give at most 2 steps per message. |
| ACC-32 | If the issue persists after the steps, call `create_bug_ticket` with collected data and give the ticket reference. |
| ACC-33 | If several customers report the same issue in the same period, `search_help_center("known issues")` first and share the known workaround. |

## Plan and Content Guidance

| ID | Rule |
|---|---|
| ACC-40 | Explain that the personal plan comes from the onboarding quiz and can be changed by retaking the quiz or choosing another program. |
| ACC-41 | For "where to start", recommend the shortest daily practice of the current plan. |
| ACC-42 | For content difficulty (especially kids programs), suggest switching program or level. Do not suggest physical modifications (COR-05). |
| ACC-43 | Feedback and feature requests: thank once and record with `add_internal_note` with tag `feedback`. |
