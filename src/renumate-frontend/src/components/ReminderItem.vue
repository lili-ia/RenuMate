<template>
  <div
    class="flex items-center justify-between bg-gradient-to-r from-indigo-50 to-purple-50 p-3 rounded-lg border border-indigo-100 hover:border-indigo-300 transition"
  >
    <div class="flex items-start gap-3 flex-1">
      <IconBell class="w-4 h-4 text-indigo-600 mt-0.5 flex-shrink-0" />

      <div class="text-sm text-gray-700">
        <p class="font-medium">{{ reminder.daysBeforeRenewal }} day(s) before renewal</p>
        <p class="text-xs text-gray-600 mt-1">
          at
          <span class="font-semibold text-indigo-600">
            {{ formatTime(reminder.notifyTime) }}
          </span>
        </p>
        <p class="text-xs text-gray-500 mt-1">
          Next:
          <span class="font-medium">{{ formatDate(reminder.nextReminder) }}</span>
        </p>
      </div>
    </div>

    <button
      @click="$emit('delete', reminder.id)"
      class="p-1.5 text-red-500 hover:text-red-700 hover:bg-red-50 rounded-md transition flex-shrink-0"
    >
      <IconTrash class="w-4 h-4" />
    </button>
  </div>
</template>

<script setup>
import IconBell from './icons/IconBell.vue'
import IconTrash from './icons/IconTrash.vue'
defineProps({
  reminder: Object,
  formatDate: Function,
  formatTime: Function,
})

defineEmits(['delete'])
</script>
