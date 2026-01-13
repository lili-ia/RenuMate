<script setup>
defineProps({
  isOpen: Boolean,
  isEditing: Boolean,
  isSubmitting: Boolean,
  modelValue: Object,
  nextRenewalDate: String,
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
            :class="[
              'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent',
              errors.Name
                ? 'border-red-500 focus:ring-red-200'
                : 'border-gray-300 focus:ring-indigo-500',
            ]"
            placeholder="Netflix, Spotify, etc."
          />
          <p v-if="errors.Name" class="mt-1 text-sm text-red-600 font-medium">
            {{ errors.Name[0] }}
          </p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Cost</label>
            <input
              v-model="modelValue.cost"
              type="number"
              step="0.01"
              :class="[
                'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent',
                errors.Cost
                  ? 'border-red-500 focus:ring-red-200'
                  : 'border-gray-300 focus:ring-indigo-500',
              ]"
            />
            <p v-if="errors.Cost" class="mt-1 text-sm text-red-600 font-medium">
              {{ errors.Cost[0] }}
            </p>
          </div>

          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-2">Currency</label>
            <select
              v-model="modelValue.currency"
              :class="[
                'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent',
                errors.Currency
                  ? 'border-red-500 focus:ring-red-200'
                  : 'border-gray-300 focus:ring-indigo-500',
              ]"
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
            :class="[
              'w-full px-4 py-2 border rounded-lg focus:ring-2 focus:border-transparent bg-indigo-50',
              errors.CustomPeriodInDays
                ? 'border-red-500 focus:ring-red-200'
                : 'border-indigo-300 focus:ring-indigo-500',
            ]"
          />
          <p v-if="errors.CustomPeriodInDays" class="mt-1 text-sm text-red-600 font-medium">
            {{ errors.CustomPeriodInDays[0] }}
          </p>
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
            :disabled="isSubmitting"
            class="flex-1 flex items-center justify-center bg-indigo-600 text-white py-2 rounded-lg hover:bg-indigo-700 transition font-semibold disabled:opacity-70 disabled:cursor-not-allowed"
          >
            <IconSpinner v-if="isSubmitting" class="-ml-1 mr-3 h-5 w-5 text-white" />

            <span>
              {{
                isSubmitting
                  ? 'Processing...'
                  : isEditing
                    ? 'Update Subscription'
                    : 'Add Subscription'
              }}
            </span>
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
