interface InfoItemProps {
  icon: React.ReactNode
  label: string
  value: string
  isEmpty?: boolean
  action?: React.ReactNode
}

/**
 * Read-only profile/detail row with an optional action slot.
 *
 * @param props.isEmpty - Applies muted placeholder styling for missing optional values.
 */
export function InfoItem({
  icon,
  label,
  value,
  isEmpty = false,
  action,
}: InfoItemProps) {
  return (
    <div className="group flex items-center justify-between gap-3 rounded-lg border bg-card p-4 shadow-sm transition-colors hover:bg-muted/50">
      <div className="flex min-w-0 flex-1 items-start gap-3">
        <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          {icon}
        </div>
        <div className="flex min-w-0 flex-col">
          <span className="truncate text-xs font-medium tracking-wider text-muted-foreground uppercase">
            {label}
          </span>
          <span
            className={`mt-1 truncate text-sm font-semibold ${
              isEmpty
                ? 'font-normal text-muted-foreground/70 italic'
                : 'text-foreground'
            }`}
          >
            {value}
          </span>
        </div>
      </div>

      {action && (
        <div className="ml-2 flex shrink-0 items-center justify-center">
          {action}
        </div>
      )}
    </div>
  )
}
