import React, { useState } from 'react'
import { Input } from '@/components/ui/Input'
import { toLocalDateTimeInputValue, toUtcIsoString } from '@/lib/dateHelpers'

interface DateTimePickerProps extends Omit<
  React.ComponentProps<typeof Input>,
  'value' | 'onChange' | 'min' | 'max'
> {
  value?: string | null
  min?: string | null
  max?: string | null
  onChange: (isoString: string | null) => void
}

/**
 * Local datetime input that emits UTC ISO strings to the API.
 * Empty values are emitted as null only after blur to avoid noisy patch calls.
 */
export function DateTimePicker({
  value,
  onChange,
  min,
  max,
  icon,
  ...props
}: DateTimePickerProps) {
  const [prevValueProp, setPrevValueProp] = useState(value)
  const [localValue, setLocalValue] = useState(toLocalDateTimeInputValue(value))

  if (value !== prevValueProp) {
    setPrevValueProp(value)
    setLocalValue(toLocalDateTimeInputValue(value))
  }

  const handleBlur = () => {
    if (!localValue) {
      if (value !== null) onChange(null)
      return
    }

    const utcDate = toUtcIsoString(localValue)

    if (utcDate && utcDate !== value) {
      onChange(utcDate)
    }
  }

  return (
    <Input
      type="datetime-local"
      icon={icon}
      value={localValue}
      onChange={(e) => setLocalValue(e.target.value)}
      onBlur={handleBlur}
      min={min ? toLocalDateTimeInputValue(min) : undefined}
      max={max ? toLocalDateTimeInputValue(max) : undefined}
      {...props}
    />
  )
}
