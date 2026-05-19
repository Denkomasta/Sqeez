import { createFileRoute } from '@tanstack/react-router'

import { useQuizEditorUIStore } from '@/store/useQuizEditorUIStore'
import { useEffect } from 'react'
import { QuizEditorSidebar } from './-/QuizEditorSidebar'
import { QuizQuestionEditor } from './-/QuizQuestionEditor'
import { teacherRouteGuard } from '@/lib/routeGuards'

export const Route = createFileRoute(
  '/app/_authenticated/quizzes/$quizId/builder/',
)({
  beforeLoad: teacherRouteGuard,
  component: QuizEditorPage,
})

export function QuizEditorPage() {
  const { actions } = useQuizEditorUIStore()
  const { quizId } = Route.useParams()

  useEffect(() => {
    return () => actions.resetEditor()
  }, [actions])

  return (
    <div className="flex h-[calc(100svh-4rem)] max-h-[calc(100svh-4rem)] min-h-0 w-full flex-col bg-background">
      <div className="flex min-h-0 flex-1">
        <QuizEditorSidebar quizId={quizId} />

        <QuizQuestionEditor quizId={quizId} />
      </div>
    </div>
  )
}
