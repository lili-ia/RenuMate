<template>
  <nav class="sticky top-0 z-[100] bg-white/80 backdrop-blur-lg border-b border-slate-100">
    <div class="max-w-7xl mx-auto px-4 lg:px-8">
      <div class="flex justify-between items-center h-20">
        <RouterLink to="/" class="flex items-center gap-3 group">
          <div
            class="bg-indigo-600 p-2 rounded-xl shadow-lg shadow-indigo-200 group-hover:scale-110 transition-transform duration-300"
          >
            <Icon name="logo" class="w-6 h-6 text-white" />
          </div>
          <div class="flex flex-col">
            <span class="text-xl font-black text-slate-900 leading-none tracking-tight"
              >RenuMate</span
            >
          </div>
        </RouterLink>

        <div class="flex items-center gap-2">
          <RouterLink
            to="/profile"
            class="flex items-center gap-3 p-1.5 pr-4 rounded-2xl hover:bg-slate-50 transition-all duration-200 group"
          >
            <div
              class="w-10 h-10 rounded-xl bg-slate-100 flex items-center justify-center text-slate-500 group-hover:bg-indigo-50 group-hover:text-indigo-600 transition-colors"
            >
              <i class="pi pi-user text-slate-500" style="font-size: 1.5rem"></i>
            </div>
            <div class="hidden sm:flex flex-col text-left">
              <span class="text-xs text-slate-400 font-bold uppercase tracking-wider">Account</span>
              <span class="text-sm font-bold text-slate-700">{{ userName }}</span>
            </div>
          </RouterLink>

          <div class="w-px h-8 bg-slate-100 mx-2"></div>

          <button
            @click="$emit('logout')"
            class="p-3 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-xl transition-all duration-200 cursor-pointer group flex items-center justify-center"
            title="Logout"
          >
            <i 
              class="pi pi-sign-out group-hover:translate-x-1 transition-transform" 
              style="font-size: 1.5rem" 
            ></i>
          </button>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
import Icon from '@/Icon.vue';

defineProps({
  userName: {
    type: String,
    default: 'User',
  },
})

defineEmits(['logout'])
</script>
