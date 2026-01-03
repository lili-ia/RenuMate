<script setup>
defineProps(['sub', 'formatDate', 'formatTime'])
defineEmits(['edit', 'delete', 'toggle-mute', 'add-reminder', 'delete-reminder'])
import IconActive from './icons/IconActive.vue'
import IconMuted from './icons/IconMuted.vue'
import IconEdit from './icons/IconEdit.vue'
import IconTrash from './icons/IconTrash.vue'
import ReminderItem from './ReminderItem.vue'
</script>

<template>
  <div :class="['border rounded-lg p-4', sub.isMuted && 'opacity-50']">
    <div class="flex justify-between items-start mb-3">
      <div class="flex-1">
        <h3 class="text-lg font-bold text-gray-800">{{ sub.name }}</h3>
        <p class="text-2xl font-bold text-indigo-600 mt-1">
          {{ sub.cost }} {{ sub.currency }}
          <span class="text-sm text-gray-600 font-normal ml-2">/ {{ sub.plan }}</span>
        </p>
      </div>
      <div class="flex gap-2">
        <button @click="$emit('edit', sub)" class="p-2 text-blue-600 hover:bg-blue-50 rounded">
          <IconEdit class="w-4 h-4" />
        </button>
        <button @click="$emit('delete', sub.id)" class="p-2 text-red-600 hover:bg-red-50 rounded">
          <IconTrash class="w-4 h-4" />
        </button>
      </div>
    </div>

    <div class="grid grid-cols-3 gap-4 text-sm mb-3">
      <div>
        <p class="text-gray-600">Start Date</p>
        <p class="font-semibold">{{ formatDate(sub.startDate) }}</p>
      </div>
      <div>
        <p class="text-gray-600">Next Renewal</p>
        <p class="font-semibold">{{ formatDate(sub.renewalDate) }}</p>
      </div>
      <div>
        <p class="text-gray-600">Days Left</p>
        <p class="font-semibold">{{ sub.daysLeft }}</p>
      </div>
    </div>

    <p v-if="sub.notes" class="text-sm text-gray-600 mb-3">{{ sub.notes }}</p>

    <div class="flex items-center justify-between pt-3 border-t">
      <div class="flex items-center gap-4">
        <button
          @click="$emit('toggle-mute', sub.id)"
          :class="[
            'flex items-center gap-1 text-sm font-semibold transition-colors duration-200',
            !sub.isMuted
              ? 'text-green-600 hover:text-green-700'
              : 'text-gray-400 hover:text-gray-600',
          ]"
        >
          <IconMuted v-if="sub.isMuted" />
          <IconActive v-else />
          <span>{{ !sub.isMuted ? 'Active' : 'Muted' }}</span>
        </button>
        <button
          @click="$emit('add-reminder', sub)"
          class="flex items-center gap-1 text-sm text-indigo-600 hover:text-indigo-700"
        >
          Add Reminder
        </button>
      </div>
      <span class="text-sm text-gray-600">{{ sub.reminders?.length || 0 }} reminder(s)</span>
    </div>

    <div v-if="sub.reminders?.length > 0" class="mt-4 pt-4 border-t space-y-2">
      <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Reminders</p>

      <ReminderItem
        v-for="reminder in sub.reminders"
        :key="reminder.reminderId"
        :reminder="reminder"
        :format-date="formatDate"
        :format-time="formatTime"
        @delete="$emit('delete-reminder', reminder.id)"
      />
    </div>
  </div>
</template>
