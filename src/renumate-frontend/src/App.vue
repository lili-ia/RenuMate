<script setup>
import { ref, computed, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import api, { setAuthToken } from './api'

const {
  isAuthenticated,
  loginWithRedirect,
  isLoading,
  getAccessTokenSilently,
  user,
  logout: auth0Logout,
} = useAuth0()

const subscriptions = ref([])
const showModal = ref(false)
const editingSubscription = ref(null)
const showReminderModal = ref(false)
const selectedSubscription = ref(null)

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

const formData = ref({
  name: '',
  cost: '',
  currency: 'UAH',
  plan: 'Monthly',
  customPeriodInDays: null,
  startDate: '',
  notes: '',
  isMuted: false,
})

const reminderForm = ref({
  daysBeforeRenewal: '',
  notifyTime: '09:00',
})

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

const activeSubscriptionsCount = computed(() => {
  return subscriptions.value.filter((s) => !s.isMuted).length
})

const localNotifyTime = (utcTime) => {
  const date = new Date('January 06, 1990 ' + utcTime + ' GMT+00:00')
  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

const totalReminders = computed(() => {
  return subscriptions.value.reduce((sum, s) => sum + (s.reminders?.length || 0), 0)
})

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

const totalCost = ref(0)
const selectedCurrency = ref('USD')
const selectedPeriod = ref('monthly')

const popularCurrencies = [
  { code: 'USD', name: 'US Dollar', symbol: '$' },
  { code: 'EUR', name: 'Euro', symbol: '€' },
  { code: 'UAH', name: 'Hryvnia', symbol: '₴' },
  { code: 'PLN', name: 'Złoty', symbol: 'zł' },
  { code: 'GBP', name: 'Pound', symbol: '£' },
  { code: 'JPY', name: 'Yen', symbol: '¥' },
]

const fetchTotal = async () => {
  try {
    const response = await api.get('/subscriptions/total', {
      params: {
        currency: selectedCurrency.value,
        period: selectedPeriod.value,
      },
    })
    totalCost.value = response.data
  } catch (error) {
    console.error('Failed to fetch analytics', error)
  }
}

watch([selectedCurrency, selectedPeriod], fetchTotal)

const formatDateTime = (isoString) => {
  if (!isoString) return ''

  const date = new Date(isoString)

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
    .format(date)
    .replace(',', '')
}

const loadSubscriptions = async () => {
  try {
    const response = await api.get('/subscriptions')
    subscriptions.value = response.data.items

    console.log('Subscriptions loaded successfully.')
  } catch (error) {
    console.error('Error loading subscriptions:', error)
    subscriptions.value = []
  }
}

const openAddModal = () => {
  showModal.value = true
}

const editSubscription = (sub) => {
  const baseData = { ...sub }

  formData.value = {
    ...baseData,
    startDate: formatDate(baseData.startDate),
  }

  editingSubscription.value = sub
  showModal.value = true
}

const calculatedNextRenewalDate = computed(() => {
  if (!formData.value.startDate || !formData.value.plan) return ''

  const start = new Date(formData.value.startDate)
  let nextDate = new Date(start)
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  const plan = formData.value.plan.toLowerCase()

  const addPeriod = (date) => {
    const d = new Date(date)
    if (plan === 'weekly') d.setDate(d.getDate() + 7)
    else if (plan === 'monthly') d.setMonth(d.getMonth() + 1)
    else if (plan === 'quarterly') d.setMonth(d.getMonth() + 3)
    else if (plan === 'annual') d.setFullYear(d.getFullYear() + 1)
    else if (plan === 'custom' && formData.value.customPeriodInDays) {
      d.setDate(d.getDate() + parseInt(formData.value.customPeriodInDays))
    }
    return d
  }

  while (nextDate <= today) {
    const prevTime = nextDate.getTime()
    nextDate = addPeriod(nextDate)

    if (nextDate.getTime() <= prevTime) break
  }

  return nextDate.toISOString().split('T')[0]
})

const handleSubmit = async () => {
  if (!formData.value.name || !formData.value.cost) {
    alert('Please fill in required fields')
    return
  }

  if (formData.value.plan === 'Custom' && !formData.value.customPeriodInDays) {
    alert('Please specify the custom period in days')
    return
  }

  const payload = {
    ...formData.value,
    customPeriodInDays:
      formData.value.plan === 'Custom' ? parseInt(formData.value.customPeriodInDays) : null,
  }

  try {
    if (editingSubscription.value) {
      const subId = editingSubscription.value.id
      await api.put(`/subscriptions/${subId}`, payload)
      console.log(`Subscription ${subId} updated successfully on API.`)
    } else {
      await api.post('/subscriptions', payload)
      console.log('Subscription created successfully on API.')
    }

    await loadSubscriptions()
  } catch (error) {
    if (error.response) {
      const { status, data } = error.response
      if (status === 403) {
        alert('Similar subscription already exists.')
      }
      console.log(data)
    }
    alert('Failed to save subscription. Check the console.')
    return
  }

  resetForm()
}

const deleteSubscription = async (id) => {
  if (!confirm('Are you sure you want to delete this subscription?')) {
    return
  }

  try {
    await api.delete(`/subscriptions/${id}`)
    console.log(`Subscription ${id} deleted successfully from API. Reloading data...`)
    await loadSubscriptions()
  } catch (error) {
    console.error('Error deleting subscription via API:', error)
    alert('Failed to delete subscription. Please try again.')
  }
}

const toggleActive = async (id) => {
  const sub = subscriptions.value.find((s) => s.id === id)
  if (!sub) return

  const newMuteStatus = !sub.isMuted

  try {
    await api.patch(`/subscriptions/${id}`, {
      isMuted: newMuteStatus,
    })

    sub.isMuted = newMuteStatus

    console.log(`Subscription ${id} is now ${newMuteStatus ? 'Muted' : 'Active'}`)
  } catch (error) {
    console.error('Error toggling mute status:', error)
    alert('Failed to update status. Please try again.')
  }
}

const formatDate = (dateString) => {
  if (!dateString) return ''
  return dateString.substring(0, 10)
}

const resetForm = () => {
  formData.value = {
    name: '',
    cost: '',
    currency: 'UAH',
    plan: 'monthly',
    startDate: '',
    notes: '',
    isMuted: false,
  }
  editingSubscription.value = null
  showModal.value = false
}

const openReminderModal = (sub) => {
  console.log('Opening reminder modal for subscription:', sub)
  selectedSubscription.value = sub
  console.log('Selected subscription set to:', selectedSubscription.value)
  showReminderModal.value = true
}

const handleAddReminder = async () => {
  if (!reminderForm.value.daysBeforeRenewal) {
    alert('Please enter days before renewal')
    return
  }

  const payload = {
    subscriptionId: selectedSubscription.value.id,
    daysBeforeRenewal: parseInt(reminderForm.value.daysBeforeRenewal),
    notifyTime: reminderForm.value.notifyTime,
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  }

  console.log('Adding reminder with payload:', payload)

  try {
    await api.post('/reminders', payload)
    console.log('Reminder successfully created on API. Reloading subscriptions...')
    await loadSubscriptions()
    closeReminderModal()
  } catch (error) {
    if (error.response) {
      const { status, data } = error.response
      console.log(data)
    }
    alert('Failed to add reminder. Please try again.')
  }
}

const deleteReminder = async (reminderId) => {
  if (!confirm('Are you sure you want to delete this reminder?')) {
    return
  }

  try {
    await api.delete(`/reminders/${reminderId}`)
    console.log(`Reminder ${reminderId} deleted successfully from API. Reloading subscriptions...`)
    await loadSubscriptions()
  } catch (error) {
    console.error('Error deleting reminder via API:', error)
    alert('Failed to delete reminder. Please try again.')
  }
}

const closeReminderModal = () => {
  showReminderModal.value = false
  reminderForm.value = { daysBeforeRenewal: '', notifyTime: '09:00' }
}
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
            <svg
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
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
      <nav class="bg-white shadow-sm border-b">
        <div class="max-w-7xl mx-auto px-4 py-4 flex justify-between items-center">
          <div class="flex items-center gap-2">
            <svg
              class="w-6 h-6 text-indigo-600"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <h1 class="text-xl font-bold text-gray-800">Subscription Manager</h1>
          </div>
          <div class="flex items-center gap-4">
            <div class="flex items-center gap-2 text-gray-700">
              <svg
                class="w-5 h-5"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
              <span class="text-sm">{{ user?.name }}</span>
            </div>
            <button
              @click="logout"
              class="flex items-center gap-2 text-gray-600 hover:text-gray-800"
            >
              <svg
                class="w-5 h-5"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                />
              </svg>
            </button>
          </div>
        </div>
      </nav>

      <div class="max-w-7xl mx-auto p-6">
        <div class="bg-white rounded-lg shadow mb-6"></div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
          <div class="bg-white rounded-lg shadow p-6">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-gray-600 text-sm">Active Subscriptions</p>
                <p class="text-3xl font-bold text-gray-800">{{ activeSubscriptionsCount }}</p>
              </div>
              <svg
                class="w-12 h-12 text-indigo-600 opacity-20"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                />
              </svg>
            </div>
          </div>

          <div class="bg-white rounded-lg shadow p-6">
            <div class="flex items-center justify-between mb-4">
              <div class="flex flex-col">
                <select
                  v-model="selectedPeriod"
                  class="text-gray-600 text-sm bg-transparent border-none focus:ring-0 cursor-pointer p-0 capitalize"
                >
                  <option value="daily">Daily Cost</option>
                  <option value="weekly">Weekly Cost</option>
                  <option value="monthly">Monthly Cost</option>
                  <option value="yearly">Yearly Cost</option>
                </select>

                <p class="text-3xl font-bold text-gray-800">
                  {{ totalCost.toFixed(2) }} {{ selectedCurrency }}
                </p>
              </div>

              <div class="flex flex-col items-end">
                <select
                  v-model="selectedCurrency"
                  class="block w-24 p-2 text-sm text-gray-700 bg-gray-50 rounded-lg border border-gray-300 focus:ring-green-500 focus:border-green-500"
                >
                  <option v-for="curr in popularCurrencies" :key="curr.code" :value="curr.code">
                    {{ curr.code }} ({{ curr.symbol }})
                  </option>
                </select>
              </div>
            </div>

            <div class="mt-4 text-xs text-gray-400">Rates updated every 24 hours</div>
          </div>

          <div class="bg-white rounded-lg shadow p-6">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-gray-600 text-sm">Total Reminders</p>
                <p class="text-3xl font-bold text-gray-800">{{ totalReminders }}</p>
              </div>
              <svg
                class="w-12 h-12 text-purple-600 opacity-20"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-lg shadow mb-6">
          <div class="p-6 border-b flex justify-between items-center">
            <h2 class="text-xl font-bold text-gray-800">My Subscriptions</h2>
            <button
              @click="openAddModal"
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
            <div v-if="subscriptions.length === 0" class="text-center py-12 text-gray-500">
              <svg
                class="w-16 h-16 mx-auto mb-4 opacity-20"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <p>No subscriptions yet. Add your first one!</p>
            </div>

            <div v-else class="space-y-4">
              <div
                v-for="sub in subscriptions"
                :key="sub.id"
                :class="['border rounded-lg p-4', sub.isMuted && 'opacity-50']"
              >
                <div class="flex justify-between items-start mb-3">
                  <div class="flex-1">
                    <h3 class="text-lg font-bold text-gray-800">{{ sub.name }}</h3>
                    <p class="text-2xl font-bold text-indigo-600 mt-1">
                      {{ sub.cost }} {{ sub.currency }}
                      <span class="text-sm text-gray-600 font-normal ml-2">/ {{ sub.plan }}</span>
                    </p>
                  </div>
                  <div class="flex gap-2">
                    <button
                      @click="editSubscription(sub)"
                      class="p-2 text-blue-600 hover:bg-blue-50 rounded"
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
                          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                        />
                      </svg>
                    </button>
                    <button
                      @click="deleteSubscription(sub.id)"
                      class="p-2 text-red-600 hover:bg-red-50 rounded"
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
                          d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                        />
                      </svg>
                    </button>
                  </div>
                </div>

                <div class="grid grid-cols-3 gap-4 text-sm mb-3">
                  <div>
                    <p class="text-gray-600">Start Date</p>
                    <p class="font-semibold">{{ formatDateTime(sub.startDate) }}</p>
                  </div>
                  <div>
                    <p class="text-gray-600">Next Renewal</p>
                    <p class="font-semibold">{{ formatDateTime(sub.renewalDate) }}</p>
                  </div>
                  <div>
                    <p class="text-gray-600">Days Left</p>
                    <p class="font-semibold">{{ sub.daysLeft }}</p>
                  </div>
                </div>

                <p v-if="sub.notes" class="text-sm text-gray-600 mb-3">{{ sub.notes }}</p>

                <div class="flex items-center justify-between pt-3 border-t">
                  <div class="flex items-center gap-4">
                    <button
                      @click="toggleActive(sub.id)"
                      :class="[
                        'flex items-center gap-1 text-sm font-semibold transition-colors duration-200',
                        !sub.isMuted
                          ? 'text-green-600 hover:text-green-700'
                          : 'text-gray-400 hover:text-gray-600',
                      ]"
                    >
                      <svg
                        v-if="sub.isMuted"
                        class="w-4 h-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z"
                          clip-rule="evenodd"
                        />
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M17 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2"
                        />
                      </svg>
                      <svg
                        v-else
                        class="w-4 h-4"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z"
                        />
                      </svg>

                      {{ !sub.isMuted ? 'Active' : 'Muted' }}
                    </button>
                    <button
                      @click="openReminderModal(sub)"
                      class="flex items-center gap-1 text-sm text-indigo-600 hover:text-indigo-700"
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
                          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                        />
                      </svg>
                      Add Reminder
                    </button>
                  </div>
                  <span class="text-sm text-gray-600">
                    {{ sub.reminders?.length || 0 }} reminder(s)
                  </span>
                </div>

                <div
                  v-if="sub.reminders && sub.reminders.length > 0"
                  class="mt-4 pt-4 border-t space-y-2"
                >
                  <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">
                    Reminders
                  </p>
                  <div
                    v-for="reminder in sub.reminders"
                    :key="reminder.reminderId"
                    class="flex items-center justify-between bg-gradient-to-r from-indigo-50 to-purple-50 p-3 rounded-lg border border-indigo-100 hover:border-indigo-300 transition"
                  >
                    <div class="flex items-start gap-3 flex-1">
                      <svg
                        class="w-4 h-4 text-indigo-600 mt-0.5 flex-shrink-0"
                        xmlns="http://www.w3.org/2000/svg"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                        />
                      </svg>
                      <div class="text-sm text-gray-700">
                        <p class="font-medium">
                          {{ reminder.daysBeforeRenewal }} day(s) before renewal
                        </p>
                        <p class="text-xs text-gray-600 mt-1">
                          at
                          <span class="font-semibold text-indigo-600">{{
                            localNotifyTime(reminder.notifyTime)
                          }}</span>
                        </p>
                        <p class="text-xs text-gray-500 mt-1">
                          Next:
                          <span class="font-medium">{{
                            formatDateTime(reminder.nextReminder)
                          }}</span>
                        </p>
                      </div>
                    </div>
                    <button
                      @click="deleteReminder(reminder.id)"
                      class="p-1.5 text-red-500 hover:text-red-700 hover:bg-red-50 rounded-md transition flex-shrink-0"
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
                          d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                        />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div
        v-if="showModal"
        class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
      >
        <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
          <div class="p-6 border-b">
            <h2 class="text-2xl font-bold text-gray-800">
              {{ editingSubscription ? 'Edit Subscription' : 'Add Subscription' }}
            </h2>
          </div>

          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">Name</label>
              <input
                v-model="formData.name"
                type="text"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="Netflix, Spotify, etc."
              />
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-semibold text-gray-700 mb-2">Cost</label>
                <input
                  v-model="formData.cost"
                  type="number"
                  step="0.01"
                  class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
              </div>
              <div>
                <label class="block text-sm font-semibold text-gray-700 mb-2">Currency</label>
                <select
                  v-model="formData.currency"
                  class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                >
                  <option>UAH</option>
                  <option>USD</option>
                  <option>EUR</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">Plan</label>
              <select
                v-model="formData.plan"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500"
              >
                <option value="Weekly">Weekly</option>
                <option value="Monthly">Monthly</option>
                <option value="Quarterly">Quarterly</option>
                <option value="Annual">Annual</option>
                <option value="Custom">Custom Period</option>
              </select>
            </div>

            <div v-if="formData.plan === 'Custom'" class="mt-4">
              <label class="block text-sm font-semibold text-gray-700 mb-2"> Every X Days </label>
              <input
                v-model="formData.customPeriodInDays"
                type="number"
                min="1"
                placeholder="e.g. 45"
                class="w-full px-4 py-2 border border-indigo-300 rounded-lg focus:ring-2 focus:ring-indigo-500 bg-indigo-50"
              />
              <p class="text-xs text-indigo-600 mt-1">Enter the number of days between billings.</p>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-semibold text-gray-700 mb-2">Start Date</label>
                <input
                  v-model="formData.startDate"
                  type="date"
                  class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
              </div>
            </div>
            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2"
                >Auto-Calculated Renewal</label
              >
              <div
                class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-indigo-600 font-bold"
              >
                {{ calculatedNextRenewalDate || 'Select a start date' }}
              </div>
            </div>
            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">Notes</label>
              <textarea
                v-model="formData.notes"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                rows="3"
                placeholder="Any additional notes..."
              ></textarea>
            </div>

            <div class="flex gap-3 pt-4">
              <button
                @click="handleSubmit"
                class="flex-1 bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold"
              >
                {{ editingSubscription ? 'Update' : 'Add' }} Subscription
              </button>
              <button
                @click="resetForm"
                class="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition font-semibold"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>

      <div
        v-if="showReminderModal && selectedSubscription"
        class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
      >
        <div class="bg-white rounded-lg shadow-xl max-w-md w-full">
          <div class="p-6 border-b">
            <h2 class="text-2xl font-bold text-gray-800">Add Reminder</h2>
            <p class="text-gray-600 text-sm mt-1">for {{ selectedSubscription.name }}</p>
          </div>

          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">
                Remind me (days before renewal)
              </label>
              <input
                v-model="reminderForm.daysBeforeRenewal"
                type="number"
                min="1"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="e.g., 3"
              />
            </div>

            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">
                Notification Time
              </label>
              <input
                v-model="reminderForm.notifyTime"
                type="time"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500"
              />
              <p class="text-xs text-gray-500 mt-1">
                Pick the time of day you want to receive the alert.
              </p>
            </div>

            <div class="flex gap-3 pt-4">
              <button
                @click="handleAddReminder"
                class="flex-1 bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold"
              >
                Add Reminder
              </button>
              <button
                @click="closeReminderModal"
                class="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition font-semibold"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
