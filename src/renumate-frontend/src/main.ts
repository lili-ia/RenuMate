import './assets/main.css'

import { createApp } from 'vue'
import { createAuth0 } from '@auth0/auth0-vue'
import ToastPlugin from 'vue-toast-notification'

import App from './App.vue'

const app = createApp(App)

console.log(import.meta.env.AUTH0_DOMAIN)

app.use(
  createAuth0({
    domain: import.meta.env.VITE_AUTH0_DOMAIN,
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      scope: 'openid profile email',
    },
    cacheLocation: 'localstorage',
    useRefreshTokens: true,
  }),
)

app.use(ToastPlugin)

app.mount('#app')
