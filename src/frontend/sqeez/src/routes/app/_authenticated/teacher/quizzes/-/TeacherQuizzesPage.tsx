import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FileUp, Filter, Plus } from 'lucide-react'
import { isAxiosError } from 'axios'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import {
  getGetApiQuizzesQueryKey,
  useDeleteApiQuizzesQuizId,
  useGetApiQuizzes,
} from '@/api/generated/endpoints/quizzes/quizzes'
import { useGetApiSubjects } from '@/api/generated/endpoints/subjects/subjects'
import { Button } from '@/components/ui/Button'
import { ConfirmModal } from '@/components/ui'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { QuizListView } from '../../../quizzes/-/QuizListView'
import { useAuthStore } from '@/store/useAuthStore'
import { CreateQuizModal } from './CreateQuizModal'
import { ImportQuizCsvModal } from './ImportQuizCsvModal'
import { CollapsibleSidebar } from '@/components/ui/CollapsibleSidebar'
import type { QuizDto } from '@/api/generated/model'
import type { AspNetProblemDetails } from '@/api/custom-axios'

import { Route } from '../index'

export function TeacherQuizzesPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { user } = useAuthStore()

  const userId = user?.id

  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const selectedSubjectId = search.subjectId || 'all'
  const showActiveOnly = search.activeOnly ?? true

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [isImportModalOpen, setIsImportModalOpen] = useState(false)
  const [quizToDelete, setQuizToDelete] = useState<QuizDto | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)

  const setShowActiveOnly = (activeOnly: boolean) => {
    navigate({
      search: (prev) => ({ ...prev, activeOnly }),
    })
    setPageNumber(1)
  }

  const { data: subjectsData, isLoading: isLoadingSubjects } =
    useGetApiSubjects({ TeacherId: userId }, { query: { enabled: !!userId } })
  const subjects = subjectsData?.data || []

  const quizzesQueryParams = {
    TeacherId: userId,
    SubjectId:
      selectedSubjectId === 'all' ? undefined : Number(selectedSubjectId),
    SearchTerm: searchQuery || undefined,
    PageNumber: pageNumber,
    PageSize: 12,
    IsActive: showActiveOnly,
  }

  const {
    data: quizzesResponse,
    isLoading: isLoadingQuizzes,
    isFetching: isFetchingQuizzes,
  } = useGetApiQuizzes(quizzesQueryParams, { query: { enabled: !!userId } })

  const subjectOptions = [
    { id: 'all', title: t('dashboard.allSubjects') },
    ...subjects.map((subject) => ({
      id: subject.id,
      title: subject.name,
    })),
  ]

  const isAllSubjects = selectedSubjectId === 'all'
  const selectedQuizHasAttempts = Number(quizToDelete?.quizAttempts ?? 0) > 0
  const selectedQuizWillBeClosed = showActiveOnly && selectedQuizHasAttempts

  const getDeleteQuizForbiddenMessage = (error: unknown) => {
    if (
      !isAxiosError<AspNetProblemDetails>(error) ||
      error.response?.status !== 403
    ) {
      return null
    }

    return (
      error.response?.data?.detail ||
      error.response?.data?.title ||
      error.response?.data?.error ||
      null
    )
  }

  const getDeleteQuizErrorToast = (error: unknown) => {
    const forbiddenMessage = getDeleteQuizForbiddenMessage(error)

    if (forbiddenMessage?.includes('subject')) {
      return t('quiz.closedSubjectDeleteFailed')
    }

    if (forbiddenMessage?.includes('permission')) {
      return t('quiz.deletePermissionDenied')
    }

    return t('quiz.quizDeleteFailed')
  }

  const deleteQuizMutation = useDeleteApiQuizzesQuizId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: getGetApiQuizzesQueryKey(),
        })
        if (selectedQuizWillBeClosed) {
          toast.info(t('quiz.quizClosed'))
          return
        }

        toast.success(t('quiz.quizDeleted'))
      },
      onError: (error) => {
        toast.error(getDeleteQuizErrorToast(error))
      },
    },
  })

  const handleDeleteQuizConfirm = async () => {
    if (!quizToDelete) return

    try {
      await deleteQuizMutation.mutateAsync({ quizId: quizToDelete.id })
      setQuizToDelete(null)
    } catch (error) {
      console.error(error)
    }
  }

  const handleQuizImported = () => {
    queryClient.invalidateQueries({
      queryKey: getGetApiQuizzesQueryKey(quizzesQueryParams),
    })
  }

  const quizActions = isAllSubjects ? (
    <div className="flex flex-wrap gap-2">
      <div
        title={t('dashboard.selectSubjectToCreate')}
        className="cursor-not-allowed"
      >
        <Button disabled size="sm" className="w-fit gap-1 shadow-md">
          <Plus className="h-4 w-4" />
          {t('dashboard.createNewQuiz')}
        </Button>
      </div>
      <div
        title={t('dashboard.selectSubjectToImport')}
        className="cursor-not-allowed"
      >
        <Button disabled size="sm" variant="outline" className="w-fit gap-1">
          <FileUp className="h-4 w-4" />
          {t('dashboard.importQuizCsv')}
        </Button>
      </div>
    </div>
  ) : (
    <div className="flex flex-wrap gap-2">
      <Button
        size="sm"
        className="w-fit gap-1 shadow-md"
        onClick={() => setIsCreateModalOpen(true)}
      >
        <Plus className="h-4 w-4" />
        {t('dashboard.createNewQuiz')}
      </Button>
      <Button
        size="sm"
        variant="outline"
        className="w-fit gap-1"
        onClick={() => setIsImportModalOpen(true)}
      >
        <FileUp className="h-4 w-4" />
        {t('dashboard.importQuizCsv')}
      </Button>
    </div>
  )

  const titleNode = t('dashboard.myQuizzes')

  return (
    <div className="flex h-full w-full flex-1 flex-col overflow-hidden bg-background lg:flex-row">
      <CollapsibleSidebar
        title={t('dashboard.filterBySubject')}
        icon={<Filter className="h-4 w-4 text-primary" />}
        expandedWidth="w-full lg:w-75"
        expandTooltip={t('dashboard.showFilters')}
        collapseTooltip={t('dashboard.hideFilters')}
        className="border-b lg:border-r lg:border-b-0"
      >
        <div className="sticky top-6 px-3">
          <ScrollableSelectList
            options={subjectOptions}
            selectedId={isAllSubjects ? 'all' : Number(selectedSubjectId)}
            onSelect={(id) => {
              navigate({
                search: (prev) => ({ ...prev, subjectId: String(id) }),
              })
              setPageNumber(1)
            }}
            isLoading={isLoadingSubjects}
            loadingText={t('common.loading')}
            emptyText={t('dashboard.noSubjectsFound')}
            maxHeight="max-h-[60vh]"
          />
        </div>
      </CollapsibleSidebar>

      <section className="flex-1 overflow-y-auto">
        <QuizListView
          role="Teacher"
          titleNode={titleNode}
          headerActions={quizActions}
          quizzes={quizzesResponse?.data || []}
          totalQuizzes={Number(quizzesResponse?.totalCount || 0)}
          totalPages={Number(quizzesResponse?.totalPages || 1)}
          isLoading={isLoadingQuizzes}
          isFetching={isFetchingQuizzes}
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          pageNumber={pageNumber}
          setPageNumber={setPageNumber}
          emptyStateMessage={t('dashboard.createFirstQuizPrompt')}
          subject={
            !isAllSubjects
              ? subjects.find((s) => s.id === Number(selectedSubjectId))
              : undefined
          }
          showActiveToggle={true}
          showActiveOnly={showActiveOnly}
          setShowActiveOnly={setShowActiveOnly}
          onDeleteQuiz={setQuizToDelete}
          pendingDeleteQuizId={deleteQuizMutation.variables?.quizId}
        />
      </section>
      <CreateQuizModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        subjectId={selectedSubjectId}
      />
      {!isAllSubjects && (
        <ImportQuizCsvModal
          isOpen={isImportModalOpen}
          onClose={() => setIsImportModalOpen(false)}
          onImported={handleQuizImported}
          subjectId={selectedSubjectId}
        />
      )}

      <ConfirmModal
        isOpen={!!quizToDelete}
        onClose={() => setQuizToDelete(null)}
        onConfirm={handleDeleteQuizConfirm}
        title={t('quiz.deleteQuizTitle')}
        description={
          selectedQuizHasAttempts
            ? t('quiz.deleteQuizWithAttemptsConfirm', {
                title: quizToDelete?.title,
              })
            : t('quiz.deleteQuizWithoutAttemptsConfirm', {
                title: quizToDelete?.title,
              })
        }
        confirmText={t('quiz.deleteQuiz')}
        isDestructive
        isLoading={deleteQuizMutation.isPending}
      />
    </div>
  )
}
