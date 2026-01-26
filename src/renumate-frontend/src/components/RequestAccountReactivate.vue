<template>
  <div class="min-h-[80vh] flex items-center justify-center p-4">
    <div
      class="max-w-md w-full bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100"
    >
      <div
        :class="[
          isAlreadyActive ? 'bg-indigo-800' : isSent ? 'bg-green-500' : 'bg-indigo-600',
          'p-8 text-center transition-colors duration-500',
        ]"
      >
        <div
          class="inline-flex items-center justify-center w-16 h-16 bg-white/20 rounded-full mb-4"
        >
          <svg
            v-if="isAlreadyActive"
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8 text-white"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <svg
            v-else-if="!isSent"
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8 text-white"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
            />
          </svg>
          <svg
            v-else
            xmlns="http://www.w3.org/2000/svg"
            class="h-8 w-8 text-white"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-white">
          {{ isAlreadyActive ? 'Account Ready' : isSent ? 'Check Your Email' : 'Account Inactive' }}
        </h1>
      </div>

      <div class="p-8 text-center">
        <div v-if="isAlreadyActive">
          <p class="text-gray-600 mb-6">
            Your account is already active. You can now access your dashboard.
          </p>
          <button
            @click="onGoToDashboard"
            class="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 px-4 rounded-xl transition duration-200 shadow-lg flex items-center justify-center cursor-pointer"
          >
            Go to Dashboard
          </button>
        </div>

        <div v-else-if="isSent">
          <p class="text-gray-600 mb-2 font-semibold">Email has been sent!</p>
          <p class="text-gray-500 mb-8 text-sm">
            We've sent an email to your address. Please click the link in the message to restore
            your access.
          </p>
          <button
            @click="isSent = false"
            class="text-indigo-600 hover:text-indigo-800 text-sm font-medium mb-4 cursor-pointer"
          >
            Didn't get the email? Try again
          </button>
        </div>

        <div v-else>
          <p class="text-gray-600 mb-8">
            Your account is currently deactivated. Click the button below to receive a reactivation
            link via email.
          </p>
          <button
            @click="onReactivateRequest"
            :disabled="isProcessing"
            class="w-full bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-300 text-white font-bold py-3 px-4 rounded-xl transition duration-200 shadow-lg cursor-pointer flex items-center justify-center"
          >
            <span
              v-if="isProcessing"
              class="animate-spin mr-2 h-5 w-5 border-2 border-white border-t-transparent rounded-full"
            ></span>
            {{ isProcessing ? 'Sending Link...' : 'Send Reactivation Link' }}
          </button>
        </div>

        <button
          v-if="!isAlreadyActive"
          @click="onLogout"
          :disabled="isProcessing"
          class="w-full mt-4 bg-white border border-gray-200 text-gray-600 hover:bg-gray-50 disabled:opacity-50 font-semibold py-3 px-4 rounded-xl transition cursor-pointer"
        >
          Log Out
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useAuth0 } from '@auth0/auth0-vue'
import { useUsers } from '@/composables/useUsers'
const { onReactivateRequest, isProcessing, isAlreadyActive, isSent } = useUsers()

const { logout: auth0Logout, loginWithRedirect } = useAuth0()

const onGoToDashboard = async () => {
  await loginWithRedirect({
    appState: { target: '/' },
  })
}

const onLogout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}
</script>
