<template>
  <nav class="sticky top-0 z-[100] bg-white/80 backdrop-blur-lg border-b border-slate-100">
    <div class="max-w-7xl mx-auto px-4 lg:px-8">
      <div class="flex justify-between items-center h-20">
        <RouterLink to="/" class="flex items-center gap-3 group">
          <div
            class="bg-indigo-600 p-2 rounded-xl shadow-lg shadow-indigo-200 group-hover:scale-110 transition-transform duration-300"
          >
            <svg
              class="w-6 h-6 text-white"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
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
              <svg
                class="w-6 h-6"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </div>
            <div class="hidden sm:flex flex-col text-left">
              <span class="text-xs text-slate-400 font-bold uppercase tracking-wider">Account</span>
              <span class="text-sm font-bold text-slate-700">{{ userName }}</span>
            </div>
          </RouterLink>

          <div class="w-px h-8 bg-slate-100 mx-2"></div>

          <button
            @click="$emit('logout')"
            class="p-3 text-slate-400 hover:text-red-500 hover:bg-red-50 rounded-xl transition-all duration-200 cursor-pointer group"
            title="Logout"
          >
            <svg
              class="w-6 h-6 group-hover:translate-x-1 transition-transform"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup>
defineProps({
  userName: {
    type: String,
    default: 'User',
  },
})

defineEmits(['logout'])
</script>
