import { useTranslation } from 'react-i18next'
import {
  Controller,
  useFieldArray,
  useFormContext,
  useWatch,
  type FieldError,
} from 'react-hook-form'
import { Trash2, Plus } from 'lucide-react'

import { Input } from '@/components/ui/Input'
import { Button } from '@/components/ui/Button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/Select'

import {
  OPERATOR_MAP,
  OPERATOR_TRANSLATIONS,
  SUPPORTED_METRICS_TRANSLATIONS,
} from '@/constants/badgeRulesMappings'
import type { BadgeFormValues } from '@/schemas/badgeSchema'

export function BadgeRulesBuilder() {
  const { t } = useTranslation()

  const {
    control,
    register,
    formState: { errors },
  } = useFormContext<BadgeFormValues>()

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'rules',
  })
  const rules = useWatch({ control, name: 'rules' }) || []
  const nextAvailableMetric = (
    Object.keys(
      SUPPORTED_METRICS_TRANSLATIONS,
    ) as BadgeFormValues['rules'][number]['metric'][]
  ).find((metric) => !rules.some((rule) => rule.metric === metric))

  const rulesRootError = errors.rules?.root as FieldError | undefined
  const rulesArrayError = errors.rules as FieldError | undefined
  const rulesErrorMessage = rulesRootError?.message || rulesArrayError?.message
  const metricOptions = Object.entries(SUPPORTED_METRICS_TRANSLATIONS).map(
    ([key, value]) => ({
      id: key,
      title: t(value, key),
    }),
  )
  const operatorOptions = Object.entries(OPERATOR_MAP).map(([key, symbol]) => ({
    id: key,
    title: symbol,
    subtitle: t(OPERATOR_TRANSLATIONS[key as keyof typeof OPERATOR_MAP], key),
  }))

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <h3 className="text-lg font-semibold text-foreground">
            {t('admin.badges.rulesTitle')}
          </h3>
          <p className="text-sm text-muted-foreground">
            {t('admin.badges.rulesDesc')}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="shrink-0"
          disabled={!nextAvailableMetric}
          onClick={() => {
            if (!nextAvailableMetric) return

            append({
              id: null,
              metric: nextAvailableMetric,
              operator: 'GreaterThanOrEqual',
              targetValue: nextAvailableMetric === 'ScorePercentage' ? 80 : 1,
            })
          }}
        >
          <Plus className="mr-2 h-4 w-4" />
          {t('admin.badges.addRule')}
        </Button>
      </div>

      {rulesErrorMessage && (
        <p className="text-sm font-medium text-destructive">
          {rulesErrorMessage}
        </p>
      )}

      <div className="flex flex-col gap-3">
        {fields.map((field, index) => {
          const selectedMetric = rules[index]?.metric ?? field.metric
          const isScorePercentage = selectedMetric === 'ScorePercentage'

          return (
            <div
              key={field.id}
              className="flex flex-col gap-4 rounded-lg border border-border bg-card p-4 shadow-sm md:flex-row md:items-end"
            >
              <div className="min-w-0 flex-1">
                <label className="mb-1 block text-xs font-medium text-muted-foreground">
                  {t('admin.badges.metric')}
                </label>
                <Controller
                  name={`rules.${index}.metric`}
                  control={control}
                  render={({ field }) => (
                    <Select onValueChange={field.onChange} value={field.value}>
                      <SelectTrigger className="h-10 w-full bg-background">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {metricOptions.map((option) => (
                          <SelectItem
                            key={option.id}
                            value={option.id.toString()}
                          >
                            {option.title}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>

              <div className="flex w-full items-end gap-3 md:w-auto">
                <div className="min-w-0 flex-1 md:w-36 md:flex-none">
                  <label className="mb-1 block text-xs font-medium text-muted-foreground">
                    {t('admin.badges.operator')}
                  </label>
                  <Controller
                    name={`rules.${index}.operator`}
                    control={control}
                    render={({ field }) => (
                      <Select
                        onValueChange={field.onChange}
                        value={field.value}
                      >
                        <SelectTrigger className="h-10 w-full bg-background">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {operatorOptions.map((option) => (
                            <SelectItem
                              key={option.id}
                              value={option.id.toString()}
                            >
                              <span className="mr-2 font-bold text-primary">
                                {option.title}
                              </span>
                              <span className="text-xs text-muted-foreground">
                                {option.subtitle}
                              </span>
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                </div>

                <div className="w-28 shrink-0 sm:w-32">
                  <label className="mb-1 block text-xs font-medium text-muted-foreground">
                    {t('admin.badges.value')}
                  </label>
                  <Input
                    type="number"
                    min={0}
                    max={isScorePercentage ? 100 : undefined}
                    {...register(`rules.${index}.targetValue`, {
                      valueAsNumber: true,
                    })}
                    className="h-10"
                    hideErrors
                  />
                </div>

                <div className="flex shrink-0 pb-0.5">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-9 w-9 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                    onClick={() => remove(index)}
                    disabled={fields.length === 1}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
