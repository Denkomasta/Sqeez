import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { AlertCircle, CheckCircle, FileText, X } from 'lucide-react'

import { usePostApiSubjectsSubjectIdQuizzesImport } from '@/api/generated/endpoints/subjects/subjects'
import type { ImportResultDto } from '@/api/generated/model'
import { AsyncButton, Button } from '@/components/ui/Button'
import { BaseModal } from '@/components/ui/Modal'
import { ScrollArea } from '@/components/ui/ScrollArea'

interface ImportQuizCsvModalProps {
  isOpen: boolean
  onClose: () => void
  onImported: () => void
  subjectId: number | string
}

const quizCsvHeader =
  'Quiz Title,Quiz Description,Max Retries,Publish Date,Closing Date,Question Order,Question Title,Difficulty,Time Limit,Has Penalty,Is Strict Multiple Choice,Option Order,Option Text,Is Correct,Is Free Text'

/**
 * Teacher CSV quiz import modal.
 * The selected file is posted to the subject import endpoint and reports row-level results.
 */
export function ImportQuizCsvModal({
  isOpen,
  onClose,
  onImported,
  subjectId,
}: ImportQuizCsvModalProps) {
  const { t } = useTranslation()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [result, setResult] = useState<ImportResultDto | null>(null)

  const importMutation = usePostApiSubjectsSubjectIdQuizzesImport({
    mutation: {
      onSuccess: (response) => {
        setResult(response)
        onImported()
        toast.success(t('dashboard.quizImportCompleted'))
      },
      onError: () => {
        toast.error(t('dashboard.quizImportFailed'))
      },
    },
  })

  const resetModal = () => {
    setSelectedFile(null)
    setResult(null)
    onClose()
  }

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]

    if (!file) return

    if (!file.name.toLowerCase().endsWith('.csv')) {
      toast.error(t('dashboard.quizImportOnlyCsv'))
      event.target.value = ''
      return
    }

    setSelectedFile(file)
    setResult(null)
  }

  const handleUpload = async () => {
    if (!selectedFile) return

    await importMutation.mutateAsync({
      subjectId,
      data: { file: selectedFile },
    })
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={resetModal}
      title={t('dashboard.quizImportTitle')}
      description={t('dashboard.quizImportDescription')}
      className="sm:max-w-2xl"
    >
      <div className="space-y-6 py-4">
        <div className="space-y-3 rounded-lg border bg-muted/20 p-4 text-sm">
          <p className="font-medium text-foreground">
            {t('dashboard.quizImportFormatTitle')}
          </p>
          <p className="text-muted-foreground">
            {t('dashboard.quizImportFormatDescription')}
          </p>
          <ScrollArea className="max-h-24 rounded-md border bg-background p-3">
            <code className="text-xs whitespace-pre-wrap">{quizCsvHeader}</code>
          </ScrollArea>
          <ul className="list-disc space-y-1 pl-5 text-xs text-muted-foreground">
            <li>{t('dashboard.quizImportRuleOneRowPerOption')}</li>
            <li>{t('dashboard.quizImportRuleBooleans')}</li>
            <li>{t('dashboard.quizImportRuleDates')}</li>
            <li>{t('dashboard.quizImportRuleFreeText')}</li>
          </ul>
        </div>

        {!result ? (
          <div
            onClick={() => fileInputRef.current?.click()}
            className="flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/25 p-10 text-center transition-colors hover:bg-muted/50"
          >
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileChange}
              className="hidden"
              accept=".csv,text/csv"
            />
            <FileText className="mb-4 h-12 w-12 text-muted-foreground" />
            <p className="text-sm font-medium">
              {selectedFile
                ? selectedFile.name
                : t('dashboard.quizImportClickToUpload')}
            </p>
            {selectedFile && (
              <Button
                variant="ghost"
                size="sm"
                className="mt-2 h-7 gap-1 text-xs text-destructive"
                onClick={(event) => {
                  event.stopPropagation()
                  setSelectedFile(null)
                  if (fileInputRef.current) fileInputRef.current.value = ''
                }}
              >
                <X className="h-3 w-3" />
                {t('common.remove')}
              </Button>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center gap-3 rounded-lg bg-primary/10 p-4 text-primary">
              <CheckCircle className="h-5 w-5" />
              <div>
                <p className="text-sm font-bold">
                  {t('dashboard.quizImportSuccessTitle')}
                </p>
                <p className="text-xs">
                  {t('dashboard.quizImportRecordsCount', {
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
                  <AlertCircle className="h-3 w-3" />
                  {t('dashboard.quizImportIssuesFound')}
                </p>
                <ScrollArea className="h-48 rounded-md border bg-muted/30 p-2">
                  <ul className="space-y-1">
                    {result.errors.map((error, index) => (
                      <li
                        key={`${error}-${index}`}
                        className="text-[11px] text-muted-foreground"
                      >
                        - {error}
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
              loadingText={t('common.saving')}
            >
              {t('dashboard.quizImportProcess')}
            </AsyncButton>
          )}
        </div>
      </div>
    </BaseModal>
  )
}
