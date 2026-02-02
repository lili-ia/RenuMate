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
    :class="{ 'opacity-80 pointer-events-none': loading }"
  >


    <div class="flex items-start justify-between">
      <div class="flex flex-col min-w-0 flex-grow">
        <slot name="label"></slot>

        <div class="relative mt-2">
          <div 
            v-if="loading" 
            class="absolute inset-0 h-10 w-32 bg-slate-100 animate-pulse rounded-xl z-10"
          ></div>
          
          <div :class="{ 'invisible': loading }" class="mt-1">
            <slot name="value"></slot>

            <div
              v-if="projectedCost !== null"
              class="group relative inline-flex items-center mt-2 cursor-help"
            >
              <div class="flex items-center gap-1.5 text-[11px] font-bold tracking-tight uppercase">
                <span class="text-slate-400">Projected:</span>
                <span class="text-indigo-500 bg-indigo-50 px-2 py-0.5 rounded-lg transition-colors group-hover:bg-indigo-100">
                  {{ projectedCost }}
                </span>
              </div>
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
