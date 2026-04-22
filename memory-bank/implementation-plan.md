# Implementation Plan

## Goal
- 补齐 VS Code `tasks.json`，支持一键启动前后端与完整构建任务。
- 新增 `stop-dev` 脚本，便于快速停止本地开发服务。
- 执行并记录前后端完整构建校验结果。

## Non-goals
- 不修改任何业务模块逻辑与 API 契约。
- 不调整部署脚本与生产配置。

## Constraints
- 复用现有 `scripts/start-dev.ps1` 的启动方式，避免引入并行启动副作用。
- 变更范围仅限本地开发体验相关文件（`.vscode/`、`scripts/`、`memory-bank/`）。

## Steps
1. 检查当前启动脚本和任务配置现状。
2. 新增 `scripts/stop-dev.ps1` 与 `scripts/stop-dev.cmd`。
3. 新增 `.vscode/tasks.json`，提供启动、停止、后端构建、前端构建、全量构建任务。
4. 执行 `dotnet build PMS.sln` 与 `powershell -ExecutionPolicy Bypass -File .\\scripts\\build-frontend.ps1` 完成校验。

## Status
- 2026-04-22: 已完成步骤 1-4。

## Verification
- `dotnet build PMS.sln` 通过（0 警告，0 错误）。
- `powershell -ExecutionPolicy Bypass -File .\\scripts\\build-frontend.ps1` 通过（vite build success）。