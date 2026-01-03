<script setup>
defineProps({
  isOpen: Boolean,
  isEditing: Boolean,
  modelValue: Object,
  nextRenewalDate: String,
})

defineEmits(['close', 'save'])
</script>

<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
  >
    <div class="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
      <div class="p-6 border-b">
        <h2 class="text-2xl font-bold text-gray-800">
          {{ isEditing ? 'Edit Subscription' : 'Add Subscription' }}
        </h2>
      </div>

      <div class="p-6 space-y-4">
        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-2">Name</label>
          <input
            v-model="modelValue.name"
            type="text"
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            placeholder="Netflix, Spotify, etc."
          />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Cost</label>
            <input
              v-model="modelValue.cost"
              type="number"
              step="0.01"
              class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Currency</label>
            <select
              v-model="modelValue.currency"
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
            v-model="modelValue.plan"
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500"
          >
            <option value="Monthly">Monthly</option>
            <option value="Quarterly">Quarterly</option>
            <option value="Annual">Annual</option>
            <option value="Custom">Custom Period</option>
          </select>
        </div>

        <div v-if="modelValue.plan === 'Custom'" class="mt-4">
          <label class="block text-sm font-semibold text-gray-700 mb-2"> Every X Days </label>
          <input
            v-model="modelValue.customPeriodInDays"
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
              v-model="modelValue.startDate"
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
            {{ nextRenewalDate || 'Select a start date' }}
          </div>
        </div>

        <div>
          <label class="block text-sm font-semibold text-gray-700 mb-2">Notes</label>
          <textarea
            v-model="modelValue.notes"
            class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
            rows="3"
            placeholder="Any additional notes..."
          ></textarea>
        </div>

        <div class="flex gap-3 pt-4">
          <button
            @click="$emit('save')"
            class="flex-1 bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold"
          >
            {{ isEditing ? 'Update' : 'Add' }} Subscription
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
