import { useCallback, useEffect, useRef, useState } from 'react'
import { Input } from '@/components/ui/Input'

interface DebouncedInputProps extends Omit<
  React.InputHTMLAttributes<HTMLInputElement>,
  'onChange'
> {
  value: string
  onChange: (value: string) => void
  debounceTime?: number
  icon?: React.ReactNode
  label?: string
  hideErrors?: boolean
  helpText?: string
  wrapperClassName?: string
}

export function DebouncedInput({
  value: initialValue,
  onChange,
  debounceTime = 300,
  icon,
  wrapperClassName = '',
  label,
  hideErrors,
  helpText,
  onBlur,
  ...props
}: DebouncedInputProps) {
  const [draftValue, setDraftValue] = useState<string | null>(null)
  const [pendingCommit, setPendingCommit] = useState<{
    value: string
    baseValue: string
  } | null>(null)
  const lastSubmittedValueRef = useRef(initialValue)

  useEffect(() => {
    lastSubmittedValueRef.current = initialValue
  }, [initialValue])

  const value =
    draftValue ??
    (pendingCommit && pendingCommit.baseValue === initialValue
      ? pendingCommit.value
      : initialValue)

  const commitValue = useCallback(
    (value: string) => {
      setDraftValue(null)

      if (value === lastSubmittedValueRef.current) return

      lastSubmittedValueRef.current = value
      setPendingCommit({ value, baseValue: initialValue })
      onChange(value)
    },
    [initialValue, onChange],
  )

  useEffect(() => {
    if (draftValue === null) return
    if (draftValue === lastSubmittedValueRef.current) return

    const timer = setTimeout(() => {
      commitValue(draftValue)
    }, debounceTime)

    return () => clearTimeout(timer)
  }, [draftValue, debounceTime, commitValue])

  return (
    <div className={`relative w-full ${wrapperClassName}`}>
      <Input
        {...props}
        icon={icon}
        label={label}
        value={value}
        onChange={(e) => {
          setPendingCommit(null)
          setDraftValue(e.target.value)
        }}
        onBlur={(event) => {
          commitValue(value)
          onBlur?.(event)
        }}
        hideErrors={hideErrors}
        helpText={helpText}
      />
    </div>
  )
}
