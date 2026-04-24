import { existsSync, readFileSync, realpathSync, rmSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

const configDir = dirname(fileURLToPath(import.meta.url))
const projectRoot = realpathSync.native?.(configDir) ?? realpathSync(configDir)

// 在 Windows 上通过 junction (C:\Users\...\PMS-Standalone → D:\project\...)
// 启动时，process.cwd() 仍返回 junction 路径，而 projectRoot 被解析为真实路径。
// 该差异会导致 Vite 在依赖优化阶段拿 realpath 去匹配 esbuild 基于 cwd 产出的输出，
// 触发 "Cannot read properties of undefined (reading 'imports')" 崩溃。
// 在此强制对齐 cwd 与 projectRoot，保证两者始终一致。
if (process.cwd() !== projectRoot) {
  try {
    process.chdir(projectRoot)
  } catch {
    // 若无法切换（例如权限问题），保留原 cwd；后续 realpath 逻辑会尝试回退。
  }
}

function clearStaleViteCache(projectRoot: string) {
  const normalizedProjectRoot = projectRoot.replace(/\\/g, '/')
  const cacheDirs = [
    resolve(projectRoot, 'node_modules/.vite'),
    resolve(projectRoot, '../.vite'),
  ]

  const hasStaleCache = cacheDirs.some((cacheDir) => {
    const metadataFile = resolve(cacheDir, 'deps/_metadata.json')
    if (!existsSync(metadataFile)) {
      return false
    }

    const content = readFileSync(metadataFile, 'utf8')
    const matchedPaths = content.match(/[A-Za-z]:\/[^"\r\n]*?\/pms-web(?:\/[^"\r\n]*)?/g) ?? []
    return matchedPaths.some((matchedPath) => !matchedPath.startsWith(normalizedProjectRoot))
  })

  if (!hasStaleCache) {
    return
  }

  for (const cacheDir of cacheDirs) {
    rmSync(cacheDir, { recursive: true, force: true })
  }

  console.warn('[vite] Removed stale cache generated for a different workspace path.')
}

clearStaleViteCache(projectRoot)

// https://vite.dev/config/
export default defineConfig({
  root: projectRoot,
  cacheDir: resolve(projectRoot, 'node_modules/.vite'),
  plugins: [
    vue(),
    AutoImport({
      resolvers: [ElementPlusResolver()],
      dts: false,
    }),
    Components({
      resolvers: [ElementPlusResolver({ importStyle: 'css' })],
      dts: false,
    }),
  ],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5111',
        changeOrigin: true,
      },
    },
  },
  preview: {
    host: '0.0.0.0',
    port: 4173,
    strictPort: true,
  },
})
