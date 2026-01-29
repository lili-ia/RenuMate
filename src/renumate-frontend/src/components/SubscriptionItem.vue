<script setup>
const props = defineProps({
  sub: {
    type: Object,
    required: true,
  },
  formatDateTime: {
    type: Function,
    required: true,
  },
  formatTime: {
    type: Function,
    required: true,
  },
})

const emit = defineEmits(['edit', 'delete', 'toggle-mute', 'add-reminder', 'delete-reminder'])
import IconActive from './icons/IconActive.vue'
import IconMuted from './icons/IconMuted.vue'
import IconEdit from './icons/IconEdit.vue'
import IconTrash from './icons/IconTrash.vue'
import ReminderItem from './ReminderItem.vue'
</script>

<template>
  <div
    :class="[
      'relative overflow-hidden transition-all duration-300 transform hover:-translate-y-1',
      'bg-white rounded-[1.5rem] p-5 shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-slate-50',
      sub.isMuted ? 'opacity-70 grayscale-[0.5]' : 'hover:shadow-[0_20px_40px_rgba(0,0,0,0.08)]',
    ]"
  >
    <div class="relative z-10">
      <div class="flex gap-4 items-center mb-5">
        <div class="relative group">
          <div v-if="sub.picLink" class="relative z-10">
            <img
              :src="sub.picLink"
              alt="Sub logo"
              class="w-14 h-14 rounded-2xl object-cover shadow-sm border border-white transition-transform group-hover:scale-105"
            />
          </div>
          <div
            v-else
            class="w-14 h-14 rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-bold text-xl shadow-inner"
          >
            {{ sub.name.charAt(0).toUpperCase() }}
          </div>
          <div
            v-if="sub.plan === 'Trial'"
            class="absolute -top-2 -right-2 bg-amber-400 z-20 text-white text-[10px] font-black px-1.5 py-0.5 rounded-lg shadow-sm border-2 border-white uppercase tracking-tighter"
          >
            Trial
          </div>
        </div>

        <div class="flex-1 min-w-0">
          <h3 class="text-lg font-black text-slate-900 truncate tracking-tight">{{ sub.name }}</h3>
          <div class="flex items-baseline gap-1">
            <span class="text-xl font-bold text-indigo-600 tracking-tight">
              {{ sub.cost }} <span class="text-sm font-semibold uppercase">{{ sub.currency }}</span>
            </span>
            <span class="text-xs font-bold text-slate-400 uppercase tracking-widest"
              >/ {{ sub.plan }}</span
            >
          </div>
        </div>

        <div class="flex gap-1 self-start">
          <button
            @click="$emit('edit', sub)"
            class="p-2 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-xl transition-all cursor-pointer"
            title="Edit"
          >
            <IconEdit class="w-5 h-5" />
          </button>
          <button
            @click="$emit('delete', sub.id)"
            class="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-xl transition-all cursor-pointer"
            title="Delete"
          >
            <IconTrash class="w-5 h-5" />
          </button>
        </div>
      </div>

      <div
        class="grid grid-cols-3 gap-2 bg-slate-50/80 rounded-2xl p-3 mb-4 border border-slate-100/50"
      >
        <div class="text-center border-r border-slate-200">
          <p class="text-[10px] text-slate-400 uppercase font-black tracking-widest mb-0.5">
            Start
          </p>
          <p class="text-xs font-bold text-slate-700">
            {{ props.formatDateTime(sub.startDate, true) }}
          </p>
        </div>
        <div class="text-center border-r border-slate-200">
          <p class="text-[10px] text-slate-400 uppercase font-black tracking-widest mb-0.5">
            Renewal
          </p>
          <p class="text-xs font-bold text-slate-700">
            {{ props.formatDateTime(sub.renewalDate, true) }}
          </p>
        </div>
        <div class="text-center">
          <p class="text-[10px] text-slate-400 uppercase font-black tracking-widest mb-0.5">Left</p>
          <p
            :class="[
              'text-xs font-black',
              sub.daysLeft < 3 ? 'text-red-500 animate-pulse' : 'text-indigo-600',
            ]"
          >
            {{ sub.daysLeft }}d
          </p>
        </div>
      </div>

      <p v-if="sub.note" class="text-sm text-slate-500 mb-4 px-1 line-clamp-2 italic font-medium">
        "{{ sub.note }}"
      </p>

      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <button
            @click="$emit('toggle-mute', sub.id)"
            :class="[
              'group flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer',
              !sub.isMuted
                ? 'bg-green-50 text-green-600 hover:bg-green-100'
                : 'bg-slate-100 text-slate-400 hover:bg-slate-200',
            ]"
          >
            <component
              :is="sub.isMuted ? IconMuted : IconActive"
              class="w-3.5 h-3.5 transition-transform group-hover:scale-110"
            />
            {{ !sub.isMuted ? 'Active' : 'Muted' }}
          </button>

          <button
            @click="$emit('add-reminder', sub)"
            class="px-3 py-1.5 bg-indigo-50 text-indigo-600 hover:bg-indigo-600 hover:text-white rounded-xl text-xs font-bold transition-all cursor-pointer"
          >
            + Reminder
          </button>
        </div>

        <div class="flex items-center gap-1.5 bg-slate-100/50 px-2 py-1 rounded-lg">
          <span class="w-1.5 h-1.5 rounded-full bg-indigo-400"></span>
          <span class="text-[10px] font-black text-slate-500 uppercase tracking-tighter">
            {{ sub.reminders?.length || 0 }} Reminders
          </span>
        </div>
      </div>

      <TransitionGroup
        name="list"
        tag="div"
        v-if="sub.reminders?.length > 0"
        class="mt-4 pt-4 border-t border-slate-100 space-y-2"
      >
        <ReminderItem
          v-for="reminder in sub.reminders"
          :key="reminder.id"
          :reminder="reminder"
          :format-date-time="props.formatDateTime"
          :format-time="formatTime"
          @delete="$emit('delete-reminder', reminder)"
        />
      </TransitionGroup>
    </div>
  </div>
</template>

<style scoped>
.list-enter-active,
.list-leave-active {
  transition: all 0.3s ease;
}
.list-enter-from,
.list-leave-to {
  opacity: 0;
  transform: translateX(-10px);
}

/* */
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
