import { type ReactNode } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui'
import { cn } from '@/lib/utils'

interface BaseModalProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description?: string
  children: ReactNode
  footer?: ReactNode
  className?: string
}

/**
 * Shared modal shell used by feature-specific dialogs.
 * `onClose` is called only when the dialog transitions to a closed state.
 *
 * @param props.isOpen - Controls the underlying dialog open state.
 * @param props.onClose - Called when the user closes the dialog.
 * @param props.footer - Optional modal footer; callers own button behavior.
 */
export function BaseModal({
  isOpen,
  onClose,
  title,
  description,
  children,
  footer,
  className,
}: BaseModalProps) {
  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className={cn('max-w-fit', className)}>
        {(title || description) && (
          <DialogHeader>
            {title && <DialogTitle>{title}</DialogTitle>}
            {description && (
              <DialogDescription>{description}</DialogDescription>
            )}
          </DialogHeader>
        )}

        <div className="max-w-80 py-4 sm:max-w-full">{children}</div>

        {footer && <DialogFooter>{footer}</DialogFooter>}
      </DialogContent>
    </Dialog>
  )
}
