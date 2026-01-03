export interface Reminder {
  id: string
  daysBeforeRenewal: number
  notifyTime: string
  nextReminder: string
}

export interface Subscription {
  id: string
  name: string
  cost: number
  currency: string
  plan: string
  customPeriodInDays: number | null
  startDate: string
  renewalDate: string
  daysLeft: number
  isMuted: boolean
  notes?: string
  reminders?: Reminder[]
}
