import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  Loader2,
  Settings2,
  Globe,
  Globe2,
  Trash2,
  AlertTriangle,
  Lock,
  FileDown,
} from 'lucide-react'

import {
  getApiQuizzesQuizIdExport,
  getGetApiQuizzesQuizIdQueryKey,
  useGetApiQuizzesQuizId,
  usePatchApiQuizzesQuizId,
} from '@/api/generated/endpoints/quizzes/quizzes'

import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Button } from '@/components/ui/Button'
import { DebouncedTextArea } from '@/components/ui/TextArea'
import { DateTimePicker, Input } from '@/components/ui/Input'
import { ConfirmModal } from '@/components/ui/Modal/ConfirmModal'
import { useDeleteApiQuizAttemptsQuizIdAttempts } from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import { toUtcIsoString } from '@/lib/dateHelpers'
import type { PatchQuizDto } from '@/api/generated/model'

interface QuizSettingsEditorProps {
  quizId: string
}

const getCsvFileName = (title?: string | null) => {
  const safeTitle = title
    ?.trim()
    .replace(/[^a-zA-Z0-9-_]+/g, '-')
    .replace(/^-+|-+$/g, '')

  return `${safeTitle || 'quiz'}.csv`
}

/**
 * Edits quiz-level settings and destructive quiz actions.
 * Date clearing uses reset flags because the patch contract distinguishes null from reset.
 *
 * @param props.quizId - Quiz whose settings, attempts, and CSV export are managed.
 */
export function QuizSettingsEditor({ quizId }: QuizSettingsEditorProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { isLocked, actions } = useQuizEditorUIStore()

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const [isExporting, setIsExporting] = useState(false)

  const { data: quizResponse, isLoading } = useGetApiQuizzesQuizId(
    quizId,
    undefined,
    {
      query: { enabled: !!quizId },
    },
  )

  useEffect(() => {
    if (quizResponse) {
      actions.setLocked(Number(quizResponse.quizAttempts) > 0)
    }
  }, [quizResponse, actions])

  const patchMutation = usePatchApiQuizzesQuizId({
    mutation: {
      onSuccess: (updatedQuizData) => {
        queryClient.setQueryData(
          getGetApiQuizzesQuizIdQueryKey(quizId),
          updatedQuizData,
        )
      },
    },
  })

  const deleteAttemptsMutation = useDeleteApiQuizAttemptsQuizIdAttempts({
    mutation: {
      onSuccess: () => {
        toast.success(t('editor.attemptsDeletedSuccess'))
        setIsDeleteModalOpen(false)

        actions.setLocked(false)

        queryClient.invalidateQueries({
          predicate: (query) => query.queryKey.includes('attempts'),
        })
      },
      onError: () => {
        toast.error(t('common.error'))
      },
    },
  })

  const quiz = quizResponse

  const buildPatchPayload = (
    field: string,
    value: string | number | null,
  ): PatchQuizDto => {
    if (field === 'publishDate') {
      return value === null
        ? { resetPublishDate: true }
        : { publishDate: String(value) }
    }

    if (field === 'closingDate') {
      return value === null
        ? { resetClosingDate: true }
        : { closingDate: String(value) }
    }

    return { [field]: value } as PatchQuizDto
  }

  const handleUpdate = async (field: string, value: string | number | null) => {
    await patchMutation.mutateAsync({
      quizId,
      data: buildPatchPayload(field, value),
    })
  }

  const togglePublish = async () => {
    const isPublished = !!quiz?.publishDate
    await handleUpdate(
      'publishDate',
      isPublished ? null : new Date().toISOString(),
    )
  }

  const handleDeleteAllAttempts = async () => {
    await deleteAttemptsMutation.mutateAsync({
      quizId,
    })
  }

  const handleExportQuiz = async () => {
    try {
      setIsExporting(true)
      const response = await getApiQuizzesQuizIdExport(quizId, {
        responseType: 'blob',
      })
      const blob = response as unknown as Blob
      const objectUrl = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = objectUrl
      link.download = getCsvFileName(quiz?.title)
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(objectUrl)
      toast.success(t('editor.quizExported'))
    } catch (error) {
      console.error(error)
      toast.error(t('editor.quizExportFailed'))
    } finally {
      setIsExporting(false)
    }
  }

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center bg-background">
        <Loader2 className="h-8 w-8 animate-spin text-primary/30" />
      </div>
    )
  }

  const isPublished = !!quiz?.publishDate

  return (
    <div className="flex-1 overflow-y-auto bg-background p-8 lg:p-12">
      <div className="mx-auto max-w-3xl space-y-8">
        <div className="flex items-center justify-between border-b border-border pb-6">
          <div className="flex items-center gap-2">
            <Settings2 className="h-6 w-6 text-primary" />
            <h1 className="text-2xl font-bold tracking-tight">
              {t('editor.quizSettings')}
            </h1>
            {isLocked && (
              <Lock className="ml-2 h-5 w-5 text-muted-foreground" />
            )}
          </div>
          <div className="flex flex-wrap justify-end gap-2">
            <Button
              onClick={handleExportQuiz}
              disabled={isExporting}
              className="gap-2 shadow-sm"
            >
              {isExporting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <FileDown className="h-4 w-4" />
              )}
              {t('editor.exportCsv')}
            </Button>
            <Button
              variant={isPublished ? 'outline' : 'default'}
              onClick={togglePublish}
              disabled={patchMutation.isPending}
              className="gap-2 shadow-sm"
            >
              {isPublished ? (
                <>
                  <Globe className="h-4 w-4 text-muted-foreground" />
                  {t('editor.unpublish')}
                </>
              ) : (
                <>
                  <Globe2 className="h-4 w-4" />
                  {t('editor.publish')}
                </>
              )}
            </Button>
          </div>
        </div>

        <DebouncedInput
          value={quiz?.title ?? ''}
          label={t('editor.quizTitle')}
          onChange={(val) => handleUpdate('title', val)}
          placeholder={t('editor.untitledQuiz')}
          className="text-lg font-semibold"
          debounceTime={800}
        />

        <DebouncedTextArea
          label={t('editor.quizDescription')}
          initialValue={quiz?.description ?? ''}
          onSave={async (val) => handleUpdate('description', val)}
          placeholder={t('editor.descriptionPlaceholder')}
          savingText={t('common.saving')}
          savedText={t('common.saved')}
          errorText={t('common.error')}
          className="bg-card"
        />

        <div className="grid grid-cols-1 gap-8 rounded-xl border border-border bg-muted/5 p-6 md:grid-cols-2">
          <Input
            label={t('editor.maxRetries')}
            id="max-retries-input"
            type="number"
            min="0"
            value={quiz?.maxRetries ?? 0}
            onChange={(e) =>
              handleUpdate('maxRetries', parseInt(e.target.value) || 0)
            }
            helpText={t('editor.maxRetriesHelp')}
          />

          <DateTimePicker
            label={t('editor.closingDate')}
            id="closing-date-input"
            helpText={t('editor.closingDateHelp')}
            value={quiz?.closingDate}
            min={new Date().toISOString()}
            onChange={(isoString) =>
              handleUpdate(
                'closingDate',
                isoString ? toUtcIsoString(isoString) : null,
              )
            }
          />
        </div>

        <div className="mt-12 rounded-xl border border-destructive/40 bg-destructive/20 p-6">
          <h2 className="mb-2 flex items-center gap-2 text-lg font-semibold text-foreground">
            <AlertTriangle className="h-5 w-5" />
            {t('editor.dangerZone')}
          </h2>
          <p className="mb-4 text-sm text-foreground">
            {t('editor.deleteAllAttemptsWarning')}
          </p>
          <Button
            variant="destructive"
            onClick={() => setIsDeleteModalOpen(true)}
            className="gap-2"
          >
            <Trash2 className="h-4 w-4" />
            {t('editor.deleteAllAttempts')}
          </Button>
        </div>
      </div>

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={handleDeleteAllAttempts}
        title={t('editor.confirmDeleteAllAttempts')}
        description={t('editor.confirmDeleteAllAttemptsText')}
        confirmText={t('common.delete')}
        isDestructive={true}
        isLoading={deleteAttemptsMutation.isPending}
      />
    </div>
  )
}
