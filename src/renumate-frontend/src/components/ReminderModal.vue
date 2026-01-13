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
  <div
    v-if="isOpen && subscription"
    class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
  >
    <div class="bg-white rounded-lg shadow-xl max-w-md w-full">
      <div class="p-6 border-b">
        <h2 class="text-2xl font-bold text-gray-800">Add Reminder</h2>
        <p class="text-gray-600 text-sm mt-1">for {{ subscription.name }}</p>
      </div>

      <div class="p-6 space-y-4">
        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-2">
            Remind me (days before renewal)
          </label>
          <input
            v-model="modelValue.daysBeforeRenewal"
            type="number"
            min="1"
            :class="[
              'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent transition-colors',
              errors.DaysBeforeRenewal
                ? 'border-red-500 bg-red-50 focus:ring-red-500'
                : 'border-gray-300 focus:ring-indigo-500',
            ]"
            placeholder="e.g., 3"
          />
          <p v-if="errors.DaysBeforeRenewal" class="mt-1 text-sm text-red-600 font-medium">
            {{ errors.DaysBeforeRenewal[0] }}
          </p>
        </div>

        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-2"> Notification Time </label>
          <input
            v-model="modelValue.notifyTime"
            type="time"
            :class="[
              'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent transition-colors',
              errors.NotifyTime
                ? 'border-red-500 bg-red-50 focus:ring-red-500'
                : 'border-gray-300 focus:ring-indigo-500',
            ]"
          />
          <p v-if="errors.NotifyTime" class="mt-1 text-sm text-red-600 font-medium">
            {{ errors.NotifyTime[0] }}
          </p>
          <p class="text-xs text-gray-500 mt-1">
            Pick the time of day you want to receive the alert.
          </p>
        </div>

        <div class="flex gap-3 pt-4">
          <button
            @click="$emit('save')"
            :disabled="isSubmitting"
            class="flex-1 bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold disabled:bg-indigo-400 disabled:cursor-not-allowed flex justify-center items-center"
          >
            <IconSpinner v-if="isSubmitting" class="-ml-1 mr-3 h-5 w-5 text-white" />
            {{ isSubmitting ? 'Adding...' : 'Add Reminder' }}
          </button>
          <button
            @click="$emit('close')"
            class="px-6 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition font-semibold"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
