import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertCircle, Save, Heart, CheckSquare, Square } from 'lucide-react'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { AsyncButton } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Spinner } from '@/components/ui/Spinner'

import type { QuestionResponseDto } from '@/api/generated/model'
import {
  getGetApiQuizAttemptsIdQueryKey,
  usePatchApiQuizAttemptsResponsesResponseIdGrade,
} from '@/api/generated/endpoints/quiz-attempts/quiz-attempts'
import { useGetApiQuizzesQuizIdQuestionsQuestionIdDetailed } from '@/api/generated/endpoints/quizzes/quizzes'

interface QuestionResultCardProps {
  quizId: number | string
  attemptId: number | string
  studentResponse: QuestionResponseDto
  isTeacher: boolean
}

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
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0 pb-2">
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
      </CardHeader>

      <CardContent className="space-y-4 pt-4">
        {isFreeText && (
          <div className="rounded-md bg-muted/50 p-4">
            <p className="mb-1 text-sm font-semibold text-muted-foreground">
              {t('grading.studentAnswer')}:
            </p>
            <p className="text-base whitespace-pre-wrap text-foreground">
              {studentResponse.freeTextAnswer || (
                <span className="text-muted-foreground italic">
                  {t('grading.noAnswer')}
                </span>
              )}
            </p>
          </div>
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
                    className={`flex items-center gap-3 rounded-lg border p-3 ${
                      isSelected
                        ? 'border-primary bg-primary/5 text-foreground'
                        : 'border-border bg-card text-muted-foreground opacity-60'
                    }`}
                  >
                    {isSelected ? (
                      <CheckSquare className="h-5 w-5 text-primary" />
                    ) : (
                      <Square className="h-5 w-5" />
                    )}
                    <span className="text-sm font-medium">{option.text}</span>
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
