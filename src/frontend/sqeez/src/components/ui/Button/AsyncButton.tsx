import { useState } from 'react'
import { Button, type ButtonProps } from '@/components/ui/Button'
import { Spinner } from '../Spinner'

interface AsyncButtonProps extends Omit<ButtonProps, 'onClick' | 'asChild'> {
  onClick?: (
    e: React.MouseEvent<HTMLButtonElement, MouseEvent>,
  ) => Promise<void> | void
  loadingText?: string
  isLoading?: boolean
}

/**
 * Button wrapper for async actions.
 * It disables itself while the click handler is pending, while still allowing callers to drive loading state externally.
 *
 * @param props.onClick - Optional click handler; promises keep the button in an internal loading state until settled.
 * @param props.isLoading - External loading flag for parent-controlled mutations.
 * @param props.loadingText - Text shown while either internal or external loading is active.
 */
export function AsyncButton({
  onClick,
  children,
  disabled,
  loadingText,
  isLoading,
  ...props
}: AsyncButtonProps) {
  const [isInternalLoading, setIsInternalLoading] = useState(false)

  const handleClick = async (
    e: React.MouseEvent<HTMLButtonElement, MouseEvent>,
  ) => {
    if (!onClick) return

    setIsInternalLoading(true)
    try {
      await onClick(e)
    } finally {
      setIsInternalLoading(false)
    }
  }

  return (
    <Button
      disabled={isInternalLoading || isLoading || disabled}
      onClick={handleClick}
      {...props}
    >
      {(isInternalLoading || isLoading) && <Spinner size={'sm'} />}
      {isInternalLoading || isLoading ? loadingText : children}
    </Button>
  )
}
