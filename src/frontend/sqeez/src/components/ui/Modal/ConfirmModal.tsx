import { useTranslation } from 'react-i18next'
import { BaseModal } from '@/components/ui/Modal'
import { AsyncButton, Button } from '@/components/ui/Button'
import { AlertTriangle } from 'lucide-react'

interface ConfirmModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => Promise<void> | void
  title: string
  description: string
  confirmText?: string
  cancelText?: string
  isDestructive?: boolean
  isLoading?: boolean
}

/**
 * Confirmation dialog for actions that need an explicit user decision.
 * Use `isDestructive` for irreversible or high-risk actions so the dialog uses destructive styling.
 *
 * @param props.onConfirm - Action executed by the confirm button; promises keep the button loading.
 * @param props.isDestructive - Switches confirm styling and warning icon for irreversible actions.
 * @param props.isLoading - External loading state that disables cancellation.
 */
export function ConfirmModal({
  isOpen,
  onClose,
  onConfirm,
  title,
  description,
  confirmText,
  cancelText,
  isDestructive = false,
  isLoading = false,
}: ConfirmModalProps) {
  const { t } = useTranslation()

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      description={description}
      footer={
        <div className="flex w-full justify-center gap-4 sm:space-x-0">
          <Button
            variant="outline"
            size="lg"
            onClick={onClose}
            disabled={isLoading}
            className="min-w-32"
          >
            {cancelText || t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            onClick={onConfirm}
            loadingText={t('common.saving') + '...'}
            variant={isDestructive ? 'destructive' : 'default'}
            className="min-w-32"
          >
            {confirmText || t('common.confirm')}
          </AsyncButton>
        </div>
      }
    >
      {isDestructive && (
        <div className="flex justify-center py-4">
          <div className="rounded-full bg-destructive/10 p-4">
            <AlertTriangle className="h-8 w-8 text-destructive" />
          </div>
        </div>
      )}
    </BaseModal>
  )
}
