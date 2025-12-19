<script setup>
import { ref, computed, watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import api, { setAuthToken } from './api'

const { isAuthenticated, loginWithRedirect, isLoading, getAccessTokenSilently, user } = useAuth0()

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
        await loadSubscriptions()
        await loadActiveCount()
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
  serviceName: '',
  cost: '',
  currency: 'UAH',
  billingCycle: 'Monthly',
  startDate: '',
  nextRenewalDate: '',
  notes: '',
  isActive: true,
})

const reminderForm = ref({
  reminderDaysBefore: '',
  isEnabled: true,
})

const activeSubscriptionsCount = ref(0)

const loadActiveCount = async () => {
  try {
    const response = await api.get('/subscriptions/summary')
    if (typeof response.data === 'number') {
      activeSubscriptionsCount.value = response.data
    } else if (response.data && response.data.activeSubscriptionsCount !== undefined) {
      activeSubscriptionsCount.value = response.data.activeSubscriptionsCount
    } else {
      activeSubscriptionsCount.value = 0
    }

    console.log('Active subscription count loaded successfully.')
  } catch (error) {
    console.error('Error loading active subscription count:', error)
    activeSubscriptionsCount.value = 0
  }
}

const totalMonthlyCost = computed(() => {
  return subscriptions.value
    .filter((s) => s.isActive)
    .reduce((sum, s) => {
      const cost = parseFloat(s.cost) || 0
      if (s.billingCycle === 'Yearly') return sum + cost / 12
      if (s.billingCycle === 'Weekly') return sum + cost * 4.33
      return sum + cost
    }, 0)
})

const totalReminders = computed(() => {
  return subscriptions.value.reduce((sum, s) => sum + (s.reminders?.length || 0), 0)
})

const handleLogin = async () => {
  loginWithRedirect()
}

const logout = () => {
  localStorage.removeItem('accessToken')
}

const loadSubscriptions = async () => {
  try {
    const response = await api.get('/subscriptions')
    subscriptions.value = response.data

    console.log('Subscriptions loaded successfully.')
  } catch (error) {
    console.error('Error loading subscriptions:', error)
    subscriptions.value = []
  }
}

const saveSubscriptions = (subs) => {
  localStorage.setItem('subscriptions', JSON.stringify(subs))
  subscriptions.value = subs
}

const openAddModal = () => {
  showModal.value = true
}

const editSubscription = (sub) => {
  const baseData = { ...sub }

  formData.value = {
    ...baseData,
    startDate: formatDate(baseData.startDate),
    nextRenewalDate: formatDate(baseData.nextRenewalDate),
  }

  editingSubscription.value = sub
  showModal.value = true
}

const handleSubmit = async () => {
  if (!formData.value.serviceName || !formData.value.cost) {
    alert('Please fill in required fields')
    return
  }

  const payload = {
    ...formData.value,
  }

  try {
    if (editingSubscription.value) {
      const subId = editingSubscription.value.subscriptionId
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

const toggleActive = (id) => {
  const updated = subscriptions.value.map((sub) =>
    sub.subscriptionId === id ? { ...sub, isActive: !sub.isActive } : sub,
  )
  saveSubscriptions(updated)
}

const formatDate = (dateString) => {
  if (!dateString) return ''
  return dateString.substring(0, 10)
}

const resetForm = () => {
  formData.value = {
    serviceName: '',
    cost: '',
    currency: 'UAH',
    billingCycle: 'monthly',
    startDate: '',
    nextRenewalDate: '',
    notes: '',
    isActive: true,
  }
  editingSubscription.value = null
  showModal.value = false
}

const openReminderModal = (sub) => {
  selectedSubscription.value = sub
  showReminderModal.value = true
}

const handleAddReminder = async () => {
  if (!reminderForm.value.reminderDaysBefore) {
    alert('Please enter days before renewal')
    return
  }

  const payload = {
    subscriptionId: selectedSubscription.value.subscriptionId,
    reminderDaysBefore: parseInt(reminderForm.value.reminderDaysBefore),
    isEnabled: reminderForm.value.isEnabled,
  }

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

const deleteReminder = async (subId, reminderId) => {
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
  reminderForm.value = { reminderDaysBefore: '', isEnabled: true }
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
            <div class="flex items-center justify-between">
              <div>
                <p class="text-gray-600 text-sm">Monthly Cost</p>
                <p class="text-3xl font-bold text-gray-800">
                  {{ totalMonthlyCost.toFixed(2) }} UAH
                </p>
              </div>
              <svg
                class="w-12 h-12 text-green-600 opacity-20"
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
                :key="sub.subscriptionId"
                :class="['border rounded-lg p-4', !sub.isActive && 'opacity-50']"
              >
                <div class="flex justify-between items-start mb-3">
                  <div class="flex-1">
                    <h3 class="text-lg font-bold text-gray-800">{{ sub.serviceName }}</h3>
                    <p class="text-2xl font-bold text-indigo-600 mt-1">
                      {{ sub.cost }} {{ sub.currency }}
                      <span class="text-sm text-gray-600 font-normal ml-2"
                        >/ {{ sub.billingCycle }}</span
                      >
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
                      @click="deleteSubscription(sub.subscriptionId)"
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

                <div class="grid grid-cols-2 gap-4 text-sm mb-3">
                  <div>
                    <p class="text-gray-600">Start Date</p>
                    <p class="font-semibold">{{ sub.startDate }}</p>
                  </div>
                  <div>
                    <p class="text-gray-600">Next Renewal</p>
                    <p class="font-semibold">{{ sub.nextRenewalDate }}</p>
                  </div>
                </div>

                <p v-if="sub.notes" class="text-sm text-gray-600 mb-3">{{ sub.notes }}</p>

                <div class="flex items-center justify-between pt-3 border-t">
                  <div class="flex items-center gap-4">
                    <button
                      @click="toggleActive(sub.subscriptionId)"
                      :class="[
                        'text-sm font-semibold',
                        sub.isActive ? 'text-green-600' : 'text-gray-400',
                      ]"
                    >
                      {{ sub.isActive ? '● Active' : '○ Inactive' }}
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
                  class="mt-3 pt-3 border-t space-y-2"
                >
                  <div
                    v-for="reminder in sub.reminders"
                    :key="reminder.reminderId"
                    class="flex items-center justify-between bg-gray-50 p-2 rounded"
                  >
                    <span class="text-sm">
                      Remind {{ reminder.reminderDaysBefore }} days before renewal
                      <span v-if="!reminder.isEnabled" class="text-gray-400 ml-2">(Disabled)</span>
                    </span>
                    <button
                      @click="deleteReminder(sub.subscriptionId, reminder.reminderId)"
                      class="text-red-600 hover:text-red-700"
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
              <label class="block text-sm font-semibold text-gray-700 mb-2">Service Name</label>
              <input
                v-model="formData.serviceName"
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
              <label class="block text-sm font-semibold text-gray-700 mb-2">Billing Cycle</label>
              <select
                v-model="formData.billingCycle"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
                <option>weekly</option>
                <option>monthly</option>
                <option>yearly</option>
              </select>
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
              <div>
                <label class="block text-sm font-semibold text-gray-700 mb-2"
                  >Next Renewal Date</label
                >
                <input
                  v-model="formData.nextRenewalDate"
                  type="date"
                  class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
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
            <p class="text-gray-600 text-sm mt-1">for {{ selectedSubscription.serviceName }}</p>
          </div>

          <div class="p-6 space-y-4">
            <div>
              <label class="block text-sm font-semibold text-gray-700 mb-2">
                Remind me (days before renewal)
              </label>
              <input
                v-model="reminderForm.reminderDaysBefore"
                type="number"
                min="1"
                class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="e.g., 3"
              />
            </div>

            <div class="flex items-center gap-2">
              <input
                v-model="reminderForm.isEnabled"
                type="checkbox"
                id="reminderEnabled"
                class="w-4 h-4 text-indigo-600 rounded"
              />
              <label for="reminderEnabled" class="text-sm font-semibold text-gray-700">
                Enable reminder
              </label>
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
