import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { type QueryKey, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react'

import {
  useDeleteApiSubjectsId,
  useDeleteApiSubjectsSubjectIdEnrollments,
  useDeleteApiSubjectsSubjectIdQuizzes,
} from '@/api/generated/endpoints/subjects/subjects'

import type { SubjectDto } from '@/api/generated/model'
import { Button } from '@/components/ui/Button'
import { BaseModal } from '@/components/ui/Modal'
import { cn } from '@/lib/utils'

interface DeleteSubjectModalProps {
  isOpen: boolean
  onClose: () => void
  subject: SubjectDto | null
  subjectsQueryKey: QueryKey
}

type DeleteSubjectStepId = 'enrollments' | 'quizzes' | 'subject'
type StepState = 'completed' | 'failed' | 'pending' | 'running'

/**
 * Three-step destructive subject deletion flow.
 * Enrollments, quizzes, and the subject are confirmed and executed one step at a time.
 *
 * @param props.subject - Subject being deleted; counts decide which cleanup steps are shown.
 * @param props.subjectsQueryKey - Exact subjects table query key invalidated after each step.
 */
export function DeleteSubjectModal({
  isOpen,
  onClose,
  subject,
  subjectsQueryKey,
}: DeleteSubjectModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [activeStep, setActiveStep] = useState<DeleteSubjectStepId | null>(null)
  const [completedSteps, setCompletedSteps] = useState<DeleteSubjectStepId[]>(
    [],
  )
  const [failedStep, setFailedStep] = useState<DeleteSubjectStepId | null>(null)

  const deleteEnrollmentsMutation = useDeleteApiSubjectsSubjectIdEnrollments()
  const deleteQuizzesMutation = useDeleteApiSubjectsSubjectIdQuizzes()
  const deleteSubjectMutation = useDeleteApiSubjectsId()

  const isDeleting =
    deleteEnrollmentsMutation.isPending ||
    deleteQuizzesMutation.isPending ||
    deleteSubjectMutation.isPending

  const resetProgress = () => {
    setActiveStep(null)
    setCompletedSteps([])
    setFailedStep(null)
  }

  const handleClose = () => {
    if (isDeleting) return

    resetProgress()
    onClose()
  }

  const handleRunStep = async () => {
    if (!subject) return

    const subjectId = subject.id.toString()
    const currentStep =
      steps.find((step) => step.id === failedStep) ??
      steps.find((step) => !completedSteps.includes(step.id))

    if (!currentStep) return

    try {
      setFailedStep(null)
      setActiveStep(currentStep.id)

      if (currentStep.id === 'enrollments') {
        await deleteEnrollmentsMutation.mutateAsync({
          subjectId,
          data: null,
          params: { deleteAll: true },
        })
        setCompletedSteps((previousSteps) => [...previousSteps, currentStep.id])

        await queryClient.invalidateQueries({
          queryKey: subjectsQueryKey,
          exact: true,
        })
      }

      if (currentStep.id === 'quizzes') {
        await deleteQuizzesMutation.mutateAsync({
          subjectId,
          params: { deleteAll: true },
        })
        setCompletedSteps((previousSteps) => [...previousSteps, currentStep.id])

        await queryClient.invalidateQueries({
          queryKey: subjectsQueryKey,
          exact: true,
        })
      }

      if (currentStep.id === 'subject') {
        await deleteSubjectMutation.mutateAsync({ id: subjectId })
        setCompletedSteps((previousSteps) => [...previousSteps, currentStep.id])

        await queryClient.invalidateQueries({
          queryKey: subjectsQueryKey,
          exact: true,
        })

        toast.success(t('admin.subjects.subjectDeleted'))
        resetProgress()
        onClose()
      }
    } catch (error) {
      console.error('Failed to delete subject:', error)
      setFailedStep(currentStep.id)
      toast.error(t('admin.subjects.deleteSubjectFailed'))
    } finally {
      setActiveStep(null)
    }
  }

  const enrollmentCount = Number(subject?.enrollmentCount ?? 0)
  const quizCount = Number(subject?.quizCount ?? 0)
  const steps = [
    ...(enrollmentCount > 0
      ? [
          {
            id: 'enrollments' as const,
            label: t('admin.subjects.deleteSubjectStepEnrollments', {
              count: enrollmentCount,
            }),
          },
        ]
      : []),
    ...(quizCount > 0
      ? [
          {
            id: 'quizzes' as const,
            label: t('admin.subjects.deleteSubjectStepQuizzes', {
              count: quizCount,
            }),
          },
        ]
      : []),
    {
      id: 'subject' as const,
      label: t('admin.subjects.deleteSubjectStepSubject'),
    },
  ]
  const nextStep =
    steps.find((step) => step.id === failedStep) ??
    steps.find((step) => !completedSteps.includes(step.id))
  const nextStepIndex = nextStep ? steps.indexOf(nextStep) : -1
  const isComplete = completedSteps.length === steps.length

  const getStepState = (stepId: DeleteSubjectStepId): StepState => {
    if (failedStep === stepId) return 'failed'
    if (completedSteps.includes(stepId)) return 'completed'
    if (activeStep === stepId) return 'running'
    return 'pending'
  }

  const getStepStatusLabel = (state: StepState) => {
    switch (state) {
      case 'completed':
        return t('admin.subjects.deleteSubjectStepDone')
      case 'failed':
        return t('admin.subjects.deleteSubjectStepFailed')
      case 'running':
        return t('admin.subjects.deleteSubjectStepRunning')
      default:
        return t('admin.subjects.deleteSubjectStepPending')
    }
  }

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('admin.subjects.deleteSubjectTitle')}
      description={t('admin.subjects.deleteSubjectDesc', {
        subjectName: subject?.name,
      })}
      className="w-[calc(100vw-2rem)] max-w-[calc(100vw-2rem)] sm:max-w-lg"
      footer={
        <>
          <Button variant="outline" onClick={handleClose} disabled={isDeleting}>
            {t('common.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleRunStep}
            disabled={!subject || isDeleting || isComplete}
          >
            {isDeleting && <Loader2 className="animate-spin" />}
            {failedStep === null
              ? t('admin.subjects.deleteSubjectRunStep', {
                  stepNumber: nextStepIndex + 1,
                  totalSteps: steps.length,
                })
              : t('admin.subjects.deleteSubjectRetryStep', {
                  stepNumber: nextStepIndex + 1,
                  totalSteps: steps.length,
                })}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4" aria-live="polite">
        <p className="text-sm text-muted-foreground">
          {t('admin.subjects.deleteSubjectProcessDesc')}
        </p>

        <div className="space-y-3">
          {steps.map((step, index) => {
            const state = getStepState(step.id)

            return (
              <div
                key={step.id}
                className={cn(
                  'flex items-center gap-3 rounded-lg border p-3 text-sm transition-colors',
                  state === 'completed' &&
                    'border-success/30 bg-success/10 text-success',
                  state === 'failed' &&
                    'border-destructive/30 bg-destructive/10 text-destructive',
                  state === 'running' &&
                    'border-primary/30 bg-primary/10 text-primary',
                  state === 'pending' && 'text-muted-foreground',
                )}
              >
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full border bg-background">
                  {state === 'completed' && (
                    <CheckCircle2 className="h-4 w-4" />
                  )}
                  {state === 'failed' && <AlertCircle className="h-4 w-4" />}
                  {state === 'running' && (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  )}
                  {state === 'pending' && (
                    <span className="text-xs font-semibold">{index + 1}</span>
                  )}
                </div>

                <div className="min-w-0 flex-1">
                  <p className="font-medium text-foreground">{step.label}</p>
                  <p className="text-xs text-muted-foreground">
                    {getStepStatusLabel(state)}
                  </p>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </BaseModal>
  )
}
