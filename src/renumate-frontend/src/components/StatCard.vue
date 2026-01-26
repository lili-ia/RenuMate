<script setup>
defineProps({
  loading: {
    type: Boolean,
    default: false,
  },
  projectedCost: {
    type: [String, Number],
    default: null,
  },
})
</script>

<template>
  <div
    class="bg-white rounded-[2rem] shadow-sm p-6 relative overflow-visible transition-all duration-300 border border-slate-50 hover:shadow-md"
    :class="{ 'opacity-60 pointer-events-none': loading }"
  >
    <div v-if="loading" class="absolute top-0 left-0 w-full h-1 bg-slate-50 overflow-hidden">
      <div class="w-full h-full bg-indigo-500 animate-progress origin-left"></div>
    </div>

    <div class="flex items-start justify-between">
      <div class="flex flex-col min-w-0">
        <slot name="label"></slot>

        <div v-if="loading" class="h-10 w-32 bg-slate-100 animate-pulse rounded-xl mt-2"></div>
        <div v-else class="mt-1">
          <slot name="value"></slot>

          <div
            v-if="projectedCost !== null"
            class="group relative inline-flex items-center mt-2 cursor-help"
          >
            <div class="flex items-center gap-1.5 text-[11px] font-bold tracking-tight uppercase">
              <span class="text-slate-400">Projected:</span>
              <span
                class="text-indigo-500 bg-indigo-50 px-2 py-0.5 rounded-lg transition-colors group-hover:bg-indigo-100"
              >
                {{ projectedCost }} {{ currency }}
              </span>
            </div>

            <div
              class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-56 p-3 bg-slate-900 text-white text-[10px] font-medium leading-relaxed rounded-2xl shadow-2xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-300 z-50 pointer-events-none"
            >
              This is the estimated cost if your trial plan renews at the standard monthly rate.
            </div>
          </div>
        </div>
      </div>

      <div class="flex-shrink-0">
        <slot name="right"></slot>
      </div>
    </div>

    <slot name="footer"></slot>
  </div>
</template>

<style scoped>
@keyframes progress {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(100%);
  }
}
.animate-progress {
  animation: progress 1.5s infinite linear;
}
</style>
