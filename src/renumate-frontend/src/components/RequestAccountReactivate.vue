<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue3-toastify'
import { useUsers } from '@/composables/useUsers'

const COOLDOWN_KEY = 'reactivation_cooldown_end';

const { onReactivateRequest, isProcessing, isAlreadyActive, isSent, getActiveStatus } = useUsers()
const { logout: auth0Logout, user, checkSession } = useAuth0()
const router = useRouter()

const cooldown = ref(0)
let timer = null
let polling = null

const startCooldown = () => {
  const endTime = Date.now() + 60000; 
  localStorage.setItem(COOLDOWN_KEY, endTime.toString());
  runTimer(60)
}

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

const handleReactivate = async () => {
  if (cooldown.value > 0 || isProcessing.value) return
  
  await onReactivateRequest()
  
  if (isSent.value) {
    startCooldown()
  }
}

const checkStatus = async () => {
  try {
    if (isAlreadyActive.value) {
      toast.success('Account is ready!')
      router.push('/')
      return
    }

    await checkSession()
    
    const isActive = await getActiveStatus()
    console.log('Account active status:', isActive)
    
    if (isActive) {
      clearInterval(polling)
      toast.success('Account activated!')
      setTimeout(() => router.push('/'), 1000)
    }
  } catch (e) {
    console.debug('Still waiting for activation...')
  }
}

const onLogout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}

watch(isAlreadyActive, (active) => {
  if (active) {
    clearInterval(polling)
    router.push('/')
  }
})

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
  polling = setInterval(checkStatus, 5000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  if (polling) clearInterval(polling)
})
</script>

<template>
  <div class="min-h-[90vh] flex items-center justify-center p-6 bg-gradient-to-br from-slate-50 to-indigo-50/50">
    <div class="max-w-md w-full relative">
      <div class="absolute -top-12 -left-12 w-24 h-24 bg-indigo-200 rounded-full blur-3xl opacity-50"></div>
      <div class="absolute -bottom-12 -right-12 w-32 h-32 bg-emerald-200 rounded-full blur-3xl opacity-50"></div>

      <div class="bg-white/80 backdrop-blur-xl rounded-[2.5rem] shadow-2xl shadow-indigo-100/50 border border-white p-8 relative z-10 overflow-hidden">
        
        <div class="text-center mb-8">
          <div class="relative w-24 h-24 mx-auto mb-6">
            <div :class="[
              isSent ? 'bg-emerald-500 shadow-emerald-200' : 'bg-indigo-600 shadow-indigo-200',
              'absolute inset-0 rounded-3xl rotate-12 transition-all duration-700'
            ]"></div>
            <div class="relative bg-white rounded-3xl w-full h-full flex items-center justify-center shadow-sm border border-slate-100">
              <Transition name="scale" mode="out-in">
                <svg v-if="isSent" key="sent" class="w-10 h-10 text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
                <svg v-else key="lock" class="w-10 h-10 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
              </Transition>
            </div>
          </div>
          
          <h1 class="text-3xl font-black text-slate-900 tracking-tight">
            {{ isSent ? 'Check Inbox' : 'Account Locked' }}
          </h1>
        </div>

        <div class="space-y-6 text-center">
          <p class="text-slate-500 leading-relaxed">
            <template v-if="isSent">
              We've sent a reactivation link to your email. Please click it to restore access to <strong>RenuMate</strong>.
            </template>
            <template v-else>
              Your account is currently inactive. Would you like to request a reactivation link?
            </template>
          </p>

          <div class="pt-4 space-y-3">
            <button
              @click="handleReactivate"
              :disabled="isProcessing || cooldown > 0"
              class="group w-full py-4 px-6 rounded-2xl font-bold text-lg transition-all duration-300 flex items-center justify-center gap-3 active:scale-95 shadow-xl disabled:active:scale-100 cursor-pointer" 
              :class="[
                cooldown > 0 
                ? 'bg-slate-100 text-slate-400 shadow-none cursor-not-allowed' 
                : 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-indigo-200'
              ]"
            >
              <div v-if="isProcessing" class="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
              <span>
                {{ cooldown > 0 ? `Retry in ${cooldown}s` : (isSent ? 'Resend Link' : 'Send Link Now') }}
              </span>
            </button>

            <button
              @click="onLogout"
              class="w-full py-4 text-slate-400 font-bold hover:text-slate-600 transition-colors rounded-2xl border border-transparent hover:bg-slate-50 cursor-pointer"
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scale-enter-active, .scale-leave-active {
  transition: all 0.4s ease;
}
.scale-enter-from, .scale-leave-to {
  opacity: 0;
  transform: scale(0.8) rotate(-10deg);
}
</style>