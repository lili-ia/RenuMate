export const localNotifyTime = (utcTime: string) => {
  const date = new Date('January 06, 1990 ' + utcTime + ' GMT+00:00')
  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

export const formatDateTime = (isoString: string) => {
  if (!isoString) return ''

  const date = new Date(isoString)

  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
    .format(date)
    .replace(',', '')
}

export const formatDate = (dateString: string) => {
  if (!dateString) return ''
  return dateString.substring(0, 10)
}
