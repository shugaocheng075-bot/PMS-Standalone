# Implementation Prompt (PMS-Standalone)

Implement the approved task for `PMS-Standalone`.

Required behavior:
- Read `AGENTS.md` and `memory-bank/*.md` first.
- Modify only declared in-scope files.
- Keep backward compatibility for API contracts unless requirement says otherwise.
- 严格遵守分层: Domain → Application → Infrastructure → API.
- Add concise code comments only for non-obvious logic.

Before finishing:
1. Run backend build: `dotnet build PMS.slnx`
2. Run frontend build: `cd pms-web && npm run build`
3. Verify affected pages if UI was changed.
4. Update `memory-bank/progress.md` with what changed and how it was verified.
