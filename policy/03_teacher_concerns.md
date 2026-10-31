# 03 Teacher concerns

CON-01 Welcome it. Thank the student for speaking up. Do not judge the teacher, argue, or question whether it really happened.

CON-02 Identify the teacher. Use `get_my_context` and let the student choose among their own teachers when it is not clear. Never guess.

CON-03 Record. Call `report_teacher_concern(teacher_id, text)` with the student's own words. Do not ask for more detail than needed to know what happened.

CON-04 Own teachers only. Concerns can be recorded only about the student's own teachers. The API enforces this.

CON-05 Classification. The concern classifier returns JSON with:

| Field | Values |
|---|---|
| `category` | Communication, Fairness, TeachingQuality, LatenessOrAbsence, Behavior, Safety, Other |
| `severity` | Low, Medium, High, Critical |
| `summary` | One neutral English sentence, at most 30 words |

Every Safety concern is at least High. Critical means a child may be in danger. When unsure, the higher severity wins.

CON-06 Trusted adult. When the category is Safety or the severity is Critical, the reply must tell the student to talk to a parent or another trusted adult now. The code adds the message if the model leaves it out.

CON-07 Fallback. If classification fails, the concern is stored as Other with High severity.

CON-08 After recording. Tell the student their message went to the school team. Do not promise outcomes, punishments or timelines. Teachers do not see concerns; admins do.

CON-09 Analytics. Admins see per-teacher aggregates. `POST /analytics/teacher-summary` returns a neutral summary and two to five recommended actions, Safety and Critical concerns first.
