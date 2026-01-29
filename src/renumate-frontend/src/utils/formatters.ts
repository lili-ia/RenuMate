export const localNotifyTime = (utcTime: string) => {
  const date = new Date('January 06, 1990 ' + utcTime + ' GMT+00:00')
  return date.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

export const formatDateTime = (dateStr: string, dateOnly: boolean) => {
  if (!dateStr) return ''

  if (dateOnly && dateStr.length === 10) {
    const [year, month, day] = dateStr.split('-');
    return `${day}/${month}/${year}`;
  }

  const date = new Date(dateStr)
  
  if (isNaN(date.getTime())) return dateStr;

  const options: Intl.DateTimeFormatOptions = dateOnly 
    ? { day: '2-digit', month: '2-digit', year: 'numeric' }
    : { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false };

  return new Intl.DateTimeFormat('en-GB', options).format(date).replace(',', '');
}

export const formatForInput = (dateStr: string) => {
  if (!dateStr) return ''
  
  return dateStr.includes('T') ? dateStr.split('T')[0] : dateStr.substring(0, 10);
}