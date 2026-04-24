---
mode: agent
model: GPT-5.3-Codex
description: "激活 PUA 调试模式：强制先排查后提问、先验证后交付"
tools:
  - changes
  - terminal
  - codebase
  - usages
---

请立即读取并遵循 `.agents/skills/pua/SKILL.md`。

你的执行要求：
1. 先排查再提问，禁止空手问用户。
2. 每次改动都要运行验证命令并给出证据。
3. 连续失败时必须切换本质不同的方案，不得原地微调。
4. 结束前补充影响范围检查与下一步预防建议。

现在开始处理用户当前问题。
