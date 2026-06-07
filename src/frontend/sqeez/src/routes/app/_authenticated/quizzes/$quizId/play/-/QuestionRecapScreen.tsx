import type { DetailedQuizQuestionDto } from '@/api/generated/model'
import { useTranslation } from 'react-i18next'
import { Check, X, ArrowRight, Clock, Hourglass } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { MediaAssetViewer } from './MediaAssetViewer'

interface QuestionRecapScreenProps {
  question: DetailedQuizQuestionDto
  selectedOptionIds: (number | string)[]
  correctOptionIds: (number | string)[]
  userFreeTextAnswer: string
  correctFreeTextAnswer: string | null
  timeSpentMs: number
  onContinue: () => void
  isLastQuestion: boolean
}

/**
 * Per-question recap shown after submission.
 * Displays scoring feedback and continues through the quiz engine callback.
 */
export function QuestionRecapScreen({
  question,
  selectedOptionIds,
  correctOptionIds,
  userFreeTextAnswer,
  correctFreeTextAnswer,
  timeSpentMs,
  onContinue,
  isLastQuestion,
}: QuestionRecapScreenProps) {
  const { t } = useTranslation()

  const isFreeText = question.options.some((o) => o.isFreeText)

  const isFullyCorrect =
    !isFreeText && question.isStrictMultipleChoice
      ? selectedOptionIds.length === correctOptionIds.length &&
        selectedOptionIds.every((id) => correctOptionIds.includes(id))
      : selectedOptionIds.every((id) => correctOptionIds.includes(id))

  let bannerStyle = ''
  let BannerIcon = null
  let bannerText = ''

  if (isFreeText) {
    bannerStyle = 'border-info/80 bg-info text-info-foreground'
    BannerIcon = Hourglass
    bannerText = t('quiz.pendingGrading')
  } else if (isFullyCorrect) {
    bannerStyle = 'border-success/80 bg-success text-success-foreground'
    BannerIcon = Check
    bannerText = t('quiz.correctAnswer')
  } else {
    bannerStyle =
      'border-destructive/80 bg-destructive text-destructive-foreground'
    BannerIcon = X
    bannerText = t('quiz.incorrectAnswer')
  }

  return (
    <div className="flex min-h-[calc(100vh-4rem)] w-full animate-in flex-col justify-center p-4 duration-500 fade-in md:p-6 md:px-12 lg:p-8 lg:px-16">
      <div
        className={cn(
          'mb-6 flex items-center justify-between rounded-2xl border-4 p-4 font-black shadow-sm md:p-6',
          bannerStyle,
        )}
      >
        <div className="flex items-center gap-4 text-2xl tracking-wide md:text-3xl">
          <BannerIcon className="size-8 stroke-4 md:size-10" />
          <span>{bannerText}</span>
        </div>

        <div className="flex items-center gap-2 rounded-xl bg-black/20 px-4 py-2 text-sm font-bold md:text-lg">
          <Clock className="size-5" />
          {(timeSpentMs / 1000).toFixed(1)}s
        </div>
      </div>

      <Card className="flex-1 overflow-hidden border-0 bg-transparent shadow-none">
        <CardHeader className="mb-6 rounded-3xl border-4 border-primary/10 bg-card pb-8 text-center shadow-sm">
          <CardTitle className="text-xl leading-relaxed font-black tracking-wide text-primary md:text-3xl">
            {question.title}
          </CardTitle>

          {question.mediaAssetId && (
            <div className="mx-auto mt-6 max-w-xl overflow-hidden rounded-2xl border-4 border-primary/5 shadow-md">
              <MediaAssetViewer
                assetId={question.mediaAssetId}
                isOption={false}
              />
            </div>
          )}
        </CardHeader>

        <CardContent className="p-0">
          {isFreeText ? (
            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-6 rounded-3xl border-4 border-info/20 bg-card p-6 shadow-sm md:flex-row">
                <div className="flex min-w-0 flex-1 flex-col gap-3 rounded-2xl border-4 border-muted/30 bg-muted/10 p-6 text-center">
                  <span className="shrink-0 text-sm font-bold tracking-widest text-muted-foreground uppercase">
                    {t('quiz.yourAnswer')}
                  </span>
                  <div className="max-h-48 flex-1 overflow-y-auto pr-2">
                    <span className="w-full text-2xl font-black break-words text-foreground md:text-3xl">
                      {userFreeTextAnswer || '-'}
                    </span>
                  </div>
                </div>

                <div className="flex min-w-0 flex-1 flex-col gap-3 rounded-2xl border-4 border-info/80 bg-info p-6 text-center text-info-foreground">
                  <span className="shrink-0 text-sm font-bold tracking-widest text-info-foreground/80 uppercase">
                    {t('quiz.expectedAnswer')}
                  </span>
                  <div className="max-h-48 flex-1 overflow-y-auto pr-2">
                    <span className="w-full text-2xl font-black break-words md:text-3xl">
                      {correctFreeTextAnswer || '-'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="mt-2 text-center text-lg font-bold text-muted-foreground/80">
                {t('quiz.pendingGradingDesc')}
              </div>
            </div>
          ) : (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:gap-6">
              {question.options.map((option) => {
                const isSelected = selectedOptionIds.includes(option.id)
                const isCorrect = correctOptionIds.includes(option.id)

                let blockStyle = ''
                let Icon = null

                if (isSelected && isCorrect) {
                  blockStyle =
                    'bg-success border-success/80 text-success-foreground border-b-4 translate-y-1 brightness-95'
                  Icon = (
                    <Check className="size-6 stroke-4 text-success-foreground" />
                  )
                } else if (isSelected && !isCorrect) {
                  blockStyle =
                    'bg-destructive border-destructive/80 text-destructive-foreground border-b-4 translate-y-1 brightness-95'
                  Icon = (
                    <X className="size-6 stroke-4 text-destructive-foreground" />
                  )
                } else if (!isSelected && isCorrect) {
                  blockStyle =
                    'bg-success/20 border-success text-success border-b-8 opacity-90'
                  Icon = <Check className="size-6 stroke-4 text-success" />
                } else {
                  blockStyle =
                    'bg-card border-border text-foreground/80 border-b-4 opacity-80'
                }

                return (
                  <div
                    key={option.id}
                    className={cn(
                      'relative flex min-h-24 w-full flex-col items-center justify-center gap-2 rounded-3xl border-4 p-6 text-center transition-all md:min-h-32',
                      blockStyle,
                    )}
                  >
                    {Icon && (
                      <div className="absolute top-4 right-4 flex size-8 items-center justify-center">
                        {Icon}
                      </div>
                    )}

                    {option.mediaAssetId && (
                      <div className="w-full overflow-hidden rounded-xl">
                        <MediaAssetViewer
                          assetId={option.mediaAssetId}
                          isOption
                        />
                      </div>
                    )}

                    <span className="line-clamp-3 w-full text-lg font-black tracking-wide break-words md:text-xl lg:text-2xl">
                      {option.text}
                    </span>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="mt-8 flex justify-end">
        <Button
          size="lg"
          onClick={onContinue}
          className="w-full rounded-2xl border-b-4 border-primary-foreground/30 px-8 py-6 text-xl font-black shadow-md transition-transform hover:-translate-y-1 active:translate-y-1 active:border-b-0 sm:w-auto"
        >
          {isLastQuestion ? t('quiz.finishQuiz') : t('common.continue')}
          <ArrowRight className="ml-2 h-6 w-6 stroke-3" />
        </Button>
      </div>
    </div>
  )
}
