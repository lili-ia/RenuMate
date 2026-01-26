import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'main',
      component: () => import('@/components/Dashboard.vue'),
      meta: { title: 'Dashboard' },
    },
    {
      path: '/profile',
      name: 'profile',
      component: () => import('@/components/UserProfile.vue'),
      meta: { title: 'Me' },
    },
    {
      path: '/reactivate-request',
      name: 'reactivate-request',
      component: () => import('@/components/RequestAccountReactivate.vue'),
      meta: {
        title: 'Reactivate Account',
        hideNavbar: true,
      },
    },
    {
      path: '/reactivate',
      name: 'confirm-reactivation',
      component: () => import('@/components/ConfirmAccountReactivation.vue'),
      meta: {
        title: 'Confirming...',
        hideNavbar: true,
      },
      beforeEnter: (to, from, next) => {
        if (!to.query.token) {
          next({ name: 'reactivate-request' })
        } else {
          next()
        }
      },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/', 
    },
  ],
})

router.beforeEach((to, from, next) => {
  document.title = to.meta.title ? `${to.meta.title} | RenuMate` : 'RenuMate'
  next()
})

export default router
