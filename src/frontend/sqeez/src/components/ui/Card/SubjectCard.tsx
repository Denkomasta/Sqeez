import type { ReactNode } from 'react'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge/Badge'
import { Link } from '@tanstack/react-router'

/** Shared subject card contract for student and teacher subject lists. */
export interface SubjectCardProps {
  title: string
  code: string
  url: string
  params?: Record<string, string>
  borderColorClass: string
  badgeColorClass?: string
  topRightSlot?: ReactNode
  description?: string | null // Mostly for teachers
  metricsSlot?: ReactNode // E.g., Enroll date OR Student/Quiz counts
  actionsSlot?: ReactNode // E.g., Delete button OR Settings button
}

/**
 * Subject summary card with optional metrics and action slots.
 *
 * @param props.url - Router target opened when the subject title is clicked.
 * @param props.metricsSlot - Optional subject metadata such as enroll date or counts.
 * @param props.actionsSlot - Optional actions kept separate from title navigation.
 */
export function SubjectCard({
  title,
  code,
  url,
  params,
  borderColorClass,
  badgeColorClass = 'bg-secondary text-secondary-foreground',
  topRightSlot,
  description,
  metricsSlot,
  actionsSlot,
}: SubjectCardProps) {
  return (
    <Card
      className={`flex flex-col justify-between border-l-4 transition-all hover:shadow-md ${borderColorClass}`}
    >
      <CardHeader className="pb-3">
        <div className="mb-2 flex items-center justify-between">
          <Badge variant="outline" className={badgeColorClass}>
            {code}
          </Badge>
          {topRightSlot && <div>{topRightSlot}</div>}
        </div>

        <CardTitle className="text-xl">
          <Link
            to={url}
            params={params}
            className="line-clamp-2 font-bold transition-colors hover:underline hover:opacity-80"
          >
            {title}
          </Link>
        </CardTitle>

        {description && (
          <CardDescription className="mt-1 line-clamp-2">
            {description}
          </CardDescription>
        )}
      </CardHeader>

      <CardContent className="mt-auto">
        {metricsSlot && (
          <div className="mb-4 flex items-center gap-4 text-sm text-muted-foreground">
            {metricsSlot}
          </div>
        )}

        {actionsSlot && (
          <div className="flex items-center justify-end border-t pt-4">
            {actionsSlot}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
