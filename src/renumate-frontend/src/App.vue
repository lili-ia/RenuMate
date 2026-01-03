<script setup>
import { watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import api, { setAuthToken } from './api'
import SubscriptionModal from './components/SubscriptionModal.vue'
import ReminderModal from './components/ReminderModal.vue'
import StatCard from './components/StatCard.vue'
import IconBell from './components/icons/IconBell.vue'
import IconCalendar from './components/icons/IconCalendar.vue'
import TheNavbar from './components/TheNavbar.vue'
import SubscriptionItem from './components/SubscriptionItem.vue'
import SubscriptionEmptyState from './components/SubscriptionEmptyState.vue'
import IconDollar from './components/icons/IconDollar.vue'
import { formatDateTime, localNotifyTime } from './utils/formatters.ts'
import { useSubscriptions } from '@/composables/useSubscriptions'
import { useReminders } from '@/composables/useReminders'

const {
  subscriptions,
  resetForm,
  popularCurrencies,
  activeSubscriptionsCount,
  loadSubscriptions,
  fetchTotal,
  selectedCurrency,
  selectedPeriod,
  totalCost,
  formData,
  calculatedNextRenewalDate,
  editingSubscription,
  showSubscriptionModal,
  editSubscription,
  deleteSubscription,
  toggleActive,
  handleSubmit,
  totalReminders,
  openAddSubscriptionModal,
  selectedSubscription,
} = useSubscriptions()

const {
  showReminderModal,
  reminderForm,
  openReminderModal,
  handleAddReminder,
  deleteReminder,
  closeReminderModal,
} = useReminders(loadSubscriptions, selectedSubscription)

const {
  isAuthenticated,
  loginWithRedirect,
  isLoading,
  getAccessTokenSilently,
  user,
  logout: auth0Logout,
} = useAuth0()

watch(
  isAuthenticated,
  async (isAuth) => {
    if (isAuth) {
      try {
        const accessToken = await getAccessTokenSilently()
        setAuthToken(accessToken)
        await syncUserWithDatabase()
        await loadSubscriptions()
        await fetchTotal()
        console.log('Authentication complete, token set, and data loaded.')
      } catch (error) {
        console.error('Error in post-login process:', error)
        if (error.error === 'unauthorized') {
          console.log('Session expired, redirecting to login.')
          loginWithRedirect()
        }
      }
    }
  },
  { immediate: true },
)

const syncUserWithDatabase = async () => {
  try {
    const response = await api.post('/users/sync-user', {})
    console.log('User synced with backend database:', response.data)

    if (response.data.message.includes('created') || response.data.message.includes('synced')) {
      await getAccessTokenSilently({
        ignoreCache: true,
      })

      console.log('Token refreshed with new internal ID!')
    }
  } catch (error) {
    console.error('Failed to sync user with database:', error)
    throw error
  }
}

const handleLogin = async () => {
  loginWithRedirect()
}

const logout = () => {
  localStorage.removeItem('accessToken')
  auth0Logout({
    logoutParams: {
      returnTo: window.location.origin,
    },
  })
}

watch([selectedCurrency, selectedPeriod], fetchTotal)
</script>

<template>
  <div>
    <div
      v-if="isLoading"
      class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4"
    >
      <div class="text-center">
        <h1 class="text-2xl font-bold text-indigo-600">Loading Session...</h1>
        <p class="text-gray-500 mt-2">Checking authentication status...</p>
      </div>
    </div>
    <div
      v-if="!isAuthenticated"
      class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4"
    >
      <div class="bg-white rounded-lg shadow-xl p-8 max-w-md w-full">
        <div class="text-center mb-6">
          <div class="w-16 h-16 mx-auto mb-4 text-indigo-600">
            <IconDollar />
          </div>
          <h1 class="text-3xl font-bold text-gray-800 mb-2">Subscription Manager</h1>
          <p class="text-gray-600">Track and manage your subscriptions with ease</p>
        </div>
        <button
          @click="handleLogin"
          class="w-full bg-indigo-600 text-white py-3 rounded-lg hover:bg-indigo-700 transition font-semibold"
        >
          Login with Auth0
        </button>
      </div>
    </div>

    <div v-else class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      <TheNavbar :user-name="user?.name" @logout="logout" />

      <div class="max-w-7xl mx-auto p-6">
        <div class="bg-white rounded-lg shadow mb-6"></div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <StatCard>
            <template #label>
              <p class="text-gray-600 text-sm">Active Subscriptions</p>
            </template>
            <template #value>
              <p class="text-3xl font-bold text-gray-800">{{ activeSubscriptionsCount }}</p>
            </template>
            <template #right>
              <IconBell />
            </template>
          </StatCard>

          <StatCard>
            <template #label>
              <select
                v-model="selectedPeriod"
                class="text-gray-600 text-sm bg-transparent border-none focus:ring-0 cursor-pointer p-0 capitalize"
              >
                <option value="daily">Daily Cost</option>
                <option value="monthly">Monthly Cost</option>
                <option value="yearly">Yearly Cost</option>
              </select>
            </template>
            <template #value>
              <p class="text-3xl font-bold text-gray-800">
                {{ totalCost.toFixed(2) }} {{ selectedCurrency }}
              </p>
            </template>
            <template #right>
              <select
                v-model="selectedCurrency"
                class="block w-24 p-2 text-sm text-gray-700 bg-gray-50 rounded-lg border border-gray-300 focus:ring-green-500 focus:border-green-500"
              >
                <option v-for="curr in popularCurrencies" :key="curr.code" :value="curr.code">
                  {{ curr.code }} ({{ curr.symbol }})
                </option>
              </select>
            </template>
            <template #footer>
              <div class="mt-4 text-xs text-gray-400">Rates updated every 24 hours</div>
            </template>
          </StatCard>

          <StatCard>
            <template #label>
              <p class="text-gray-600 text-sm">Total Reminders</p>
            </template>
            <template #value>
              <p class="text-3xl font-bold text-gray-800">{{ totalReminders }}</p>
            </template>
            <template #right>
              <IconCalendar />
            </template>
          </StatCard>
        </div>

        <div class="bg-white rounded-lg shadow mb-6">
          <div class="p-6 border-b flex justify-between items-center">
            <h2 class="text-xl font-bold text-gray-800">My Subscriptions</h2>
            <button
              @click="openAddSubscriptionModal"
              class="flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition"
            >
              <svg
                class="w-4 h-4"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Add Subscription
            </button>
          </div>

          <div class="p-6">
            <SubscriptionEmptyState v-if="subscriptions.length === 0" />

            <div v-else class="space-y-4">
              <SubscriptionItem
                v-for="sub in subscriptions"
                :key="sub.id"
                :sub="sub"
                :format-date="formatDateTime"
                :format-time="localNotifyTime"
                @edit="editSubscription"
                @delete="deleteSubscription"
                @toggle-mute="toggleActive"
                @add-reminder="openReminderModal"
                @delete-reminder="deleteReminder"
              />
            </div>
          </div>
        </div>
      </div>

      <SubscriptionModal
        :is-open="showSubscriptionModal"
        :is-editing="!!editingSubscription"
        :model-value="formData"
        :next-renewal-date="calculatedNextRenewalDate"
        @close="resetForm"
        @save="handleSubmit"
      />

      <ReminderModal
        :is-open="showReminderModal"
        :subscription="selectedSubscription"
        :model-value="reminderForm"
        @close="closeReminderModal"
        @save="handleAddReminder"
      />
    </div>
  </div>
</template>
const $toast = useToast();
