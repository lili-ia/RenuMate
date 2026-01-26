<script setup>
import { ref, onMounted, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import TheNavbar from '@/components/TheNavbar.vue'
import IconDollar from '@/components/icons/IconDollar.vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue3-toastify'

const {
  isAuthenticated,
  loginWithRedirect,
  isLoading,
  user,
  logout: auth0Logout,
  error: auth0Error,
} = useAuth0()

const route = useRoute()

const serviceIcons = [
  { name: 'Netflix', color: 'bg-red-600', left: '10%', delay: '0s', size: 'w-12 h-12' },
  { name: 'Spotify', color: 'bg-green-500', left: '25%', delay: '2s', size: 'w-10 h-10' },
  { name: 'YouTube', color: 'bg-red-500', left: '80%', delay: '4s', size: 'w-14 h-14' },
  { name: 'Apple', color: 'bg-gray-800', left: '65%', delay: '1s', size: 'w-8 h-8' },
  { name: 'Amazon', color: 'bg-orange-400', left: '45%', delay: '3s', size: 'w-12 h-12' },
  { name: 'Disney+', color: 'bg-blue-900', left: '90%', delay: '5s', size: 'w-10 h-10' },
]

const verificationErrorMessage = ref(null)

watch(auth0Error, (newError) => {
  if (newError) {
    console.error('Auth0 Error:', newError)
    if (newError.message.includes('verify your email')) {
      verificationErrorMessage.value = 'Please confirm your email before logging in.'
    } else {
      toast.error(newError.message)
    }
  }
})

onMounted(() => {
  const urlParams = new URLSearchParams(window.location.search)
  const errorDesc = urlParams.get('error_description')
  if (errorDesc && errorDesc.includes('verify your email')) {
    verificationErrorMessage.value = 'Check your email: we have sent you a confirmation link.'
  }
})

const handleLogin = () => {
  verificationErrorMessage.value = null
  loginWithRedirect()
}

const handleLogout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}
</script>

<template>
  <div class="relative min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 overflow-hidden">
    <div v-if="!isAuthenticated && !isLoading" class="absolute inset-0 pointer-events-none">
      <div
        v-for="(icon, index) in serviceIcons"
        :key="index"
        class="absolute animate-float opacity-20 rounded-full flex items-center justify-center text-white text-[10px] font-bold shadow-lg"
        :class="[icon.color, icon.size]"
        :style="{
          left: icon.left,
          animationDelay: icon.delay,
          top: '-64px',
        }"
      >
        {{ icon.name }}
      </div>
    </div>

    <div v-if="isLoading" class="min-h-screen flex items-center justify-center p-4">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600"></div>
    </div>

    <div
      v-else-if="!isAuthenticated"
      class="relative z-10 min-h-screen flex items-center justify-center p-4"
    >
      <div
        class="bg-white/80 backdrop-blur-sm rounded-lg shadow-xl p-8 max-w-md w-full text-center border border-white"
      >
        <div class="relative w-20 h-20 mx-auto mb-6">
          <div class="absolute inset-0 bg-indigo-200 rounded-2xl rotate-12 animate-pulse"></div>
          <div class="relative bg-indigo-600 rounded-2xl p-4 text-white shadow-lg">
            <IconDollar />
          </div>
        </div>
        <h1 class="text-4xl font-black text-gray-900 mb-2 tracking-tight">RenuMate</h1>

        <div
          v-if="verificationErrorMessage"
          class="mb-6 p-4 bg-amber-50 border border-amber-200 rounded-lg animate-bounce-subtle"
        >
          <p class="text-amber-700 text-sm font-medium">
            {{ verificationErrorMessage }}
          </p>
        </div>

        <p v-else class="text-gray-600 mb-6">Track and manage your subscriptions with ease</p>

        <button
          @click="handleLogin"
          class="group relative w-full bg-gray-900 text-white py-4 rounded-2xl transition-all duration-300 hover:bg-indigo-600 hover:shadow-indigo-200 hover:shadow-2xl active:scale-95 font-bold overflow-hidden cursor-pointer"
        >
          <span class="relative z-10">
            {{ verificationErrorMessage ? 'Try Login Again' : 'Login to Continue' }}
          </span>
        </button>
      </div>
    </div>

    <div v-else class="relative z-10">
      <TheNavbar v-if="!route.meta.hideNavbar" :user-name="user?.name" @logout="handleLogout" />
      <main class="container mx-auto py-6">
        <RouterView />
      </main>
    </div>
  </div>
</template>
<style>
@keyframes float {
  0% {
    transform: translateY(0) rotate(0deg);
    opacity: 0;
  }
  10% {
    opacity: 0.3;
  }
  90% {
    opacity: 0.3;
  }
  100% {
    transform: translateY(110vh) rotate(360deg);
    opacity: 0;
  }
}

@keyframes bounce-subtle {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-5px);
  }
}

.animate-float {
  animation: float 15s linear infinite;
}

.animate-bounce-subtle {
  animation: bounce-subtle 2s ease-in-out infinite;
}
</style>
