<script setup>
defineProps({
  isOpen: Boolean,
  isSubmitting: Boolean,
  subscription: Object,
  modelValue: Object,
  errors: {
    type: Object,
    default: () => ({}),
  },
})

defineEmits(['close', 'save'])
import IconSpinner from './icons/IconSpinner.vue'
</script>

<template>
  <Transition name="modal-fade">
    <div
      v-if="isOpen && subscription"
      class="fixed inset-0 z-[60] flex items-center justify-center p-4 overflow-y-auto"
    >
      <div
        class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity"
        @click="$emit('close')"
      ></div>

      <div
        class="relative bg-white rounded-[2rem] shadow-2xl max-w-sm w-full overflow-hidden transform transition-all border border-slate-100"
      >
        <div class="px-8 pt-8 pb-4 text-center">
          <div
            class="w-16 h-16 bg-indigo-50 text-indigo-600 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-inner"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-8 w-8"
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
          <h2 class="text-2xl font-black text-slate-900 tracking-tight">Add Reminder</h2>
          <p class="text-slate-500 text-sm mt-1 italic">for {{ subscription.name }}</p>
        </div>

        <div class="p-8 space-y-6">
          <div>
            <label
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
            >
              Days Before Renewal
            </label>
            <div class="relative">
              <input
                v-model="modelValue.daysBeforeRenewal"
                type="number"
                min="1"
                class="w-full pl-5 pr-12 py-3.5 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all font-bold text-slate-900 shadow-sm"
                placeholder="e.g. 3"
              />
              <span
                class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 text-sm font-bold"
                >days</span
              >
            </div>
            <p v-if="errors.DaysBeforeRenewal" class="mt-2 text-xs text-red-500 font-medium">
              {{ errors.DaysBeforeRenewal[0] }}
            </p>
          </div>

          <div>
            <label
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
            >
              Notification Time
            </label>
            <input
              v-model="modelValue.notifyTime"
              type="time"
              class="w-full px-5 py-3.5 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all font-bold text-slate-900 shadow-sm cursor-pointer"
            />
            <p v-if="errors.NotifyTime" class="mt-2 text-xs text-red-500 font-medium">
              {{ errors.NotifyTime[0] }}
            </p>
            <p
              class="text-[10px] text-slate-400 mt-2 ml-1 leading-tight uppercase font-bold tracking-tight"
            >
              Best time to catch your attention.
            </p>
          </div>

          <div class="flex flex-col gap-3 pt-2">
            <button
              @click="$emit('save')"
              :disabled="isSubmitting"
              class="w-full bg-indigo-600 text-white py-4 rounded-2xl hover:bg-indigo-700 active:scale-[0.98] transition-all font-bold shadow-lg shadow-indigo-100 disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
            >
              <IconSpinner v-if="isSubmitting" class="h-5 w-5" />
              {{ isSubmitting ? 'Setting alert...' : 'Confirm Reminder' }}
            </button>
            <button
              @click="$emit('close')"
              class="w-full bg-white text-slate-500 py-3 rounded-2xl border border-transparent hover:text-slate-700 transition-all font-bold text-sm cursor-pointer"
            >
              Not now
            </button>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
  transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.modal-fade-enter-from {
  opacity: 50;
  transform: scale(0.9) translateY(20px);
}
.modal-fade-leave-to {
  opacity: 0;
  transform: scale(0.9) translateY(20px);
}

input[type='time']::-webkit-calendar-picker-indicator {
  cursor: pointer;
  filter: invert(0.5);
}
</style>
