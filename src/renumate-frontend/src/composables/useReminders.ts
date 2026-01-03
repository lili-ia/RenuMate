import { ref, type Ref } from 'vue'
import api from '@/api'
import type { Subscription } from '@/types'

export function useReminders(
  loadSubscriptions: () => Promise<void>,
  selectedSubscription: Ref<Subscription | null>,
) {
  const showReminderModal = ref(false)

  const reminderForm = ref({
    daysBeforeRenewal: '',
    notifyTime: '09:00',
  })

  const openReminderModal = (sub: Subscription) => {
    console.log('Opening reminder modal for subscription:', sub)
    selectedSubscription.value = sub
    console.log('Selected subscription set to:', selectedSubscription.value)
    showReminderModal.value = true
    console.log('Reminder modal is now' + (showReminderModal.value ? ' open' : ' closed'))
  }

  const handleAddReminder = async () => {
    if (!reminderForm.value.daysBeforeRenewal) {
      alert('Please enter days before renewal')
      return
    }

    if (!selectedSubscription.value) {
      alert('Please select a subscription')
      return
    }

    const payload = {
      subscriptionId: selectedSubscription.value.id,
      daysBeforeRenewal: parseInt(reminderForm.value.daysBeforeRenewal.toString()),
      notifyTime: reminderForm.value.notifyTime,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    }

    console.log('Adding reminder with payload:', payload)

    try {
      await api.post('/reminders', payload)
      console.log('Reminder successfully created on API. Reloading subscriptions...')
      await loadSubscriptions()
      closeReminderModal()
    } catch (error: any) {
      if (error.response) {
        const { status, data } = error.response
        console.log(data)
      }
      alert('Failed to add reminder. Please try again.')
    }
  }

  const deleteReminder = async (reminderId: string) => {
    if (!confirm('Are you sure you want to delete this reminder?')) {
      return
    }

    try {
      await api.delete(`/reminders/${reminderId}`)
      console.log(
        `Reminder ${reminderId} deleted successfully from API. Reloading subscriptions...`,
      )
      await loadSubscriptions()
    } catch (error) {
      console.error('Error deleting reminder via API:', error)
      alert('Failed to delete reminder. Please try again.')
    }
  }

  const closeReminderModal = () => {
    showReminderModal.value = false
    reminderForm.value = { daysBeforeRenewal: '', notifyTime: '09:00' }
  }

  return {
    showReminderModal,
    reminderForm,
    openReminderModal,
    handleAddReminder,
    deleteReminder,
    closeReminderModal,
  }
}
