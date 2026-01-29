export interface Reminder {
  id: string
  daysBeforeRenewal: number
  notifyTime: string
  nextReminder: string
  isSent: boolean
}

export interface Subscription {
  id: string
  name: string
  cost: number
  currency: string
  plan: string
  customPeriodInDays: number | null
  startDate: string
  renewalDate: Date
  daysLeft: number
  isMuted: boolean
  cancelLink?: string
  picLink?: string
  note?: string
  reminders?: Reminder[]
}

export interface PaginatedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
