import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, Save, Heart, CheckSquare, Square } from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { AsyncButton } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { LongText } from '@/components/ui/LongText'
import { Spinner } from '@/components/ui/Spinner'

import type { QuestionResponseDto } from '@/api/generated/model'
import {
  getGetApiQuizAttemptsIdQueryKey,
  usePatchApiQuizAttemptsResponsesResponseIdGrade,
} from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed } from '@/api/generated/endpoints/quizzes/quizzes'
import { MediaAssetViewer } from '../../play/-/MediaAssetViewer'

interface QuestionResultCardProps {
  quizId: number | string
  attemptId: number | string
  studentResponse: QuestionResponseDto
  isTeacher: boolean
}

interface AnswerBlockProps {
  label: string
  value?: string | null
  emptyLabel: string
  className?: string
}

function AnswerBlock({
  label,
  value,
  emptyLabel,
  className = 'bg-muted/50',
}: AnswerBlockProps) {
  return (
    <div className={`min-w-0 rounded-md p-4 ${className}`}>
      <p className="mb-2 text-sm font-semibold text-muted-foreground">
        {label}:
      </p>
      <LongText as="p" className="text-base leading-relaxed text-foreground">
        {value || (
          <span className="text-muted-foreground italic">{emptyLabel}</span>
        )}
      </LongText>
    </div>
  )
}

/**
 * Shows a graded question response for an attempt.
 * Teachers can assign points only for free-text answers that need manual review.
 */
export function QuestionResultCard({
  quizId,
  attemptId,
  studentResponse,
  isTeacher,
}: QuestionResultCardProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const { data: questionDef, isLoading } =
    useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed(
      quizId,
      studentResponse.quizQuestionId,
    )

  const awardedScore = Number(studentResponse.score || 0)
  const maxPoints = Number(questionDef?.difficulty || 1)

  const [prevScore, setPrevScore] = useState(studentResponse.score)
  const [gradeInput, setGradeInput] = useState<number | string>(
    studentResponse.score !== null && studentResponse.score !== undefined
      ? awardedScore
      : '',
  )

  const gradeMutation = usePatchApiQuizAttemptsResponsesResponseIdGrade({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: getGetApiQuizAttemptsIdQueryKey(Number(attemptId)),
        })
        toast.success(t('grading.savedSuccessfully'))
      },
      onError: () => {
        toast.error(t('common.error'))
      },
    },
  })

  if (studentResponse.score !== prevScore) {
    setPrevScore(studentResponse.score)
    setGradeInput(
      studentResponse.score !== null && studentResponse.score !== undefined
        ? Number(studentResponse.score)
        : '',
    )
  }

  if (isLoading || !questionDef) {
    return (
      <Card className="flex min-h-37.5 items-center justify-center border-l-4 border-l-muted">
        <Spinner />
      </Card>
    )
  }

  const isFreeText = questionDef.options.some((opt) => opt.isFreeText)
  const optionMediaAssetIds = questionDef.options
    .map((option) => option.mediaAssetId)
    .filter((assetId): assetId is number | string => assetId !== null)

  const isNeedsGrading =
    isFreeText &&
    awardedScore === 0 &&
    studentResponse.freeTextAnswer &&
    gradeInput === ''

  const isPerfectScore = awardedScore === maxPoints

  const handleSaveGrade = async () => {
    const numericGrade = Number(gradeInput)
    if (gradeInput === '' || numericGrade < 0 || numericGrade > maxPoints) {
      toast.error(t('grading.invalidPoints', { max: maxPoints }))
      return
    }

    await gradeMutation.mutateAsync({
      responseId: Number(studentResponse.id),
      data: {
        score: numericGrade,
        isLiked: studentResponse.isLiked,
      },
    })
  }

  return (
    <Card
      className={`border-l-4 ${
        isNeedsGrading
          ? 'border-l-warning'
          : isPerfectScore
            ? 'border-l-success'
            : 'border-l-destructive'
      }`}
    >
      <CardHeader className="space-y-4 pb-2">
        <div className="flex flex-row items-start justify-between gap-4">
          <CardTitle className="text-lg leading-tight">
            {questionDef.title}
          </CardTitle>
          <div className="flex shrink-0 items-center gap-1 text-sm font-medium">
            {studentResponse.isLiked && (
              <Heart className="mr-2 h-4 w-4 fill-destructive text-destructive" />
            )}

            {isNeedsGrading ? (
              <span className="flex items-center text-warning">
                <AlertCircle className="mr-1 h-4 w-4" />
                {t('grading.needsGrading')}
              </span>
            ) : (
              <span
                className={isPerfectScore ? 'text-success' : 'text-destructive'}
              >
                {awardedScore} / {maxPoints} {t('common.points')}
              </span>
            )}
          </div>
        </div>

        {questionDef.mediaAssetId && (
          <div className="w-full overflow-hidden rounded-xl">
            <MediaAssetViewer
              assetId={questionDef.mediaAssetId}
              isOption={false}
            />
          </div>
        )}
      </CardHeader>

      <CardContent className="space-y-4 pt-4">
        {isFreeText && (
          <>
            {optionMediaAssetIds.length > 0 && (
              <div className="grid gap-3 sm:grid-cols-2">
                {optionMediaAssetIds.map((assetId) => (
                  <div
                    key={assetId}
                    className="overflow-hidden rounded-lg border border-border bg-card"
                  >
                    <MediaAssetViewer assetId={assetId} isOption />
                  </div>
                ))}
              </div>
            )}

            <AnswerBlock
              label={t('grading.expectedAnswer')}
              value={questionDef.options[0]?.text}
              emptyLabel={t('grading.noExpectedAnswer')}
              className="bg-info/75"
            />

            <AnswerBlock
              label={t('grading.studentAnswer')}
              value={studentResponse.freeTextAnswer}
              emptyLabel={t('grading.noAnswer')}
            />
          </>
        )}

        {!isFreeText && (
          <div className="space-y-2">
            <p className="mb-2 text-sm font-semibold text-muted-foreground">
              {t('grading.selectedOptions')}:
            </p>
            <div className="grid gap-2 sm:grid-cols-2">
              {questionDef.options.map((option) => {
                const isSelected = studentResponse.selectedOptionIds.includes(
                  option.id,
                )

                return (
                  <div
                    key={option.id}
                    className={`flex flex-col gap-3 rounded-lg border p-3 ${
                      isSelected
                        ? 'border-primary bg-primary/5 text-foreground'
                        : 'border-border bg-card text-muted-foreground opacity-60'
                    }`}
                  >
                    {option.mediaAssetId && (
                      <div className="overflow-hidden rounded-md">
                        <MediaAssetViewer
                          assetId={option.mediaAssetId}
                          isOption
                        />
                      </div>
                    )}

                    <div className="flex items-center gap-3">
                      {isSelected ? (
                        <CheckSquare className="h-5 w-5 text-primary" />
                      ) : (
                        <Square className="h-5 w-5" />
                      )}
                      <span className="text-sm font-medium">{option.text}</span>
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
        )}

        {isTeacher && isFreeText && (
          <div className="mt-6 flex flex-wrap items-end gap-4 rounded-lg border border-border bg-card p-4 shadow-sm">
            <div className="min-w-37.5 flex-1">
              <label className="mb-2 block text-sm font-medium text-foreground">
                {t('grading.assignPoints')} (0 - {maxPoints})
              </label>
              <Input
                type="number"
                min={0}
                max={maxPoints}
                value={gradeInput}
                onChange={(e) =>
                  setGradeInput(
                    e.target.value === '' ? '' : Number(e.target.value),
                  )
                }
                className="max-w-37.5"
              />
            </div>

            <div className="flex items-center gap-3">
              <AsyncButton
                onClick={handleSaveGrade}
                isLoading={gradeMutation.isPending}
                loadingText={t('common.saving')}
                className="min-w-24 gap-2"
              >
                <Save className="h-4 w-4" />
                {t('common.save')}
              </AsyncButton>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
