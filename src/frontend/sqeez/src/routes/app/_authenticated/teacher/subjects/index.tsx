import { createFileRoute } from '@tanstack/react-router'
import { TeacherSubjectsView } from './-/TeacherSubjectsView'

/** Route entry for teacher subject management. */
export const Route = createFileRoute('/app/_authenticated/teacher/subjects/')({
  component: TeacherSubjectsView,
})
