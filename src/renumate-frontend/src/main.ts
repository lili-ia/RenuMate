import './assets/main.css'

import { createApp } from 'vue'
import { createAuth0 } from '@auth0/auth0-vue'
import 'vue3-toastify/dist/index.css'
import Vue3Toastify, { type ToastContainerOptions } from 'vue3-toastify'
import router from './router'
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import 'primeicons/primeicons.css'

import App from './App.vue'

const app = createApp(App)

app.use(Vue3Toastify, {
  autoClose: 3000,
  position: 'bottom-right',
} as ToastContainerOptions)


const indigoPreset = definePreset(Aura, {
    semantic: {
        primary: {
            50: '{indigo.50}',
            100: '{indigo.100}',
            200: '{indigo.200}',
            300: '{indigo.300}',
            400: '{indigo.400}',
            500: '{indigo.500}',
            600: '{indigo.600}',
            700: '{indigo.700}',
            800: '{indigo.800}',
            900: '{indigo.900}',
            950: '{indigo.950}'
        }
    }
});

export const auth0 = createAuth0({
    domain: import.meta.env.VITE_AUTH0_DOMAIN,
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      scope: 'openid profile email offline_access',
    },
    cacheLocation: 'localstorage',
    useRefreshTokens: true,
  })

app.use(auth0)
app.use(router)

app.use(PrimeVue, {
    theme: {
        preset: indigoPreset,
        options: {
            darkModeSelector: 'none',
            cssLayer: {
                name: 'primevue',
                order: 'tailwind-base, primevue, tailwind-utilities'
            }
        }
    }
});

app.mount('#app')
