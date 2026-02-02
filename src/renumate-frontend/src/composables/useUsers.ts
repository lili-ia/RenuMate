import { ref, type Ref } from 'vue'
import api from '@/api'
import { toast, type ToastOptions } from 'vue3-toastify'
import 'vue3-toastify/dist/index.css'
import { auth0 } from '@/main';

export function useUsers() {
  const user: Ref<any> = ref(null)

  const toastConfig: ToastOptions = {
    autoClose: 3000,
    position: 'bottom-right',
  }

  const isProcessing = ref(false)
  const isSent = ref(false)
  const isAlreadyActive = ref(false)
  
  const logout = () => {
    auth0.logout({ logoutParams: { returnTo: window.location.origin } })
  }

  const fetchUserInfo = async () => {
    try {
      const response = await api.get('/users/me')
      user.value = response.data
      return response.data
    } catch (error) {
      toast.error('Failed to fetch user data')
    }
  }

  const handleDeactivate = async () => {
    try {
      await api.delete('/users/me')
      logout()
    } catch (error) {
      toast.error('Failed to deactivate account', toastConfig)
    }
  }

  const onReactivateRequest = async () => {
    isProcessing.value = true
    try {
      await api.post('/users/reactivate-request')

      isSent.value = true
      toast.success('Reactivation email sent! Please check your inbox.')
    } catch (error: any) {
      console.error('Request failed:', error)

      if (error.response?.status === 502) {
        toast.warning('Email service is busy. We queued your request.')
        isSent.value = true
      } else if (error.response?.status === 409) {
        isAlreadyActive.value = true
        toast.info('Your account is already active.')
      } else {
        toast.error('Something went wrong. Please try again later.')
      }
    } finally {
      isProcessing.value = false
    }
  }

  const status = ref<'loading' | 'success' | 'error'>('loading')
  const countdown = ref(5)

  const confirmAccountActivation = async (token: string) => {
    try {
      await api.patch('/users/me', { token })

      status.value = 'success'

      try {
        await auth0.checkSession()
      } catch (e) {
        console.warn('CheckSession failed, proceeding anyway', e)
      }

      return true
    } catch (error: any) {
      status.value = 'error'
      const message = error.response?.data?.detail || 'Failed to verify token'
      toast.error(message)
      return false
    }
  }

  const syncUserWithDatabase = async () => {
    try {
      const response = await api.post('/users/sync-user', {})

      const mustBeUpdated = response.data.message.includes('created') || 
                        response.data.message.includes('successfully synced')

      if (mustBeUpdated) {
        console.log('User synced on backend.')
        
        try {
          await auth0.getAccessTokenSilently({cacheMode: 'off'}); 
          console.log('Token claims updated.')
        } catch (tokenError) {
          console.warn('Silent token refresh skipped on first login.')
        }
      }
    } catch (error) {
      console.error('Failed to sync user:', error)
      throw error 
    }
  }

  const resendEmail = async () => {
    try {
      await api.post('/users/resend-verification')
      toast.success('Check your inbox!')
    } catch (error) {
      toast.error('An internal error occurred. Please try again later.')
    }
  }

  const getActiveStatus = async () => {
    try {
      const response = await api.get('/users/status')
      return response.data.isActive
    } catch (error) {
      toast.error('An internal error occurred. Please try again later.')
    }
  }

  return { 
    fetchUserInfo, 
    user, 
    handleDeactivate, 
    onReactivateRequest, 
    isProcessing, 
    isAlreadyActive, 
    isSent,
    confirmAccountActivation,
    status,
    countdown,
    syncUserWithDatabase,
    resendEmail,
    getActiveStatus
  }
}
