import { ref, computed } from 'vue'
import api from '@/api'
import { formatDate } from '@/utils/formatters.ts'
import type { Subscription } from '@/types'

interface SubscriptionForm {
  id?: string
  name: string
  cost: string | number
  currency: string
  plan: string
  customPeriodInDays: number | null
  startDate: string
  notes: string
  isMuted: boolean
}

export function useSubscriptions() {
  const subscriptions = ref<Subscription[]>([])
  const totalCost = ref<number>(0)
  const selectedCurrency = ref<string>('USD')
  const selectedPeriod = ref<string>('monthly')
  const selectedSubscription = ref<Subscription | null>(null)
  const showSubscriptionModal = ref(false)
  const editingSubscription = ref<Subscription | null>(null)

  const popularCurrencies = [
    { code: 'USD', name: 'US Dollar', symbol: '$' },
    { code: 'EUR', name: 'Euro', symbol: '€' },
    { code: 'UAH', name: 'Hryvnia', symbol: '₴' },
    { code: 'PLN', name: 'Złoty', symbol: 'zł' },
    { code: 'GBP', name: 'Pound', symbol: '£' },
    { code: 'JPY', name: 'Yen', symbol: '¥' },
  ]

  const formData = ref<SubscriptionForm>({
    name: '',
    cost: '',
    currency: 'UAH',
    plan: 'Monthly',
    customPeriodInDays: null,
    startDate: '',
    notes: '',
    isMuted: false,
  })

  const activeSubscriptionsCount = computed(() => {
    return subscriptions.value.filter((s) => !s.isMuted).length
  })

  const loadSubscriptions = async () => {
    try {
      const response = await api.get('/subscriptions')
      subscriptions.value = response.data.items

      console.log('Subscriptions loaded successfully.')
    } catch (error) {
      console.error('Error loading subscriptions:', error)
      subscriptions.value = []
    }
  }

  const editSubscription = (sub: Subscription) => {
    formData.value = {
      id: sub.id,
      name: sub.name,
      cost: sub.cost,
      currency: sub.currency,
      plan: sub.plan,
      customPeriodInDays: sub.customPeriodInDays || null,
      startDate: formatDate(sub.startDate),
      notes: sub.notes ?? '',
      isMuted: sub.isMuted,
    }

    editingSubscription.value = sub
    showSubscriptionModal.value = true
  }

  const calculatedNextRenewalDate = computed(() => {
    if (!formData.value.startDate || !formData.value.plan) return ''

    const start = new Date(formData.value.startDate)
    let nextDate = new Date(start)
    const today = new Date()
    today.setHours(0, 0, 0, 0)

    const plan = formData.value.plan.toLowerCase()

    const addPeriod = (date: Date) => {
      const d = new Date(date)
      if (plan === 'monthly') d.setMonth(d.getMonth() + 1)
      else if (plan === 'quarterly') d.setMonth(d.getMonth() + 3)
      else if (plan === 'annual') d.setFullYear(d.getFullYear() + 1)
      else if (plan === 'custom' && formData.value.customPeriodInDays) {
        d.setDate(d.getDate() + formData.value.customPeriodInDays)
      }
      return d
    }

    while (nextDate <= today) {
      const prevTime = nextDate.getTime()
      nextDate = addPeriod(nextDate)

      if (nextDate.getTime() <= prevTime) break
    }

    return nextDate.toISOString().split('T')[0]
  })

  const fetchTotal = async () => {
    try {
      const response = await api.get('/subscriptions/total', {
        params: {
          currency: selectedCurrency.value,
          period: selectedPeriod.value,
        },
      })
      totalCost.value = response.data
    } catch (error) {
      console.error('Failed to fetch analytics', error)
    }
  }

  const resetForm = () => {
    formData.value = {
      name: '',
      cost: '',
      currency: 'UAH',
      plan: 'monthly',
      customPeriodInDays: null,
      startDate: '',
      notes: '',
      isMuted: false,
    }
    editingSubscription.value = null
    showSubscriptionModal.value = false
  }

  const handleSubmit = async () => {
    if (!formData.value.name || !formData.value.cost) {
      alert('Please fill in required fields')
      return
    }

    if (formData.value.plan === 'Custom' && !formData.value.customPeriodInDays) {
      alert('Please specify the custom period in days')
      return
    }

    const payload = {
      ...formData.value,
      customPeriodInDays:
        formData.value.plan === 'Custom' ? formData.value.customPeriodInDays : null,
    }

    try {
      if (editingSubscription.value) {
        const subId = editingSubscription.value.id
        await api.put(`/subscriptions/${subId}`, payload)
        console.log(`Subscription ${subId} updated successfully on API.`)
      } else {
        await api.post('/subscriptions', payload)
        console.log('Subscription created successfully on API.')
      }

      await loadSubscriptions()
    } catch (error: any) {
      if (error.response) {
        const { status, data } = error.response
        if (status === 403) {
          alert('Similar subscription already exists.')
        }
        console.log(data)
      }
      alert('Failed to save subscription. Check the console.')
      return
    }

    resetForm()
  }

  const deleteSubscription = async (id: string) => {
    if (!confirm('Are you sure you want to delete this subscription?')) {
      return
    }

    try {
      await api.delete(`/subscriptions/${id}`)
      console.log(`Subscription ${id} deleted successfully from API. Reloading data...`)
      await loadSubscriptions()
    } catch (error) {
      console.error('Error deleting subscription via API:', error)
      alert('Failed to delete subscription. Please try again.')
    }
  }

  const toggleActive = async (id: string) => {
    const sub = subscriptions.value.find((s) => s.id === id)
    if (!sub) return

    const newMuteStatus = !sub.isMuted

    try {
      await api.patch(`/subscriptions/${id}`, {
        isMuted: newMuteStatus,
      })

      sub.isMuted = newMuteStatus

      console.log(`Subscription ${id} is now ${newMuteStatus ? 'Muted' : 'Active'}`)
    } catch (error) {
      console.error('Error toggling mute status:', error)
      alert('Failed to update status. Please try again.')
    }
  }

  const totalReminders = computed(() => {
    return subscriptions.value.reduce((sum, s) => sum + (s.reminders?.length || 0), 0)
  })

  const openAddSubscriptionModal = () => {
    showSubscriptionModal.value = true
  }

  return {
    subscriptions,
    resetForm,
    popularCurrencies,
    activeSubscriptionsCount,
    loadSubscriptions,
    fetchTotal,
    selectedCurrency,
    selectedPeriod,
    totalCost,
    formData,
    calculatedNextRenewalDate,
    editingSubscription,
    showSubscriptionModal,
    editSubscription,
    deleteSubscription,
    toggleActive,
    handleSubmit,
    totalReminders,
    openAddSubscriptionModal,
    selectedSubscription,
  }
}
