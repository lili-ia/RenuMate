<script setup>
import { ref, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import { useUsers } from '@/composables/useUsers'
import { formatDateTime } from '@/utils/formatters'

const { isAuthenticated, loginWithRedirect } = useAuth0()
const { fetchUserInfo, user, handleDeactivate } = useUsers()

const showDeleteModal = ref(false)

watch(
    isAuthenticated,
    async (isAuth) => {
      if (isAuth) {
        try {
          await fetchUserInfo()
        } catch (error) {
          if (error?.message?.includes('Missing Refresh Token')) {
            loginWithRedirect()
          }
        }
      }
    },
    { immediate: true },
)
</script>

<template>
  <div
    class="max-w-4xl mx-auto p-6 space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700"
  >
    <div
      v-if="user"
      class="relative overflow-hidden bg-white rounded-[2.5rem] p-8 shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-slate-50"
    >
      <div
        class="absolute top-0 right-0 w-64 h-64 -mr-32 -mt-32 rounded-full bg-indigo-50/50 blur-3xl"
      ></div>

      <div class="relative z-10">
        <div class="flex flex-col md:flex-row items-center gap-8 mb-10">
          <div class="relative group">
            <div
              class="h-28 w-28 rounded-3xl bg-gradient-to-br from-indigo-500 to-purple-600 p-1 shadow-xl shadow-indigo-100 transition-transform duration-500 group-hover:rotate-3"
            >
              <div
                class="h-full w-full rounded-[1.25rem] bg-white flex items-center justify-center text-indigo-600 text-4xl font-black"
              >
                {{ user.name.charAt(0).toUpperCase() }}
              </div>
            </div>
          </div>

          <div class="text-center md:text-left">
            <h1 class="text-4xl font-black text-slate-900 tracking-tight break-all">{{ user.name }}</h1>
            <p
              class="text-slate-400 font-bold text-sm uppercase tracking-widest mt-2 flex items-center justify-center md:justify-start gap-2"
            >
              <i class="pi pi-calendar text-slate-400" style="font-size: 1rem"></i>
              Since {{ formatDateTime(user.memberSince, true) }}
            </p>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div
            class="group p-6 bg-slate-50 rounded-[2rem] border border-transparent hover:border-indigo-100 hover:bg-white hover:shadow-xl hover:shadow-indigo-50 transition-all duration-300"
          >
            <span
              class="block text-slate-400 uppercase text-[10px] font-black tracking-[0.2em] mb-2"
              >Email Address</span
            >
            <span class="text-slate-700 font-bold text-lg break-all">{{ user.email }}</span>
          </div>

          <div
            class="group p-6 bg-slate-50 rounded-[2rem] border border-transparent hover:border-indigo-100 hover:bg-white hover:shadow-xl hover:shadow-indigo-50 transition-all duration-300"
          >
            <div class="flex justify-between items-start">
              <div>
                <span
                  class="block text-slate-400 uppercase text-[10px] font-black tracking-[0.2em] mb-2"
                  >My Subscriptions</span
                >
                <span class="text-indigo-600 font-black text-3xl">{{
                  user.subscriptionCount
                }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-else class="text-center p-20 bg-white rounded-[2.5rem] shadow-sm border border-slate-50">
      <div class="relative w-12 h-12 mx-auto mb-4">
        <div class="absolute inset-0 border-4 border-indigo-100 rounded-full"></div>
        <div
          class="absolute inset-0 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"
        ></div>
      </div>
      <p class="text-slate-400 font-bold uppercase tracking-widest text-xs">
        Synchronizing profile...
      </p>
    </div>

    <div
      class="bg-rose-50/50 border border-rose-100 rounded-[2.5rem] p-8 transition-all hover:bg-rose-50"
    >
      <div class="flex flex-col md:flex-row items-center gap-6">
        <div
          class="h-16 w-16 bg-white rounded-2xl flex items-center justify-center text-rose-500 shadow-sm border border-rose-100 flex-shrink-0"
        >
          <i class="pi pi-trash text-red-500" style="font-size: 2rem"></i>
        </div>
        <div class="text-center md:text-left flex-1">
          <h2 class="text-xl font-black text-rose-900 tracking-tight">Account Safety</h2>
          <p class="text-rose-600/70 text-sm mt-1 font-medium leading-relaxed">
            Deactivating your account will pause all reminders. You'll have 30 days to recover your
            data before it's permanently wiped.
          </p>
        </div>
        <button
          @click="showDeleteModal = true"
          class="w-full md:w-auto px-8 py-4 bg-white text-rose-600 border border-rose-200 rounded-2xl hover:bg-rose-600 hover:text-white hover:border-rose-600 transition-all duration-300 font-black text-sm active:scale-95 shadow-sm shadow-rose-100 cursor-pointer"
        >
          Deactivate
        </button>
      </div>
    </div>

    <Transition name="pop">
      <div
        v-if="showDeleteModal"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4"
      >
        <div
          class="absolute inset-0 bg-slate-900/40 backdrop-blur-md transition-opacity"
          @click="showDeleteModal = false"
        ></div>

        <div
          class="relative bg-white rounded-[2rem] max-w-md w-full p-8 shadow-2xl border border-slate-100"
        >
          <div
            class="w-20 h-20 bg-rose-50 text-rose-500 rounded-3xl flex items-center justify-center mx-auto mb-6"
          >
            <i class="pi pi-exclamation-triangle text-amber-500" style="font-size: 2.5rem"></i>
          </div>

          <h3 class="text-2xl font-black text-slate-900 text-center tracking-tight mb-2">
            Are you sure?
          </h3>
          <p class="text-slate-500 text-center font-medium mb-8 leading-relaxed">
            This action will disable all your subscription tracking. You can reverse this within
            <span class="text-rose-500 font-bold">30 days</span> by logging back in.
          </p>

          <div class="grid grid-cols-2 gap-4">
            <button
              @click="showDeleteModal = false"
              class="px-6 py-4 bg-slate-50 text-slate-600 rounded-2xl font-bold hover:bg-slate-100 transition-all cursor-pointer"
            >
              Cancel
            </button>
            <button
              @click="handleDeactivate"
              class="px-6 py-4 bg-rose-600 text-white rounded-2xl font-bold hover:bg-rose-700 shadow-lg shadow-rose-100 active:scale-95 transition-all cursor-pointer"
            >
              Confirm
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.pop-enter-active,
.pop-leave-active {
  transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}
.pop-enter-from,
.pop-leave-to {
  opacity: 0;
  transform: scale(0.9) translateY(20px);
}

.animate-in {
  animation-duration: 0.7s;
}
</style>


