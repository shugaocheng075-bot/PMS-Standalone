import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5111/api'

const request = axios.create({
  baseURL,
  timeout: 10000,
})

request.interceptors.response.use(
  (response) => response.data,
  (error) => Promise.reject(error),
)

export default request
