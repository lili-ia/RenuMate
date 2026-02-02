import { ref, type Ref } from 'vue'
import api from '@/api'
import type { Subscription } from '@/types'
import { toast, type ToastOptions } from 'vue3-toastify'
import 'vue3-toastify/dist/index.css'

export function useReminders(
  loadSubscriptions: () => Promise<void>,
  selectedSubscription: Ref<Subscription | null>,
  fetchSummary: () => Promise<void>,
) {
  const showReminderModal = ref(false)
  const openReminderModal = (sub: Subscription) => {
    selectedSubscription.value = sub
    showReminderModal.value = true
  }
  
  const isSubmitting = ref(false)
  
  const reminderErrors = ref<Record<string, string[]>>({})
  const clearErrors = () => {
    reminderErrors.value = {}
  }

  const reminderForm = ref({
    daysBeforeRenewal: '',
    notifyTime: '09:00',
  })

  const toastConfig: ToastOptions = {
    autoClose: 3000,
    position: 'bottom-right',
  }
  
  const showDeleteReminderConfirm = ref(false)
  const reminderToDelete = ref<{ id: string; info: string } | null>(null)
  
  const openDeleteReminderModal = (reminder: any) => {
    reminderToDelete.value = {
      id: reminder.id,
      info: `${reminder.daysBeforeRenewal} days before`,
    }
    showDeleteReminderConfirm.value = true
  }

  const confirmDeleteReminder = async () => {
    if (!reminderToDelete.value) return

    try {
      await deleteReminder(reminderToDelete.value.id) 
      showDeleteReminderConfirm.value = false
      reminderToDelete.value = null
    } catch (error) {
      console.error(error)
    }
  }
  
  const handleAddReminder = async () => {
    if (!reminderForm.value.daysBeforeRenewal) {
      toast.warn('Please enter days before renewal', toastConfig)
      return
    }

    if (!selectedSubscription.value) {
      toast.error('No subscription selected', toastConfig)
      return
    }

    clearErrors()

    const payload = {
      subscriptionId: selectedSubscription.value.id,
      daysBeforeRenewal: parseInt(reminderForm.value.daysBeforeRenewal.toString()),
      notifyTime: reminderForm.value.notifyTime,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    }

    isSubmitting.value = true

    try {
      await api.post('/reminders', payload)
      toast.success('Reminder created successfully!', toastConfig)
      await loadSubscriptions()
      await fetchSummary()
      closeReminderModal()
    } catch (error: any) {
      if (error.response?.status === 400 && error.response.data?.errors) {
        reminderErrors.value = error.response.data.errors
        toast.error('Please correct the highlighted errors.')
      } else {
        const errorMessage =
          error.response?.data?.detail || error.response?.data?.title || 'An error occurred'
        toast.error(errorMessage, toastConfig)
      }
    } finally {
      isSubmitting.value = false
    }
  }

  const deleteReminder = async (reminderId: string) => {
    if (!reminderToDelete.value) return

    try {
      await api.delete(`/reminders/${reminderId}`)
      toast.info('Reminder removed', toastConfig)
      await loadSubscriptions()
      await fetchSummary()
    } catch (error: any) {
      const errorMessage = error.response?.data?.detail || 'Failed to delete reminder'
      toast.error(errorMessage, toastConfig)
    }
  }

  const closeReminderModal = () => {
    showReminderModal.value = false
    reminderForm.value = { daysBeforeRenewal: '', notifyTime: '09:00' }
  }

  return {
    showReminderModal,
    reminderForm,
    isSubmitting,
    openReminderModal,
    handleAddReminder,
    deleteReminder,
    closeReminderModal,
    reminderErrors,
    confirmDeleteReminder,
    showDeleteReminderConfirm,
    openDeleteReminderModal,
    reminderToDelete,
  }
}
