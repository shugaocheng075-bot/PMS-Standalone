# 国内 ECS + NGINX 部署说明（阿里云）

## 目标
- 前端：NGINX 静态托管 `/opt/pms/www`
- 后端：systemd 托管 `PMS.API`，监听 `127.0.0.1:5111`
- 反向代理：`/api` 走后端，其余走前端

## 0. 前置条件
- ECS 在中国大陆地域
- 域名已备案（国内服务器必须）
- ECS 已安装：`dotnet 8`、`nginx`、`rsync`
- 证书文件已放到：
  - `/etc/nginx/ssl/bjgoodwill.fun.pem`
  - `/etc/nginx/ssl/bjgoodwill.fun.key`

## 1. 本地打包（Windows）
在本仓库执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-cn-deploy-bundle.ps1
```

打包输出目录：`deploy-cn/bundle`

## 2. 上传到 ECS
把整个 `deploy-cn` 目录上传到 ECS，比如：`/root/deploy-cn`

## 3. ECS 执行部署
```bash
cd /root/deploy-cn
chmod +x deploy-on-ecs.sh
./deploy-on-ecs.sh
```

## 4. DNS 指向（阿里云云解析）
- `bjgoodwill.fun` -> ECS 公网 IP（A 记录）
- `api.bjgoodwill.fun` -> ECS 公网 IP（A 记录）

## 5. 验证
```bash
curl -I https://bjgoodwill.fun/
curl -I https://bjgoodwill.fun/api/health
systemctl status pms-api
systemctl status nginx
```

## 6. 说明
- 已禁用 Cloudflare Tunnel 后，访问链路将走国内 ECS，不再经过境外 Cloudflare 边缘。
- 若接口没有 `/api/health`，可改成任意已存在接口进行验证。
