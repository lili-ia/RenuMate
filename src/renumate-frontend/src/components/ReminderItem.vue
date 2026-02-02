<template>
  <div
    class="group flex items-center justify-between bg-white border border-slate-100 p-3 rounded-2xl shadow-sm hover:shadow-md hover:border-indigo-100 transition-all duration-300"
  >
    <div class="flex items-center gap-3 flex-1 min-w-0">
      <div
        class="w-10 h-10 rounded-xl bg-indigo-50 flex items-center justify-center flex-shrink-0 group-hover:scale-110 transition-transform"
      >
        <i class="pi pi-bell w-5 h-5 text-indigo-600"></i>
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
          <i class="pi pi-calendar-clock" style="font-size: 0.75rem"></i>
          {{ reminder.isSent ? 'Last' : 'Next' }} alert:
          <span class="font-semibold text-slate-500">{{
            formatDateTime(reminder.nextReminder, false)
          }}</span>
          
        </p>
        <p v-if="reminder.isSent" class="text-[11px] text-slate-400 mt-0.5 flex items-center gap-1">Next alert will be scheduled after the subscription renews.</p>
      </div>
    </div>

    <button
      @click="$emit('delete', reminder.id)"
      class="ml-2 w-8 h-8 flex items-center justify-center text-slate-300 hover:text-red-500 hover:bg-red-50 rounded-xl transition-all duration-200 cursor-pointer opacity-0 group-hover:opacity-100 border border-transparent"
      title="Remove reminder"
    >
      <i class="pi pi-trash" style="font-size: 0.85rem"></i>
    </button>
  </div>
</template>

<script setup>
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
