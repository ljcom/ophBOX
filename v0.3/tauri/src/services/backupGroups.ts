import type { MetadataRow } from '../types/domain'

const schedules = ['Hourly', 'Daily', 'Weekly', 'Monthly', 'Yearly', 'Other'] as const

export function groupBackups(backups: MetadataRow[]) {
  return schedules.map((schedule) => ({
    schedule,
    backups: backups.filter((backup) => {
      const tokens = String(backup.backupFile ?? '').toLowerCase().split(/[^a-z0-9]+/)
      const matched = schedules.find((candidate) => tokens.includes(candidate.toLowerCase())) ?? 'Other'
      return matched === schedule
    }).sort((a, b) => String(b.lastModified ?? '').localeCompare(String(a.lastModified ?? ''))),
  })).filter((group) => group.backups.length > 0)
}
