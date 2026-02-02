<script setup>
import { ref, onMounted, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import TheNavbar from '@/components/TheNavbar.vue'
import { useRoute, useRouter } from 'vue-router'

const {
  isAuthenticated,
  loginWithRedirect,
  isLoading,
  user,
  logout: auth0Logout,
} = useAuth0()

const router = useRouter()
const route = useRoute()

const serviceIcons = [
  { name: 'Netflix', color: 'bg-red-600', left: '10%', delay: '0s', size: 'w-12 h-12' },
  { name: 'Spotify', color: 'bg-green-500', left: '25%', delay: '2s', size: 'w-10 h-10' },
  { name: 'YouTube', color: 'bg-red-500', left: '80%', delay: '4s', size: 'w-14 h-14' },
  { name: 'Apple', color: 'bg-gray-800', left: '65%', delay: '1s', size: 'w-8 h-8' },
  { name: 'Amazon', color: 'bg-orange-400', left: '45%', delay: '3s', size: 'w-12 h-12' },
  { name: 'Disney+', color: 'bg-blue-900', left: '90%', delay: '5s', size: 'w-10 h-10' },
]

const infoMessage = ref('') 
const isSuccessMessage = ref(false)

watch([isAuthenticated, isLoading], ([isAuth, loading]) => {
  if (!loading && isAuth) {
    const isSocial = user.value?.sub?.includes('google-oauth2') || false
    
    if (!isSocial && !user.value?.email_verified) {
      router.push('/verification-required')
    }
  }
}, { immediate: true })

onMounted(() => {
  const urlParams = new URLSearchParams(window.location.search)
  const errorDesc = urlParams.get('error_description')
  const error = urlParams.get('error')

  if (errorDesc) {

    if (errorDesc.includes('verify your email')) {
      infoMessage.value = 'Check your email: we have sent you a confirmation link.'
      isSuccessMessage.value = false
    } 

    else if (error === 'access_denied' && errorDesc.includes('linked')) {
      infoMessage.value = errorDesc 
      isSuccessMessage.value = true
    }
  }
})

const handleLogin = async () => {
  await loginWithRedirect()
}

const handleLogout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}
</script>

<template>
  
  <div class="relative min-h-screen bg-slate-50 overflow-x-hidden">
    <div class="absolute -top-24 -left-24 w-96 h-96 bg-purple-500/20 rounded-full blur-3xl"></div>
    <div class="absolute -bottom-24 -right-24 w-96 h-96 bg-indigo-500/20 rounded-full blur-3xl"></div>
    <div v-if="!isAuthenticated && !isLoading" class="absolute inset-0 pointer-events-none">
       <div v-for="(icon, index) in serviceIcons" :key="index" 
            class="absolute animate-float opacity-10 rounded-full flex items-center justify-center text-white text-[10px] font-bold shadow-lg"
            :class="[icon.color, icon.size]" :style="{ left: icon.left, animationDelay: icon.delay, top: '-64px' }">
        {{ icon.name }}
      </div>
    </div>

    <div v-if="isLoading" class="min-h-screen flex items-center justify-center">
      <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600"></div>
    </div>

    <div v-else-if="!isAuthenticated" class="relative">
      <nav class="flex items-center justify-between px-8 py-6 max-w-7xl mx-auto">
        <div class="flex items-center gap-2">
          <div class="bg-indigo-600 p-2 rounded-xl text-white shadow-lg shadow-indigo-200">
            <i class="pi pi-dollar"></i>
          </div>
          <span class="text-xl font-black text-slate-900 tracking-tight">RenuMate</span>
        </div>
        <button @click="handleLogin" class="text-sm font-bold text-slate-900 hover:text-indigo-600 transition-colors cursor-pointer">
          Sign In
        </button>
      </nav>

      <section class="max-w-7xl mx-auto px-6 pt-16 pb-24 text-center bg-white/10 backdrop-blur-2xl border border-white/30 rounded-[3rem] shadow-[0_20px_50px_rgba(0,0,0,0.1)] relative overflow-hidden">
        <h1 class="text-5xl md:text-7xl font-black text-slate-900 mb-6 tracking-tighter">
          Don't let subscriptions <br/> 
          <span class="text-transparent bg-clip-text bg-gradient-to-r from-indigo-600 to-purple-600">drain your wallet.</span>
        </h1>
        <p class="text-lg text-slate-600 max-w-2xl mx-auto mb-10 leading-relaxed">
          The ultimate dashboard to track your recurring payments, analyze spending, and get notified before every renewal.
        </p>
        
        <div class="flex flex-col items-center gap-4">
          <button @click="handleLogin" class="px-10 py-5 bg-slate-900 relative z-100 text-white rounded-2xl font-black text-lg hover:bg-indigo-600 hover:shadow-2xl hover:shadow-indigo-200 transition-all active:scale-95 cursor-pointer">
            Start Tracking for Free
          </button>
          
          <div v-if="infoMessage" class="mt-4 p-4 rounded-2xl border bg-white shadow-sm max-w-sm" :class="isSuccessMessage ? 'border-green-100' : 'border-amber-100'">
             <p class="text-xs font-bold" :class="isSuccessMessage ? 'text-green-700' : 'text-amber-700'">{{ infoMessage }}</p>
          </div>
        </div>
      </section>

      <section class="relative max-w-7xl mx-auto px-10 py-24 border-t border-slate-200/30">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-12">
          <div class="space-y-4 p-6 rounded-2xl bg-purple-600/60 backdrop-blur-xl border border-white/30 rounded-[3rem] shadow-2xl ring-1 ring-white/10">
            <div class="w-12 h-12 bg-white/20 backdrop-blur-md rounded-xl shadow-inner border border-white/10 flex items-center justify-center text-white font-bold">01</div>
            <h3 class="text-xl font-black text-white">Visual Timeline</h3>
            <p class="text-purple-100 text-sm leading-relaxed text-pretty">
              <span class="bg-purple-600 px-1 rounded-sm font-bold drop-shadow-[0_0_10px_rgba(192,132,252,0.8)]">See exactly how many days remain</span> until your next charge with a beautiful, color-coded interface.
            </p>
          </div>
          
          <div class="space-y-4 p-6 rounded-2xl bg-purple-600/60 backdrop-blur-xl border border-white/30 rounded-[3rem] shadow-2xl ring-1 ring-white/10">
            <div class="w-12 h-12 bg-white/20 backdrop-blur-md rounded-xl shadow-inner border border-white/10 flex items-center justify-center text-white font-bold">02</div>
            <h3 class="text-xl font-black text-white">Smart Analytics</h3>
            <p class="text-purple-100 text-sm leading-relaxed text-pretty"><span class="bg-purple-600 px-1 rounded-sm font-bold drop-shadow-[0_0_10px_rgba(192,132,252,0.8)]">Analyze your expenses in any currency.</span> Switch periods to see your daily, monthly, or yearly burn rate.</p>
          </div>

          <div class="space-y-4 p-6 rounded-2xl bg-purple-600/60 backdrop-blur-xl border border-white/30 rounded-[3rem] shadow-2xl ring-1 ring-white/10">
            <div class="w-12 h-12 bg-white/20 backdrop-blur-md rounded-xl shadow-inner border border-white/10 flex items-center justify-center text-white font-bold">03</div>
            <h3 class="text-xl font-black text-white">Email Alerts</h3>
            <p class="text-purple-100 text-sm leading-relaxed text-pretty"><span class="bg-purple-600 px-1 rounded-sm font-bold drop-shadow-[0_0_10px_rgba(192,132,252,0.8)]">Set custom reminders at specific times.</span> We'll mail you before the renewal so you have time to cancel.</p>
          </div>
        </div>
      </section>
    </div>

    <div v-else class="relative z-10 min-h-screen flex flex-col">
      <TheNavbar v-if="!route.meta.hideNavbar" :user-name="user?.name" @logout="handleLogout" />
      <main class="flex-1 container mx-auto py-6 px-4">
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
