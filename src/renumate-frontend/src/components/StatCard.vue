<script setup>
defineProps({
  loading: {
    type: Boolean,
    default: false,
  },
})
</script>

<template>
  <div
    class="bg-white rounded-lg shadow p-6 relative overflow-hidden transition-all duration-300"
    :class="{ 'opacity-60 pointer-events-none': loading }"
  >
    <div v-if="loading" class="absolute top-0 left-0 w-full h-1 bg-indigo-100 overflow-hidden">
      <div class="w-full h-full bg-indigo-500 animate-progress origin-left"></div>
    </div>

    <div class="flex items-center justify-between" :class="{ 'mb-4': $slots.footer }">
      <div class="flex flex-col space-y-1">
        <slot name="label"></slot>

        <div v-if="loading" class="h-9 w-24 bg-gray-200 animate-pulse rounded"></div>
        <slot v-else name="value"></slot>
      </div>

      <div class="flex flex-col items-end">
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
