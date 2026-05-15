import { useState, useEffect } from 'react'
import { Check, CloudUpload, AlertCircle } from 'lucide-react'
import { TextArea } from '@/components/ui/TextArea'
import { cn } from '@/lib/utils'

interface DebouncedTextAreaProps {
  initialValue: string
  onSave: (value: string) => Promise<void>
  placeholder?: string
  savingText?: string
  savedText?: string
  errorText?: string
  label?: string
  className?: string
}

export function DebouncedTextArea({
  initialValue,
  onSave,
  placeholder,
  savingText = 'Saving...',
  savedText = 'Saved',
  errorText = 'Error',
  label,
  className,
}: DebouncedTextAreaProps) {
  const [value, setValue] = useState(initialValue)
  const [status, setStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>(
    'idle',
  )

  useEffect(() => {
    setValue(initialValue)
  }, [initialValue])

  useEffect(() => {
    if (value === initialValue) return

    setStatus('saving')
    const timeout = setTimeout(async () => {
      try {
        await onSave(value)
        setStatus('saved')
        setTimeout(() => setStatus('idle'), 2000)
      } catch {
        setStatus('error')
      }
    }, 1000)

    return () => clearTimeout(timeout)
  }, [value, initialValue, onSave])

  return (
    <div className="group relative">
      <TextArea
        label={label}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={placeholder}
        className={cn(
          'min-h-30 resize-none p-4 text-lg font-medium transition-all focus:ring-2 focus:ring-primary/20',
          className,
        )}
        hideErrors
      />

      <div className="absolute right-3 bottom-3 flex items-center gap-1.5 rounded border bg-background/80 px-2 py-1 text-[10px] font-bold tracking-tight uppercase shadow-sm backdrop-blur transition-opacity">
        {status === 'saving' && (
          <span className="flex items-center gap-1 text-warning">
            <CloudUpload className="h-3 w-3 animate-bounce" /> {savingText}
          </span>
        )}
        {status === 'saved' && (
          <span className="flex items-center gap-1 text-success">
            <Check className="h-3 w-3" /> {savedText}
          </span>
        )}
        {status === 'error' && (
          <span className="flex items-center gap-1 text-destructive">
            <AlertCircle className="h-3 w-3" /> {errorText}
          </span>
        )}
      </div>
    </div>
  )
}
