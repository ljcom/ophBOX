import type { OphConnectionConfig } from '../types/domain'

export type MetadataChange = {
  config: OphConnectionConfig
  databaseName: string
  sourceTable: string
}

const listeners = new Set<(change: MetadataChange) => Promise<void>>()

export function subscribeMetadataChanges(listener: (change: MetadataChange) => Promise<void>) {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}

export async function notifyMetadataChange(change: MetadataChange) {
  // A refresh failure must not turn a committed write into an apparent save failure.
  await Promise.allSettled([...listeners].map((listener) => listener(change)))
}
