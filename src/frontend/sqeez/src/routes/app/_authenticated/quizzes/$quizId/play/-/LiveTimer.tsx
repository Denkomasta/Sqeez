import { useState, useEffect, useRef } from 'react'
import { Clock } from 'lucide-react'
import { formatTimer } from '@/lib/dateHelpers'
import { cn } from '@/lib/utils'

interface LiveTimerProps {
  timeLimit: number
  onTimeUp?: () => void
}

/**
 * Shows elapsed time or a countdown for active quiz questions.
 * A positive time limit counts down and fires `onTimeUp` once.
 */
export function LiveTimer({ timeLimit, onTimeUp }: LiveTimerProps) {
  const isCountdown = timeLimit > 0
  const [seconds, setSeconds] = useState(isCountdown ? timeLimit : 0)

  const onTimeUpRef = useRef(onTimeUp)
  const hasFiredRef = useRef(false)

  useEffect(() => {
    onTimeUpRef.current = onTimeUp
  }, [onTimeUp])

  useEffect(() => {
    if (isCountdown && seconds <= 0) return

    const intervalId = setInterval(() => {
      setSeconds((prev) => (isCountdown ? prev - 1 : prev + 1))
    }, 1000)

    return () => clearInterval(intervalId)
  }, [isCountdown, seconds <= 0])

  useEffect(() => {
    if (isCountdown && seconds <= 0 && !hasFiredRef.current) {
      hasFiredRef.current = true
      onTimeUpRef.current?.()
    }
  }, [isCountdown, seconds])

  const isUrgent = isCountdown && seconds <= 10 && seconds > 0

  return (
    <div
      className={cn(
        'flex items-center gap-2 text-xl transition-colors',
        isUrgent
          ? 'animate-pulse font-bold text-destructive'
          : 'font-semibold text-foreground/80',
      )}
    >
      <Clock className="h-6 w-6" />
      <span className="w-16 text-right tabular-nums">
        {formatTimer(seconds)}
      </span>
    </div>
  )
}
