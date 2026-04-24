import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { ElLoading } from 'element-plus'
import './style.css'
import './premium-ui.css'
import App from './App.vue'
import router from './router'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'


document.documentElement.lang = 'zh-CN'
document.documentElement.setAttribute('translate', 'no')
document.documentElement.classList.add('notranslate')
document.body.setAttribute('translate', 'no')
document.body.classList.add('notranslate')

const app = createApp(App)
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}


app.use(createPinia())
app.use(router)
app.directive('loading', ElLoading.directive)

void router.isReady().finally(() => {
	app.mount('#app')
})
