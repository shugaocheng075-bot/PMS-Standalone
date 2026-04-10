# Implementation Plan

## Goal
- 启动 PMS 前后端本地服务，并恢复通过 bjgoodwill.fun / api.bjgoodwill.fun 的外网访问。

## Non-goals
- 不修改现有业务代码与接口契约。
- 不执行 ECS 正式发布流程。

## Constraints
- 优先复用现有 Cloudflare Tunnel 脚本与既有域名配置。
- 仅做当前机器可执行的启动与验证。

## Steps
1. 检查 cloudflared、Cloudflare 凭据、本地端口占用与启动依赖。
2. 启动后端 API 与前端 Web 服务。
3. 启动域名隧道并验证本地与外网入口。

## Status
- 2026-04-10: 已完成。cloudflared、证书与既有隧道 `pms-bjgoodwill` 均可用；已启动本机 API（5111）、Vite 前端（5173）与 Cloudflare Tunnel。

## Verification
- 本地 `http://127.0.0.1:5173/` 返回 200。
- 本地 `http://127.0.0.1:5111/api/health` 返回 401，证明 API 服务在线且鉴权链路生效。
- 外网 `https://bjgoodwill.fun/` 返回登录页（200）。
- 外网 `https://bjgoodwill.fun/api/health` 与 `https://api.bjgoodwill.fun/api/health` 返回 401，证明域名反代链路可达。