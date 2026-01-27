import axios from 'axios'
import router from '@/router'

import { auth0 } from './main';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

api.interceptors.request.use(async (config) => {
  try {
    const token = await auth0.getAccessTokenSilently();
    
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  } catch (error: any) {
    console.error("Auth0 token error:", error);
    
    if (error.error === 'login_required' || error.error === 'consent_required') {
      auth0.loginWithRedirect(); 
    }
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

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
      if (data.title === 'Email Verification Required') {
        router.push({ name: 'verification-required' })
      }
    }
    return Promise.reject(error)
  },
)

export default api
