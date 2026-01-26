import { ref, type Ref } from 'vue'
import api, {setAuthToken} from '@/api'
import { toast, type ToastOptions } from 'vue3-toastify'
import 'vue3-toastify/dist/index.css'
import { useAuth0 } from '@auth0/auth0-vue'

export function useUsers() {
  const user: Ref<any> = ref(null)

  const toastConfig: ToastOptions = {
    autoClose: 3000,
    position: 'top-right',
  }

  const { logout: auth0Logout, getAccessTokenSilently, checkSession } = useAuth0()

  const isProcessing = ref(false)
  const isSent = ref(false)
  const isAlreadyActive = ref(false)
  
  const logout = () => {
    auth0Logout({ logoutParams: { returnTo: window.location.origin } })
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
      toast.success('Account deactivated successfully', toastConfig)
      logout()
    } catch (error) {
      toast.error('Failed to deactivate account', toastConfig)
    }
  }

  const onReactivateRequest = async () => {
    isProcessing.value = true
    try {
      const accessToken = await getAccessTokenSilently({ ignoreCache: true })
      setAuthToken(accessToken)

      await api.post('/users/reactivate-request')

      isSent.value = true
      toast.success('Reactivation email sent! Please check your inbox.')
    } catch (error) {
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
      const accessToken = await getAccessTokenSilently({ ignoreCache: true })
      setAuthToken(accessToken)

      await api.patch('/users/me', { token })

      status.value = 'success'

      try {
        await checkSession()
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
    countdown
  }
}
