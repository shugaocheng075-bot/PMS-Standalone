# Task Intake Prompt (PMS-Standalone)

You are working on `PMS-Standalone`.

## Input Requirement
<fill requirement here>

## Output Format (strict)
1. Goal (one sentence)
2. Non-goals (bullet list)
3. Constraints (bullet list)
4. Impacted modules (exact folders/files)
5. Plan (small ordered steps)
6. Acceptance criteria (testable)

Rules:
- Keep scope minimal.
- Do not include unrelated refactors.
- 如果 API 契约变化，必须同步验证前端受影响模块。
- If requirement is ambiguous, ask clarifying questions first.
