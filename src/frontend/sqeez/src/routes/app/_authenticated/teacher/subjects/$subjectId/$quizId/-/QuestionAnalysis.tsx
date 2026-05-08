import { useTranslation } from 'react-i18next'
import {
  CheckCircle2,
  XCircle,
  Clock,
  Target,
  Users,
  MessageSquare,
  Lightbulb,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import type { QuestionStatDto } from '@/api/generated/model'

export function QuestionAnalysis({
  questions,
}: {
  questions: QuestionStatDto[]
}) {
  const { t } = useTranslation()

  if (!questions || questions.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="mb-2">
        <h2 className="text-xl font-semibold tracking-tight">
          {t('quiz.questionAnalysis')}
        </h2>
        <p className="text-sm text-muted-foreground">
          {t('quiz.questionAnalysisDesc')}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {questions.map((question, index) => {
          const totalAnswers = Number(question.totalAnswers || 0)
          const avgScore = Number(question.averageScore || 0)
          const avgTimeSeconds = Number(
            question.averageResponseTimeSeconds || 0,
          )

          const expectedOption =
            (question.options || []).find((o) => o.isCorrect) ||
            (question.options || [])[0]

          return (
            <Card key={question.id} className="border-border shadow-sm">
              <CardHeader className="pb-3">
                <CardTitle className="text-base leading-relaxed">
                  <span className="mr-2 text-muted-foreground">
                    {index + 1}.
                  </span>
                  {question.questionText}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="mb-5 flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
                  <div className="flex items-center gap-1.5 rounded-md bg-muted/50 px-2 py-1">
                    <Users className="h-3.5 w-3.5" />
                    <span>
                      {totalAnswers} {t('quiz.responses')}
                    </span>
                  </div>
                  <div className="flex items-center gap-1.5 rounded-md bg-muted/50 px-2 py-1">
                    <Target className="h-3.5 w-3.5" />
                    <span>
                      {avgScore.toFixed(1)} {t('quiz.avgScore')}
                    </span>
                  </div>
                  <div className="flex items-center gap-1.5 rounded-md bg-muted/50 px-2 py-1">
                    <Clock className="h-3.5 w-3.5" />
                    <span>
                      {avgTimeSeconds.toFixed(1)}s {t('quiz.avgTime')}
                    </span>
                  </div>
                  {question.isFreeText && (
                    <div className="flex items-center gap-1.5 rounded-md bg-muted/50 px-2 py-1">
                      <MessageSquare className="h-3.5 w-3.5" />
                      <span>{t('quiz.freeText')}</span>
                    </div>
                  )}
                </div>

                {question.isFreeText ? (
                  <div className="flex flex-col gap-5">
                    {expectedOption && (
                      <div className="flex flex-col gap-1.5 rounded-md border border-success/20 bg-success/10 p-3">
                        <div className="flex items-center gap-1.5 text-xs font-semibold text-success">
                          <Lightbulb className="h-3.5 w-3.5" />
                          {t('quiz.expectedAnswer')}
                        </div>
                        <span className="text-sm font-medium text-foreground">
                          {expectedOption.text}
                        </span>
                      </div>
                    )}

                    <div className="flex flex-col gap-3">
                      <div className="text-sm font-medium text-muted-foreground">
                        {t('quiz.submittedAnswers')}
                      </div>
                      {question.submittedFreeTextAnswers &&
                      question.submittedFreeTextAnswers.length > 0 ? (
                        <div className="flex max-h-48 flex-col gap-2 overflow-y-auto rounded-md border border-border p-2">
                          {question.submittedFreeTextAnswers.map(
                            (answer, i) => (
                              <div
                                key={i}
                                className="rounded-sm bg-muted/40 px-3 py-2 text-sm text-foreground"
                              >
                                {answer}
                              </div>
                            ),
                          )}
                        </div>
                      ) : (
                        <p className="text-sm text-muted-foreground italic">
                          {t('quiz.noAnswersYet')}
                        </p>
                      )}
                    </div>
                  </div>
                ) : (
                  <div className="flex flex-col gap-4">
                    {(question.options || []).map((option) => {
                      const pickCount = Number(option.pickCount || 0)

                      const percentage =
                        totalAnswers > 0
                          ? Math.round((pickCount / totalAnswers) * 100)
                          : 0

                      return (
                        <div key={option.id} className="flex flex-col gap-1.5">
                          <div className="flex items-start justify-between gap-4 text-sm">
                            <div className="flex flex-1 items-start gap-2">
                              {option.isCorrect ? (
                                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-success" />
                              ) : (
                                <XCircle className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground/30" />
                              )}
                              <span
                                className={
                                  option.isCorrect
                                    ? 'font-medium text-foreground'
                                    : 'text-muted-foreground'
                                }
                              >
                                {option.text}
                              </span>
                            </div>
                            <div className="flex items-center gap-2 text-right">
                              <span className="font-medium text-foreground">
                                {percentage}%
                              </span>
                              <span className="w-12 text-xs text-muted-foreground">
                                ({pickCount})
                              </span>
                            </div>
                          </div>

                          <div className="ml-6 h-2 overflow-hidden rounded-full bg-muted">
                            <div
                              className={`h-full rounded-full transition-all duration-1000 ease-out ${
                                option.isCorrect
                                  ? 'bg-success'
                                  : 'bg-primary/40'
                              }`}
                              style={{ width: `${percentage}%` }}
                            />
                          </div>
                        </div>
                      )
                    })}
                  </div>
                )}
              </CardContent>
            </Card>
          )
        })}
      </div>
    </div>
  )
}
