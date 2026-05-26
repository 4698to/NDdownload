const STORAGE_KEY = 'ndtools-data-key'

let dataKey = sessionStorage.getItem(STORAGE_KEY) || ''

type Listener = () => void
const listeners = new Set<Listener>()

export function getDataKey(): string {
  return dataKey
}

export function hasDataKey(): boolean {
  return dataKey.trim().length > 0
}

export function setDataKey(key: string): void {
  dataKey = key.trim()
  if (dataKey) {
    sessionStorage.setItem(STORAGE_KEY, dataKey)
  } else {
    sessionStorage.removeItem(STORAGE_KEY)
  }
  listeners.forEach(fn => fn())
}

export function clearDataKey(): void {
  setDataKey('')
}

export function onDataKeyChange(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
