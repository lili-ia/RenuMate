<script setup>
defineProps({
  isOpen: Boolean,
  subscription: Object,
  modelValue: Object,
})

defineEmits(['close', 'save'])
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
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            placeholder="e.g., 3"
          />
        </div>

        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-2"> Notification Time </label>
          <input
            v-model="modelValue.notifyTime"
            type="time"
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500"
          />
          <p class="text-xs text-gray-500 mt-1">
            Pick the time of day you want to receive the alert.
          </p>
        </div>

        <div class="flex gap-3 pt-4">
          <button
            @click="$emit('save')"
            class="flex-1 bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold"
          >
            Add Reminder
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
