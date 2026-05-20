import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FileText, X, AlertCircle, CheckCircle } from 'lucide-react'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import { usePostApiImportMaster } from '@/api/generated/endpoints/import/import'
import type { ImportResultDto } from '@/api/generated/model'
import { ScrollArea } from '@/components/ui/ScrollArea'

interface ImportCsvModalProps {
  isOpen: boolean
  onClose: () => void
}

/**
 * Admin CSV import modal.
 * Upload result details are kept visible so admins can inspect partial failures.
 *
 * @param props.isOpen - Controls modal visibility.
 * @param props.onClose - Closes the modal after local file/result state is reset.
 */
export function ImportCsvModal({ isOpen, onClose }: ImportCsvModalProps) {
  const { t } = useTranslation()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [result, setResult] = useState<ImportResultDto | null>(null)

  const importMutation = usePostApiImportMaster({
    mutation: {
      onSuccess: (response) => {
        setResult(response)
        toast.success(t('admin.import.completed'))
      },
      onError: () => {
        toast.error(t('errors.importFailed'))
      },
    },
  })

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile && selectedFile.name.endsWith('.csv')) {
      setSelectedFile(selectedFile)
      setResult(null)
    } else {
      toast.error(t('admin.import.onlyCsvAllowed'))
    }
  }

  const handleUpload = async () => {
    if (!selectedFile) return
    await importMutation.mutateAsync({ data: { file: selectedFile } })
  }

  const resetModal = () => {
    setSelectedFile(null)
    setResult(null)
    onClose()
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={resetModal}
      title={t('admin.import.uploadTitle')}
      className="sm:max-w-xl"
    >
      <div className="space-y-6 py-4">
        {!result ? (
          <div
            onClick={() => fileInputRef.current?.click()}
            className="flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/25 p-12 transition-colors hover:bg-muted/50"
          >
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileChange}
              className="hidden"
              accept=".csv"
            />
            <FileText className="mb-4 h-12 w-12 text-muted-foreground" />
            <p className="text-sm font-medium">
              {selectedFile
                ? selectedFile.name
                : t('admin.import.clickToUpload')}
            </p>
            {selectedFile && (
              <Button
                variant="ghost"
                size="sm"
                className="mt-2 h-7 gap-1 text-xs text-destructive"
                onClick={(e) => {
                  e.stopPropagation()
                  setSelectedFile(null)
                }}
              >
                <X className="h-3 w-3" /> {t('common.remove')}
              </Button>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center gap-3 rounded-lg bg-primary/10 p-4 text-primary">
              <CheckCircle className="h-5 w-5" />
              <div>
                <p className="text-sm font-bold">
                  {t('admin.import.successTitle')}
                </p>
                <p className="text-xs">
                  {t('admin.import.recordsCount', {
                    count: result.recordsImported
                      ? Number(result.recordsImported)
                      : 0,
                  })}
                </p>
              </div>
            </div>

            {result.errors && result.errors.length > 0 && (
              <div className="space-y-2">
                <p className="flex items-center gap-1 text-xs font-semibold text-destructive">
                  <AlertCircle className="h-3 w-3" />{' '}
                  {t('admin.import.issuesFound')}
                </p>
                <ScrollArea className="h-48 rounded-md border bg-muted/30 p-2">
                  <ul className="space-y-1">
                    {result.errors.map((err, idx) => (
                      <li
                        key={idx}
                        className="text-[11px] text-muted-foreground"
                      >
                        • {err}
                      </li>
                    ))}
                  </ul>
                </ScrollArea>
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end gap-3">
          <Button variant="outline" onClick={resetModal}>
            {result ? t('common.close') : t('common.cancel')}
          </Button>
          {!result && (
            <AsyncButton
              onClick={handleUpload}
              isLoading={importMutation.isPending}
              disabled={!selectedFile}
              className="min-w-25"
            >
              {t('admin.import.process')}
            </AsyncButton>
          )}
        </div>
      </div>
    </BaseModal>
  )
}
