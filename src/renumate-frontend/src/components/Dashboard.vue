<script setup>
import { watch } from 'vue'
import { useAuth0 } from '@auth0/auth0-vue'
import SubscriptionModal from '@/components/SubscriptionModal.vue'
import ReminderModal from '@/components/ReminderModal.vue'
import StatCard from '@/components/StatCard.vue'
import IconBell from '@/components/icons/IconBell.vue'
import IconCalendar from '@/components/icons/IconCalendar.vue'
import SubscriptionItem from '@/components/SubscriptionItem.vue'
import SubscriptionEmptyState from '@/components/SubscriptionEmptyState.vue'
import { formatDateTime, localNotifyTime } from '@/utils/formatters.ts'
import { useSubscriptions } from '@/composables/useSubscriptions'
import { useReminders } from '@/composables/useReminders'
import { useUsers } from '@/composables/useUsers'
import ConfirmDeleteModal from './ConfirmDeleteModal.vue'

const {
  subscriptions,
  resetForm,
  popularCurrencies,
  activeSubscriptionsCount,
  activeRemindersCount,
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
  confirmDelete,
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
  projectedCost,
  showDeleteConfirm,
  subToDelete,
  openDeleteModal,
  isSubmitting: isSubmittingSubscription,
} = useSubscriptions()

const {
  showReminderModal,
  reminderForm,
  openReminderModal,
  handleAddReminder,
  closeReminderModal,
  reminderErrors,
  reminderToDelete,
  openDeleteReminderModal,
  showDeleteReminderConfirm,
  confirmDeleteReminder,
  isSubmitting: isSubmittingReminder,
} = useReminders(loadSubscriptions, selectedSubscription, fetchSummary)

const { syncUserWithDatabase } = useUsers()

const { isAuthenticated, loginWithRedirect, isLoading } = useAuth0()

watch(
  [isAuthenticated, isLoading],
  async ([isAuth, isLoad]) => {
    if (!isLoad && isAuth) {
      try {
        await syncUserWithDatabase();
        await loadSubscriptions();
        await fetchSummary();
      } catch (error) {
        console.error('Initial sync failed:', error);
      }
    }
  },
  { immediate: true }
);

watch([selectedCurrency, selectedPeriod], fetchSummary)
</script>
<template>
  <div class="min-h-screen bg-slate-50/50">
    <div
      v-if="isLoading"
      class="min-h-screen flex items-center justify-center p-4 bg-white/80 backdrop-blur-md fixed inset-0 z-[100]"
    >
      <div class="text-center">
        <div class="relative w-16 h-16 mx-auto mb-6">
          <div class="absolute inset-0 rounded-2xl border-4 border-indigo-100"></div>
          <div
            class="absolute inset-0 rounded-2xl border-4 border-indigo-600 border-t-transparent animate-spin"
          ></div>
        </div>
        <h1 class="text-xl font-black text-slate-800 tracking-tight animate-pulse">
          Authenticating...
        </h1>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-4 py-8 lg:px-8">
      <div class="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-10">
        <div>
          <h1 class="text-4xl font-black text-slate-900 tracking-tight">Dashboard</h1>
          <p class="text-slate-500 font-medium mt-1">
            Welcome back! Here's what's happening with your subscriptions.
          </p>
        </div>
        <button
          @click="openAddSubscriptionModal"
          class="group flex items-center justify-center gap-2 bg-slate-900 text-white px-6 py-4 rounded-2xl hover:bg-indigo-600 hover:shadow-2xl hover:shadow-indigo-200 transition-all duration-300 font-bold active:scale-95 shadow-xl shadow-slate-200 cursor-pointer"
        >
          <svg
            class="w-5 h-5 group-hover:rotate-90 transition-transform duration-300"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2.5"
              d="M12 4v16m8-8H4"
            />
          </svg>
          Add Subscription
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
        <StatCard
          :loading="summaryLoading"
          class="!bg-white !rounded-[2rem] !p-6 border-none shadow-sm hover:shadow-md transition-shadow"
        >
          <template #label
            ><p class="text-slate-400 text-xs font-black uppercase tracking-widest">
              Active Subscriptions
            </p></template
          >
          <template #value
            ><p class="text-4xl font-black text-slate-900 mt-2">
              {{ activeSubscriptionsCount ?? '0' }}
            </p></template
          >
          <template #right>
            <div class="p-3 bg-indigo-50 text-indigo-600 rounded-2xl"><IconBell /></div>
          </template>
        </StatCard>

        <StatCard
          :loading="summaryLoading"
          :projected-cost="projectedCost?.toFixed(2)"
          class="!bg-indigo-600 !rounded-[2rem] !p-6 border-none shadow-xl shadow-indigo-100"
        >
          <template #label>
            <select
              v-model="selectedPeriod"
              class="bg-white/10 text-white text-[10px] font-black p-2 rounded-xl border border-white/20 focus:ring-0 cursor-pointer backdrop-blur-md uppercase"
            >
              <option value="daily">Daily Cost</option>
              <option value="monthly">Monthly Cost</option>
              <option value="yearly">Yearly Cost</option>
            </select>
          </template>
          <template #value>
            <p class="text-3xl font-black text-white mt-2 leading-none">
              {{ totalCost?.toFixed(2) ?? '0.00' }}
              <span class="text-lg opacity-80">{{ selectedCurrency }}</span>
            </p>
          </template>
          <template #right>
            <select
              v-model="selectedCurrency"
              class="bg-white/10 text-white text-[10px] font-black p-2 rounded-xl border border-white/20 focus:ring-0 cursor-pointer backdrop-blur-md uppercase"
            >
              <option
                v-for="curr in popularCurrencies"
                :key="curr.code"
                :value="curr.code"
                class="text-slate-900"
              >
                {{ curr.code }}
              </option>
            </select>
          </template>
        </StatCard>

        <StatCard
          :loading="summaryLoading"
          class="!bg-white !rounded-[2rem] !p-6 border-none shadow-sm"
        >
          <template #label
            ><p class="text-slate-400 text-xs font-black uppercase tracking-widest">
              Reminders Set
            </p></template
          >
          <template #value
            ><p class="text-4xl font-black text-slate-900 mt-2">
              {{ activeRemindersCount ?? '0' }}
            </p></template
          >
          <template #right>
            <div class="p-3 bg-rose-50 text-rose-500 rounded-2xl"><IconCalendar /></div>
          </template>
        </StatCard>
      </div>

      <div
        class="bg-white/40 backdrop-blur-md rounded-[2.5rem] border border-white shadow-sm overflow-hidden p-2"
      >
        <div class="p-6 md:p-10">
          <div class="flex items-center justify-between mb-8 px-2">
            <h2 class="text-2xl font-black text-slate-900 tracking-tight">My Subscriptions</h2>
            <div class="flex items-center gap-2">
              <span class="text-xs font-bold text-slate-400 bg-slate-100 px-3 py-1 rounded-full"
                >{{ totalCount }} total</span
              >
            </div>
          </div>

          <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div
              v-for="i in 4"
              :key="i"
              class="h-48 bg-white/50 animate-pulse rounded-[2rem] border border-slate-100"
            ></div>
          </div>

          <SubscriptionEmptyState v-else-if="subscriptions.length === 0" />

          <div v-else class="space-y-6">
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <SubscriptionItem
                v-for="sub in subscriptions"
                :key="sub.id"
                :sub="sub"
                :format-date-time="formatDateTime"
                :format-time="localNotifyTime"
                @edit="editSubscription"
                @delete="openDeleteModal(sub)"
                @toggle-mute="toggleActive"
                @add-reminder="openReminderModal"
                @delete-reminder="openDeleteReminderModal"
              />
            </div>

            <div
              v-if="totalPages > 1"
              class="mt-12 flex flex-col sm:flex-row items-center justify-between gap-6 bg-white p-6 rounded-3xl shadow-sm border border-slate-50"
            >
              <p class="text-sm font-bold text-slate-400">
                Showing <span class="text-slate-900">{{ (currentPage - 1) * pageSize + 1 }}</span
                >-
                <span class="text-slate-900">{{
                  Math.min(currentPage * pageSize, totalCount)
                }}</span>
              </p>

              <nav class="flex items-center gap-2">
                <button
                  v-for="p in totalPages"
                  :key="p"
                  @click="setPage(p)"
                  :class="[
                    'w-10 h-10 rounded-xl font-bold transition-all text-sm',
                    currentPage === p
                      ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-100 scale-110'
                      : 'bg-slate-50 text-slate-500 hover:bg-slate-100',
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

    <SubscriptionModal
      :is-open="showSubscriptionModal"
      :is-editing="!!editingSubscription"
      :is-submitting="isSubmittingSubscription"
      :model-value="formData"
      :next-renewal-date="calculatedNextRenewalDate"
      :format-date-time="formatDateTime"
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

    <ConfirmDeleteModal
      :is-open="showDeleteConfirm"
      :title="subToDelete?.name || ''"
      type="subscription"
      @close="showDeleteConfirm = false"
      @confirm="confirmDelete"
    />

    <ConfirmDeleteModal
      :is-open="showDeleteReminderConfirm"
      :title="reminderToDelete?.info || ''"
      type="reminder"
      @close="showDeleteReminderConfirm = false"
      @confirm="confirmDeleteReminder"
    />
  </div>
</template>
