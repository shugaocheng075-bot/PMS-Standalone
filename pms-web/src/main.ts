import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
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
app.use(ElementPlus)

app.mount('#app')
