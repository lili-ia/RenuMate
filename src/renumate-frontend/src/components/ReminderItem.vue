<template>
  <div
    class="group flex items-center justify-between bg-white border border-slate-100 p-3 rounded-2xl shadow-sm hover:shadow-md hover:border-indigo-100 transition-all duration-300"
  >
    <div class="flex items-center gap-3 flex-1 min-w-0">
      <div
        class="w-10 h-10 rounded-xl bg-indigo-50 flex items-center justify-center flex-shrink-0 group-hover:scale-110 transition-transform"
      >
        <IconBell class="w-5 h-5 text-indigo-600" />
      </div>

      <div class="flex flex-col min-w-0">
        <div class="flex items-center gap-2">
          <p class="text-sm font-bold text-slate-800">
            {{ reminder.daysBeforeRenewal }}
            {{ reminder.daysBeforeRenewal === 1 ? 'day' : 'days' }} before
          </p>
          <span
            class="px-1.5 py-0.5 bg-indigo-100 text-indigo-700 text-[10px] font-black rounded-md uppercase tracking-tighter"
          >
            {{ formatTime(reminder.notifyTime) }}
          </span>
        </div>

        <p class="text-[11px] text-slate-400 mt-0.5 flex items-center gap-1">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-3 w-3"
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
          Next alert:
          <span class="font-semibold text-slate-500">{{
            formatDateTime(reminder.nextReminder, false)
          }}</span>
        </p>
      </div>
    </div>

    <button
      @click="$emit('delete', reminder.id)"
      class="ml-2 p-2 text-slate-300 hover:text-red-500 hover:bg-red-50 rounded-xl transition-all duration-200 cursor-pointer opacity-0 group-hover:opacity-100"
      title="Remove reminder"
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
  formatTime: Function,
  formatDateTime: Function,
})

defineEmits(['delete'])
</script>

<style scoped>

.text-red-500 {
  animation: pulse-red 2s infinite;
}

@keyframes pulse-red {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
}
</style>
