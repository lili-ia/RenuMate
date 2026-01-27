<script setup>
import { ref, onUnmounted, onMounted } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import { useUsers } from '@/composables/useUsers'
import { toast } from 'vue3-toastify'
import IconDollar from '@/components/icons/IconDollar.vue'
import { useRouter } from 'vue-router'

const COOLDOWN_KEY = 'verification_cooldown_end';

const router = useRouter()

const { logout: auth0Logout, user, checkSession, getAccessTokenSilently } = useAuth0()
const { resendEmail } = useUsers()

const isResending = ref(false)
const cooldown = ref(0)
let timer = null

const startCooldown = () => {
  const endTime = Date.now() + 60000; 
  localStorage.setItem(COOLDOWN_KEY, endTime.toString());
  runTimer(60);
};

const runTimer = (seconds) => {
  cooldown.value = seconds;
  if (timer) clearInterval(timer);
  
  timer = setInterval(() => {
    cooldown.value--;
    if (cooldown.value <= 0) {
      clearInterval(timer);
      localStorage.removeItem(COOLDOWN_KEY);
    }
  }, 1000);
};

const handleResend = async () => {
  if (cooldown.value > 0 || isResending.value) return
  
  isResending.value = true
  try {
    await resendEmail()
    toast.success('New verification link sent!')
    startCooldown()
  } catch (err) {
    toast.error('Failed to send email. Try again later.')
  } finally {
    isResending.value = false
  }
}

let pollingTimer = null

const handleLogout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}

const verifyStatus = async () => {
  try {
    await checkSession()
    
    if (user.value?.email_verified) {
      clearInterval(pollingTimer)
      setTimeout(() => {
        router.push('/')
      }, 500)
    }
  } catch (e) {
    console.log('Still waiting for verification...')
  }
}

onMounted(() => {
  const savedEndTime = localStorage.getItem(COOLDOWN_KEY);
  if (savedEndTime) {
    const remainingMs = parseInt(savedEndTime) - Date.now();
    const remainingSeconds = Math.ceil(remainingMs / 1000);

    if (remainingSeconds > 0) {
      runTimer(remainingSeconds);
    } else {
      localStorage.removeItem(COOLDOWN_KEY);
    }
  }
  pollingTimer = setInterval(verifyStatus, 4000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  if (pollingTimer) clearInterval(pollingTimer)
})
</script>

<template>
  <div class="min-h-screen bg-slate-50 flex items-center justify-center p-4">
    <div class="max-w-md w-full bg-white rounded-3xl shadow-xl p-8 border border-slate-100 text-center">
      <div class="w-20 h-20 bg-amber-100 rounded-2xl flex items-center justify-center mx-auto mb-6">
        <IconDollar class="w-10 h-10 text-amber-600" />
      </div>

      <h1 class="text-2xl font-black text-slate-900 mb-2">Verify your email</h1>
      <p class="text-slate-500 mb-8">
        We've sent a confirmation link to <br>
        <span class="font-bold text-slate-700">{{ user?.email }}</span>. <br>
        Please check your inbox.
      </p>
      <div v-if="user?.email_verified" class="fixed inset-0 bg-white z-50 flex flex-col items-center justify-center">
        <div class="animate-bounce bg-green-100 p-4 rounded-full mb-4">
            <svg class="w-12 h-12 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
            </svg>
        </div>
            <h2 class="text-xl font-bold">Email Verified!</h2>
            <p class="text-gray-500">Redirecting to dashboard...</p>
        </div>

      <div class="space-y-4">
        <button
          @click="handleResend"
          :disabled="isResending || cooldown > 0"
          class="w-full py-4 rounded-2xl font-bold transition-all duration-300 flex items-center justify-center gap-2 shadow-lg"
          :class="[
            cooldown > 0 
            ? 'bg-slate-100 text-slate-400 cursor-not-allowed shadow-none' 
            : 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-indigo-100'
          ]"
        >
          <div v-if="isResending" class="animate-spin rounded-full h-5 w-5 border-2 border-white/30 border-t-white"></div>
          <span>
            {{ cooldown > 0 ? `Resend link in ${cooldown}s` : 'Resend Verification Email' }}
          </span>
        </button>

        <button 
          @click="handleLogout"
          class="w-full py-4 rounded-2xl font-bold text-slate-600 hover:bg-slate-50 transition-colors border border-transparent"
        >
          Logout and use another email
        </button>
      </div>

      <p class="mt-8 text-xs text-slate-400">
        Already verified? Try refreshing the page or logging in again.
      </p>
    </div>
  </div>
</template>