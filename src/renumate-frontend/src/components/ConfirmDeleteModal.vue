<template>
  <Transition name="modal-fade">
    <div v-if="isOpen" class="fixed inset-0 z-[110] flex items-center justify-center p-4">
      <div
        class="fixed inset-0 bg-slate-900/60 backdrop-blur-md transition-opacity"
        @click="$emit('close')"
      ></div>

      <div
        class="relative bg-white rounded-[2.5rem] max-w-sm w-full p-8 shadow-2xl border border-slate-100 overflow-hidden"
      >
        <div class="relative z-10 text-center">
          <div
            class="relative w-20 h-20 bg-rose-500 text-white rounded-[2rem] flex items-center justify-center mx-auto mb-6 shadow-xl shadow-rose-200 animate-bounce-subtle"
          >
            <i class="pi pi-trash" style="font-size: 2.5rem"></i>
          </div>

          <h3 class="text-2xl font-black text-slate-900 tracking-tight mb-2">
            Delete {{ type === 'subscription' ? 'Plan' : 'Reminder' }}?
          </h3>

          <p class="text-slate-500 font-medium leading-relaxed mb-8">
            Are you sure you want to remove
            <span class="text-slate-900 font-bold">"{{ title }}"</span>? This action cannot be
            undone.
          </p>

          <div class="grid grid-cols-1 gap-3">
            <button
              @click="$emit('confirm')"
              class="w-full py-4 bg-rose-600 text-white rounded-2xl font-bold hover:bg-rose-700 shadow-lg shadow-rose-100 active:scale-95 transition-all cursor-pointer"
            >
              Yes, delete it
            </button>
            <button
              @click="$emit('close')"
              class="w-full py-3 bg-white text-slate-400 rounded-2xl font-bold hover:text-slate-600 transition-all cursor-pointer text-sm"
            >
              No, keep it
            </button>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
defineProps({
  isOpen: Boolean,
  title: String,
  type: {
    type: String,
    default: 'subscription',
  },
})
defineEmits(['close', 'confirm'])
</script>

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
</style>
