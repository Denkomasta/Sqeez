import { createFileRoute } from '@tanstack/react-router'
import { TeacherQuizzesPage } from './-/TeacherQuizzesPage'

/** Route entry for teacher quiz management. */
export const Route = createFileRoute('/app/_authenticated/teacher/quizzes/')({
  validateSearch: (search: Record<string, unknown>) => {
    return {
      subjectId: search.subjectId as string | undefined,
      activeOnly: search.activeOnly as boolean | undefined,
    }
  },
  component: TeacherQuizzesPage,
})
