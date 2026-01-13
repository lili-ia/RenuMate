import { ref, computed } from 'vue'
import api from '@/api'
import { formatDate } from '@/utils/formatters.ts'
import type { PaginatedResponse, Subscription } from '@/types'
import { toast } from 'vue3-toastify'

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
  const loading = ref(true)
  const summaryLoading = ref(true)

  const subscriptions = ref<Subscription[]>([])
  const totalCost = ref<number>(0)
  const selectedCurrency = ref<string>('USD')
  const selectedPeriod = ref<string>('monthly')
  const selectedSubscription = ref<Subscription | null>(null)
  const showSubscriptionModal = ref(false)
  const editingSubscription = ref<Subscription | null>(null)
  const isSubmitting = ref(false)

  const currentPage = ref(1)
  const pageSize = ref(10)
  const totalCount = ref(0)
  const totalPages = ref(0)
  const activeSubscriptionsCount = ref(0)
  const totalRemindersCount = ref(0)

  const popularCurrencies = [
    { code: 'USD', name: 'US Dollar', symbol: '$' },
    { code: 'EUR', name: 'Euro', symbol: '€' },
    { code: 'UAH', name: 'Hryvnia', symbol: '₴' },
    { code: 'PLN', name: 'Złoty', symbol: 'zł' },
    { code: 'GBP', name: 'British Pound', symbol: '£' },
    { code: 'JPY', name: 'Japanese Yen', symbol: '¥' },
    { code: 'CAD', name: 'Canadian Dollar', symbol: 'CA$' },
    { code: 'AUD', name: 'Australian Dollar', symbol: 'A$' },
    { code: 'CHF', name: 'Swiss Franc', symbol: 'CHF' },
    { code: 'CNY', name: 'Chinese Yuan', symbol: '¥' },
    { code: 'INR', name: 'Indian Rupee', symbol: '₹' },
    { code: 'BRL', name: 'Brazilian Real', symbol: 'R$' },
    { code: 'TRY', name: 'Turkish Lira', symbol: '₺' },
    { code: 'MXN', name: 'Mexican Peso', symbol: 'Mex$' },
    { code: 'SGD', name: 'Singapore Dollar', symbol: 'S$' },
    { code: 'HKD', name: 'Hong Kong Dollar', symbol: 'HK$' },
    { code: 'SEK', name: 'Swedish Krona', symbol: 'kr' },
    { code: 'NOK', name: 'Norwegian Krone', symbol: 'kr' },
    { code: 'ILS', name: 'Israeli New Shekel', symbol: '₪' },
    { code: 'ZAR', name: 'South African Rand', symbol: 'R' },
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

  const loadSubscriptions = async () => {
    loading.value = true
    try {
      const response = await api.get<PaginatedResponse<Subscription>>('/subscriptions', {
        params: {
          page: currentPage.value,
          pageSize: pageSize.value,
        },
      })
      subscriptions.value = response.data.items
      totalCount.value = response.data.totalCount
      totalPages.value = response.data.totalPages
      currentPage.value = response.data.page
      console.log('totalPages:', totalPages.value)
    } catch (error) {
      toast.error('Failed to load subscriptions')
      subscriptions.value = []
    } finally {
      loading.value = false
    }
  }

  const setPage = (page: number) => {
    console.log('setPage called with', page)
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page
      loadSubscriptions()
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

  const fetchSummary = async () => {
    summaryLoading.value = true
    try {
      const response = await api.get('/subscriptions/summary', {
        params: {
          currency: selectedCurrency.value,
          period: selectedPeriod.value,
        },
      })
      totalCost.value = response.data.totalCost
      activeSubscriptionsCount.value = response.data.activeSubscriptionsCount
      totalRemindersCount.value = response.data.totalRemindersCount
    } catch (error) {
      toast.error('Could not calculate total cost')
    } finally {
      summaryLoading.value = false
    }
  }

  const resetForm = () => {
    formData.value = {
      name: '',
      cost: '',
      currency: 'UAH',
      plan: 'Monthly',
      customPeriodInDays: null,
      startDate: '',
      notes: '',
      isMuted: false,
    }
    editingSubscription.value = null
    showSubscriptionModal.value = false
  }

  const formErrors = ref<Record<string, string[]>>({})

  const clearErrors = () => {
    formErrors.value = {}
  }

  const handleSubmit = async () => {
    clearErrors()

    if (!formData.value.name || !formData.value.cost) {
      toast.warn('Please fill in required fields')
      return
    }

    if (formData.value.plan === 'Custom' && !formData.value.customPeriodInDays) {
      toast.warn('Please specify the custom period in days')
      return
    }

    const payload = {
      ...formData.value,
      customPeriodInDays:
        formData.value.plan === 'Custom' ? formData.value.customPeriodInDays : null,
    }

    isSubmitting.value = true
    try {
      if (editingSubscription.value) {
        await api.put(`/subscriptions/${editingSubscription.value.id}`, payload)
      } else {
        await api.post('/subscriptions', payload)
      }

      toast.success('Subscription saved successfully')
      resetForm()
      await loadSubscriptions()
      await fetchSummary()
    } catch (error: any) {
      if (error.response?.status === 400 && error.response.data?.errors) {
        formErrors.value = error.response.data.errors
        toast.error('Please correct the highlighted errors.')
      } else {
        const message = error.response?.data?.detail || 'An unexpected error occurred'
        toast.error(message)
      }
    } finally {
      isSubmitting.value = false
      loading.value = false
    }
  }

  const deleteSubscription = async (id: string) => {
    if (!confirm('Are you sure you want to delete this subscription?')) {
      return
    }

    try {
      await api.delete(`/subscriptions/${id}`)
      toast.info('Subscription deleted')
      await loadSubscriptions()
      await fetchSummary()
    } catch (error: any) {
      toast.error(error.response?.data?.detail || 'Failed to delete subscription')
    }
  }

  const toggleActive = async (id: string) => {
    const sub = subscriptions.value.find((s) => s.id === id)
    if (!sub) return

    const newMuteStatus = !sub.isMuted
    try {
      await api.patch(`/subscriptions/${id}`, { isMuted: newMuteStatus })
      sub.isMuted = newMuteStatus
      toast.success(newMuteStatus ? 'Subscription muted' : 'Subscription activated', {
        autoClose: 1500,
      })
    } catch (error) {
      toast.error('Failed to update status')
    } finally {
      loading.value = false
    }
  }

  const openAddSubscriptionModal = () => {
    showSubscriptionModal.value = true
  }

  return {
    loading,
    summaryLoading,
    subscriptions,
    currentPage,
    pageSize,
    totalPages,
    totalCount,
    totalRemindersCount,
    setPage,
    resetForm,
    popularCurrencies,
    activeSubscriptionsCount,
    loadSubscriptions,
    fetchSummary,
    selectedCurrency,
    selectedPeriod,
    totalCost,
    formData,
    calculatedNextRenewalDate,
    editingSubscription,
    showSubscriptionModal,
    isSubmitting,
    editSubscription,
    deleteSubscription,
    toggleActive,
    handleSubmit,
    openAddSubscriptionModal,
    selectedSubscription,
    formErrors,
  }
}
