<script setup>
defineProps(['name', 'cost', 'currency', 'isMuted'])
defineEmits(['toggle-mute'])
</script>

<template>
  <div
    class="bg-white rounded-lg shadow p-6 border-l-4"
    :class="isMuted ? 'border-gray-300' : 'border-green-500'"
  >
    <div class="flex justify-between items-start">
      <div>
        <h3 class="font-bold text-lg" :class="{ 'text-gray-400': isMuted }">{{ name }}</h3>
        <p class="text-2xl font-bold">{{ cost }} {{ currency }}</p>
      </div>
      <button @click="$emit('toggle-mute')" class="text-sm px-3 py-1 rounded border">
        {{ isMuted ? 'Unmute' : 'Mute' }}
      </button>
    </div>
  </div>
</template>
