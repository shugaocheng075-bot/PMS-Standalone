# AGENTS.md

This file defines project-level agent rules for Vibe-style coding in `PMS-Standalone`.

## Scope
- Applies to all files under this project root.
- Use this guidance for coding agents (Copilot/Codex/Claude-style workflows).

## Mission
- Keep this .NET 8 + Vue 3 full-stack project stable while iterating quickly.
- Prefer small, reversible changes and clear validation steps.
- Prevent large, cross-cutting edits without an explicit plan.

## Hard Rules
- Do not refactor unrelated modules in the same change.
- One task per patch: feature, bugfix, or cleanup, not all at once.
- Keep API contracts backward compatible unless requirement says otherwise.
- If API 契约发生变化，必须在同一任务内验证 `pms-web` 受影响点.
- Do not change config defaults silently (`appsettings.json`, deployment scripts).
- Always include a verification section in task output.

## Architecture Guardrails
- 严格遵守后端分层:
  - `PMS.Domain/`: Entities, value objects — no infrastructure dependencies.
  - `PMS.Application/`: Contracts (interfaces), Models (DTOs) — no infrastructure dependencies.
  - `PMS.Infrastructure/`: Service implementations, persistence (SQLite/SqliteTableStore).
  - `PMS.API/`: Controllers, middleware — thin orchestration, delegate to services.
- Frontend (`pms-web/`):
  - `api/`: HTTP client modules, one per backend module.
  - `views/`: Page-level components, grouped by module.
  - `composables/`: Shared reactive logic.
  - `layout/`: App shell, navigation, responsive breakpoints.
- Prefer adding new service classes over enlarging massive existing files.
- Avoid circular dependencies between layers.

## Task Workflow (Required)
1. Read `memory-bank/project-brief.md` and `memory-bank/architecture.md`.
2. Update or create a short plan in `memory-bank/implementation-plan.md`.
3. Implement one scoped step.
4. Validate build and/or targeted verification.
5. Log done items in `memory-bank/progress.md`.

## Quality Bar
- Backend build: `dotnet build PMS.slnx`
- Frontend build: `cd pms-web && npm run build`
- For risky changes, verify affected pages in browser (localhost:5173).
- No dead code or placeholder TODOs without an issue note in `progress.md`.

## Prompting Conventions
- Each task must declare:
  - Goal
  - Non-goals
  - Constraints
  - Acceptance criteria
- If requirement is ambiguous, stop and ask for clarification before broad edits.
