<template>
  <div class="min-h-[80vh] flex items-center justify-center p-4 text-center">
    <div class="max-w-md w-full">
      <div v-if="status === 'loading'">
        <div
          class="animate-spin inline-block w-12 h-12 border-4 border-indigo-600 border-t-transparent rounded-full mb-4"
        ></div>
        <h2 class="text-xl font-semibold text-gray-700">Verifying your token...</h2>
        <p class="text-gray-500">Please wait while we reactivate your account.</p>
      </div>

      <div
        v-if="status === 'success'"
        class="bg-white p-8 rounded-2xl shadow-xl border border-green-100"
      >
        <div
          class="w-16 h-16 bg-green-100 text-green-600 rounded-full flex items-center justify-center mx-auto mb-4"
        >
          <i class="pi pi-check-circle" style="font-size: 2.5rem"></i>
        </div>
        <h2 class="text-2xl font-bold text-gray-800 mb-2">Account Restored!</h2>
        <p class="text-gray-600 mb-6">
          Your account is now active. You will be logged out and redirected to the login page.
        </p>
        <div class="text-sm text-gray-400">Redirecting in {{ countdown }}s...</div>
      </div>

      <div
        v-if="status === 'error'"
        class="bg-white p-8 rounded-2xl shadow-xl border border-red-100"
      >
        <div
          class="w-16 h-16 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4"
        >
          <i class="pi pi-times-circle" style="font-size: 2.5rem"></i>
        </div>
        <h2 class="text-2xl font-bold text-gray-800 mb-2">Invalid Link</h2>
        <p class="text-gray-600 mb-6">This reactivation link is invalid or has expired.</p>
        <button
          @click="router.push({ name: 'reactivate-request' })"
          class="bg-indigo-600 text-white px-6 py-2 rounded-lg hover:bg-indigo-700 transition"
        >
          Request new link
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue3-toastify'
import { useAuth0 } from '@auth0/auth0-vue'
import {useUsers} from "@/composables/useUsers.js";

const route = useRoute()
const router = useRouter()

const {
  isAuthenticated,
  loginWithRedirect,
  isLoading,
  logout,
} = useAuth0()

const { status, countdown, confirmAccountActivation } = useUsers()

watch(
  [isAuthenticated, isLoading],
  async ([isAuth, loading]) => {
    if (loading) return

    if (!isAuth) {
      loginWithRedirect({ appState: { target: route.fullPath } })
      return
    }

    try {
      const activationToken = route.query.token

      if (!activationToken) {
        status.value = 'error'
        toast.error('Activation token is missing')
        return
      }

      const success = await confirmAccountActivation(activationToken)

      if (success) {
        const timer = setInterval(() => {
          countdown.value--
          if (countdown.value <= 0) {
            clearInterval(timer)
            logout()
          }
        }, 1000)
      }
    } catch (error){
      console.log(error)
    }
  },
  { immediate: true }
)
</script>
