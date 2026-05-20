import { Button } from '@/components/ui/Button'
import { InfoItem } from '@/components/ui/InfoItem'
import { Pencil } from 'lucide-react'

interface EditableInfoItemProps {
  icon: React.ReactNode
  label: string
  value: string | number
  editValue?: string | number
  fieldKey: string
  canEdit?: boolean
  buttonText?: string
  isEmpty?: boolean
  onEdit: (key: string, label: string, value: string) => void
}

/**
 * Profile info row that can expose an edit action.
 *
 * @param props.fieldKey - Field identifier passed back to the edit modal.
 * @param props.editValue - Raw value used for editing when display value is formatted.
 * @param props.onEdit - Opens the caller-owned editing flow.
 */
export function EditableInfoItem({
  icon,
  label,
  value,
  editValue,
  fieldKey,
  canEdit = true,
  buttonText,
  isEmpty = false,
  onEdit,
}: EditableInfoItemProps) {
  const editButton = canEdit ? (
    <Button
      variant="ghost"
      size="icon"
      className="h-10 w-10 opacity-100 transition-opacity focus:opacity-100 md:h-8 md:w-8 md:opacity-0 md:group-hover:opacity-100"
      onClick={() => onEdit(fieldKey, label, String(editValue ?? value))}
      title={`${buttonText ?? 'Edit'} ${label}`}
    >
      <Pencil className="h-4 w-4 text-muted-foreground" />
    </Button>
  ) : undefined

  return (
    <InfoItem
      icon={icon}
      label={label}
      value={String(value)}
      isEmpty={isEmpty}
      action={editButton}
    />
  )
}
