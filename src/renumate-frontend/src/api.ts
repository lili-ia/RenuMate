import axios from 'axios'
import router from '@/router'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

export const setAuthToken = (token: string) => {
  api.interceptors.request.use((config) => {
    config.headers.Authorization = `Bearer ${token}`
    return config
  })
}

api.interceptors.response.use(
  (response) => response, 
  (error) => {
    if (error.response && error.response.status === 403) {
      const data = error.response.data

      if (
        data.detail === 'Your account is currently inactive.' ||
        data.title === 'Account Deactivated'
      ) {
        router.push({ name: 'reactivate-request' })
      }
    }

    return Promise.reject(error)
  },
)

export default api
