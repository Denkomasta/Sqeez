import type { DetailedQuizQuestionDto } from '@/api/generated/model'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { QuizOptionItem } from '../QuizOptionItem'
import { useTranslation } from 'react-i18next'

interface QuestionCardProps {
  question: DetailedQuizQuestionDto
  selectedOptionIds: (number | string)[]
  onSelectOption: (optionId: number | string) => void
  renderMediaAsset?: (
    assetId: number | string,
    isOption?: boolean,
  ) => React.ReactNode
  freeTextValue?: string
  onChangeFreeText?: (text: string) => void
}

/**
 * Displays the active quiz question and delegates answer input to option items.
 * Media rendering is injected so callers control fetching and asset presentation.
 *
 * @param props.question - Detailed question returned by the student quiz endpoint.
 * @param props.selectedOptionIds - Currently selected option ids; free-text answers still select the free-text option.
 * @param props.renderMediaAsset - Optional renderer for question/option media assets.
 */
export function QuestionCard({
  question,
  selectedOptionIds,
  onSelectOption,
  renderMediaAsset,
  freeTextValue = '',
  onChangeFreeText,
}: QuestionCardProps) {
  const { t } = useTranslation()

  const isMultiChoice = question.isStrictMultipleChoice === true

  return (
    <Card className="flex-1 overflow-hidden border-0 bg-transparent shadow-none">
      <CardHeader className="mb-6 rounded-3xl border-4 border-primary/10 bg-card pb-8 text-center shadow-sm">
        <div className="space-y-3">
          <CardTitle className="text-2xl leading-relaxed font-black tracking-wide text-primary md:text-4xl">
            {question.title}
          </CardTitle>

          <div className="inline-block rounded-full bg-muted/50 px-4 py-1 text-sm font-bold text-muted-foreground">
            {isMultiChoice
              ? t('quiz.selectMultipleHint')
              : t('quiz.selectSingleHint')}
          </div>
        </div>

        {question.mediaAssetId && renderMediaAsset && (
          <div className="mx-auto mt-6 max-w-xl overflow-hidden rounded-2xl border-4 border-primary/5 shadow-md">
            {renderMediaAsset(question.mediaAssetId, false)}
          </div>
        )}
      </CardHeader>

      <CardContent className="p-0">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 md:gap-6">
          {question.options.map((option, index) => (
            <QuizOptionItem
              key={option.id}
              option={option}
              index={index}
              isSelected={selectedOptionIds.includes(option.id)}
              isMultiChoice={isMultiChoice}
              onSelect={() => onSelectOption(option.id)}
              freeTextValue={freeTextValue}
              onFreeTextChange={(text) => onChangeFreeText?.(text)}
              mediaNode={
                option.mediaAssetId && renderMediaAsset
                  ? renderMediaAsset(option.mediaAssetId, true)
                  : undefined
              }
            />
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
