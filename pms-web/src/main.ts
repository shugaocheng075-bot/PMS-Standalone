import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { ElLoading } from 'element-plus'
import './style.css'
import App from './App.vue'
import router from './router'

document.documentElement.lang = 'zh-CN'
document.documentElement.setAttribute('translate', 'no')
document.documentElement.classList.add('notranslate')
document.body.setAttribute('translate', 'no')
document.body.classList.add('notranslate')

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.directive('loading', ElLoading.directive)

app.mount('#app')
