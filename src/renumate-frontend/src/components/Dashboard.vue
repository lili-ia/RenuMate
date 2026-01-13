<script setup>
import { watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import api, { setAuthToken } from '@/api.ts'
import SubscriptionModal from '@/components/SubscriptionModal.vue'
import ReminderModal from '@/components/ReminderModal.vue'
import StatCard from '@/components/StatCard.vue'
import IconBell from '@/components/icons/IconBell.vue'
import IconCalendar from '@/components/icons/IconCalendar.vue'
import TheNavbar from '@/components/TheNavbar.vue'
import SubscriptionItem from '@/components/SubscriptionItem.vue'
import SubscriptionEmptyState from '@/components/SubscriptionEmptyState.vue'
import IconDollar from '@/components/icons/IconDollar.vue'
import { formatDateTime, localNotifyTime } from '@/utils/formatters.ts'
import { useSubscriptions } from '@/composables/useSubscriptions'
import { useReminders } from '@/composables/useReminders'
import { toast } from 'vue3-toastify'

const {
  subscriptions,
  resetForm,
  popularCurrencies,
  activeSubscriptionsCount,
  totalRemindersCount,
  loadSubscriptions,
  fetchSummary,
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
  openAddSubscriptionModal,
  selectedSubscription,
  formErrors,
  currentPage,
  totalPages,
  totalCount,
  pageSize,
  setPage,
  loading,
  summaryLoading,
  isSubmitting: isSubmittingSubscription,
} = useSubscriptions()

const {
  showReminderModal,
  reminderForm,
  openReminderModal,
  handleAddReminder,
  deleteReminder,
  closeReminderModal,
  reminderErrors,
  isSubmitting: isSubmittingReminder,
} = useReminders(loadSubscriptions, selectedSubscription, fetchSummary)

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
        await fetchSummary()
      } catch (error) {
        toast.error('Session error. Please log in again.')
        if (error.error === 'login_required') {
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
    if (response.data.message.includes('created') || response.data.message.includes('synced')) {
      await getAccessTokenSilently({ ignoreCache: true })
    }
  } catch (error) {
    console.error('Failed to sync user:', error)
    throw error
  }
}

const handleLogin = () => loginWithRedirect()

const logout = () => {
  auth0Logout({ logoutParams: { returnTo: window.location.origin } })
}

watch([selectedCurrency, selectedPeriod], fetchSummary)
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
    <div v-if="isLoading" class="min-h-screen flex items-center justify-center p-4">
      <div class="text-center">
        <div
          class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto mb-4"
        ></div>
        <h1 class="text-2xl font-bold text-indigo-600">Loading Session...</h1>
      </div>
    </div>

    <div v-else-if="!isAuthenticated" class="min-h-screen flex items-center justify-center p-4">
      <div class="bg-white rounded-lg shadow-xl p-8 max-w-md w-full text-center">
        <div class="w-16 h-16 mx-auto mb-4 text-indigo-600">
          <IconDollar />
        </div>
        <h1 class="text-3xl font-bold text-gray-800 mb-2">RenuMate</h1>
        <p class="text-gray-600 mb-6">Track and manage your subscriptions with ease</p>
        <button
          @click="handleLogin"
          class="w-full bg-indigo-600 text-white py-3 rounded-lg hover:bg-indigo-700 transition font-semibold shadow-lg"
        >
          Login to Continue
        </button>
      </div>
    </div>

    <div v-else>
      <TheNavbar :user-name="user?.name" @logout="logout" />

      <div class="max-w-7xl mx-auto p-6">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <StatCard :loading="summaryLoading">
            <template #label><p class="text-gray-600 text-sm">Active Subscriptions</p></template>
            <template #value
              ><p class="text-3xl font-bold text-gray-800">
                {{ activeSubscriptionsCount }}
              </p></template
            >
            <template #right><IconBell /></template>
          </StatCard>

          <StatCard :loading="summaryLoading">
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
                class="block w-24 p-2 text-sm text-gray-700 bg-gray-50 rounded-lg border border-gray-300"
              >
                <option v-for="curr in popularCurrencies" :key="curr.code" :value="curr.code">
                  {{ curr.code }} ({{ curr.symbol }})
                </option>
              </select>
            </template>
          </StatCard>

          <StatCard :loading="summaryLoading">
            <template #label><p class="text-gray-600 text-sm">Total Reminders</p></template>
            <template #value
              ><p class="text-3xl font-bold text-gray-800">{{ totalRemindersCount }}</p></template
            >
            <template #right><IconCalendar /></template>
          </StatCard>
        </div>

        <div class="bg-white rounded-lg shadow-sm border border-gray-100 overflow-hidden">
          <div class="p-6 border-b flex justify-between items-center bg-gray-50/50">
            <h2 class="text-xl font-bold text-gray-800">My Subscriptions</h2>
            <button
              @click="openAddSubscriptionModal"
              class="flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition shadow-sm"
            >
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Add New
            </button>
          </div>

          <div class="p-6">
            <div v-if="loading" class="space-y-4">
              <div v-for="i in 3" :key="i" class="h-20 bg-gray-200 animate-pulse rounded-lg"></div>
            </div>

            <SubscriptionEmptyState v-else-if="subscriptions.length === 0" />

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

              <div
                v-if="totalPages > 1"
                class="mt-8 pt-6 border-t border-gray-100 flex items-center justify-between"
              >
                <div class="flex flex-1 justify-between sm:hidden">
                  <button
                    @click="setPage(currentPage - 1)"
                    :disabled="currentPage === 1"
                    class="px-4 py-2 border rounded-md disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <button
                    @click="setPage(currentPage + 1)"
                    :disabled="currentPage === totalPages"
                    class="px-4 py-2 border rounded-md disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>

                <div class="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                  <p class="text-sm text-gray-600">
                    Showing
                    <span class="font-semibold">{{ (currentPage - 1) * pageSize + 1 }}</span> to
                    <span class="font-semibold">{{
                      Math.min(currentPage * pageSize, totalCount)
                    }}</span>
                    of
                    <span class="font-semibold">{{ totalCount }}</span>
                  </p>

                  <nav class="isolate inline-flex -space-x-px rounded-md shadow-sm">
                    <button
                      v-for="p in totalPages"
                      :key="p"
                      @click="setPage(p)"
                      :class="[
                        'relative inline-flex items-center px-4 py-2 text-sm font-semibold transition-all',
                        currentPage === p
                          ? 'z-10 bg-indigo-600 text-white focus:z-20'
                          : 'text-gray-900 ring-1 ring-inset ring-gray-200 hover:bg-gray-50',
                      ]"
                    >
                      {{ p }}
                    </button>
                  </nav>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <SubscriptionModal
        :is-open="showSubscriptionModal"
        :is-editing="!!editingSubscription"
        :is-submitting="isSubmittingSubscription"
        :model-value="formData"
        :next-renewal-date="calculatedNextRenewalDate"
        :errors="formErrors"
        @close="resetForm"
        @save="handleSubmit"
      />

      <ReminderModal
        :is-open="showReminderModal"
        :is-submitting="isSubmittingReminder"
        :subscription="selectedSubscription"
        :model-value="reminderForm"
        :errors="reminderErrors"
        @close="closeReminderModal"
        @save="handleAddReminder"
      />
    </div>
  </div>
</template>
