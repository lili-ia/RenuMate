export const localNotifyTime = (utcTime: string) => {
  const date = new Date('January 06, 1990 ' + utcTime + ' GMT+00:00')
  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

export const formatDateTime = (isoString: string, dateOnly: boolean) => {
  if (!isoString) return ''

  const date = new Date(isoString)

  if (dateOnly) {
    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    })
      .format(date)
      .replace(',', '')
  }

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

export const formatForInput = (isoString: string) => {
  if (!isoString) return ''
  return isoString.split('T')[0]
}
