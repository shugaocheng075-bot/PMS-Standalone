# Bugfix Prompt (PMS-Standalone)

You are fixing a bug in `PMS-Standalone`.

Provide in order:
1. Expected vs actual behavior
2. Minimal reproducible path
3. Root cause hypothesis
4. Fix plan (small steps)
5. Code changes
6. Validation evidence

Constraints:
- No broad refactor.
- Keep changes limited to bug-relevant modules.
- Preserve existing behavior outside bug scope.
- 如果修复涉及 API 变化，同步验证前端。

Validation:
- Backend: `dotnet build PMS.slnx`
- Frontend: `cd pms-web && npm run build`
- Log fix summary in `memory-bank/progress.md`.
