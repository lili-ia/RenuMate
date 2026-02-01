import { ref, computed, watch } from 'vue'
import api from '@/api'
import type { PaginatedResponse, Subscription, Tag } from '@/types'
import { toast } from 'vue3-toastify'

interface SubscriptionForm {
  id?: string
  name: string
  cost: string | number
  currency: string
  plan: string
  customPeriodInDays: number | null
  trialPeriodInDays: number | null
  startDate?: Date | null
  tags: Tag[]
  note: string
  cancelLink: string
  picLink: string
  isMuted: boolean
}

export function useSubscriptions() {
  const loading = ref<Boolean>(true)
  const summaryLoading = ref<Boolean>(true)

  const isSubmitting = ref<Boolean>(false)

  const subscriptions = ref<Subscription[]>([])
  const selectedCurrency = ref<string>('USD')
  const selectedPeriod = ref<string>('monthly')
  const selectedSubscription = ref<Subscription | null>(null)
  const showSubscriptionModal = ref<Boolean>(false)
  const editingSubscription = ref<Subscription | null>(null)
  const showDeleteConfirm = ref<Boolean>(false)
  const subToDelete = ref<{ id: string; name: string } | null>(null)
  
  const currentPage = ref<number>(1)
  const pageSize = ref<number>(10)
  const totalCount = ref<number>(0)
  const totalPages = ref<number>(0)

  const totalCost = ref<number>(0)
  const activeSubscriptionsCount = ref<number>(0)
  const activeRemindersCount = ref<number>(0)
  const projectedCost = ref<number>(0)

  const sortConfig = ref<string>('createdAt_desc'); 

  watch([sortConfig], () => {
    currentPage.value = 1;
    loadSubscriptions();
  });

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

  const openDeleteModal = (sub: any) => {
    subToDelete.value = { id: sub.id, name: sub.name }
    showDeleteConfirm.value = true
  }

  const formData = ref<SubscriptionForm>({
    name: '',
    cost: '',
    currency: 'UAH',
    plan: 'Monthly',
    customPeriodInDays: null,
    trialPeriodInDays: null,
    startDate: null,
    tags: [],
    note: '',
    cancelLink: '',
    picLink: '',
    isMuted: false,
  })

  const loadSubscriptions = async () => {
    loading.value = true
    try {
      const [sortBy, sortOrder] = sortConfig.value.split('_');
    
      const params = {
        page: currentPage.value,
        pageSize: pageSize.value,
        sortBy: sortBy,
        sortOrder: sortOrder
      };

      const response = await api.get<PaginatedResponse<Subscription>>('/subscriptions', { params })
      
      subscriptions.value = response.data.items
      totalCount.value = response.data.totalCount
      totalPages.value = response.data.totalPages
      currentPage.value = response.data.page

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
      trialPeriodInDays: sub.plan === 'Trial' ? sub.customPeriodInDays || null : null,
      startDate: sub.startDate ? new Date(sub.startDate) : null,
      tags: sub.tags || [],
      note: sub.note ?? '',
      cancelLink: sub.cancelLink ?? '',
      picLink: sub.picLink ?? '',
      isMuted: sub.isMuted,
    }
    editingSubscription.value = sub
    showSubscriptionModal.value = true
  }

  const calculatedNextRenewalDate = computed(() => {
    if (!formData.value.startDate || !formData.value.plan) return ''

    const plan = formData.value.plan.toLowerCase()

    const start = new Date(formData.value.startDate)
    start.setHours(0, 0, 0, 0)

    const now = new Date()
    now.setHours(0, 0, 0, 0)

    const next = start

    if (plan === 'trial') {
      const trialDays = formData.value.trialPeriodInDays || 7
      next.setDate(next.getDate() + trialDays)
      console.log('Trial period, next date set to:', next)
    } else {
      while (next <= now) {
        if (plan === 'monthly') {
          next.setMonth(next.getMonth() + 1)
        } else if (plan === 'quarterly') {
          next.setMonth(next.getMonth() + 3)
        } else if (plan === 'annual') {
          next.setFullYear(next.getFullYear() + 1)
        } else if (plan === 'custom' && formData.value.customPeriodInDays) {
          next.setDate(next.getDate() + formData.value.customPeriodInDays)
        }
      }
    }

    const year = next.getFullYear()
    const month = String(next.getMonth() + 1).padStart(2, '0')
    const day = String(next.getDate()).padStart(2, '0')

    return `${year}-${month}-${day}`
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
      activeRemindersCount.value = response.data.activeRemindersCount
      projectedCost.value = response.data.projectedCost
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
      trialPeriodInDays: null,
      startDate: new Date(),
      tags: [],
      note: '',
      cancelLink: '',
      picLink: '',
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
      startDate: formData.value.startDate || new Date(),
      customPeriodInDays:
        formData.value.plan === 'Custom' ? formData.value.customPeriodInDays : null,
      tagIds: formData.value.tags.map(t => t.id) || [],
      tags: undefined
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

  const confirmDelete = async () => {
    if (!subToDelete.value) return

    try {
      const id = subToDelete.value.id
      await api.delete(`/subscriptions/${id}`)

      toast.info('Subscription deleted')

      showDeleteConfirm.value = false
      subToDelete.value = null

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
      await fetchSummary()
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
    activeRemindersCount,
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
    confirmDelete,
    toggleActive,
    handleSubmit,
    openAddSubscriptionModal,
    selectedSubscription,
    formErrors,
    openDeleteModal,
    showDeleteConfirm,
    subToDelete,
    projectedCost,
    sortConfig,
  }
}
