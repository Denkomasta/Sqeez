import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { BookCopy, Search, Plus } from 'lucide-react'

import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { Button } from '@/components/ui/Button'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import {
  getGetApiSubjectsQueryKey,
  useGetApiSubjects,
} from '@/api/generated/endpoints/subjects/subjects'
import type { SubjectDto } from '@/api/generated/model'

import { AdminSubjectsTable } from './AdminSubjectsTable'
import { CreateSubjectModal } from './CreateSubjectModal'
import { EditSubjectModal } from './EditSubjectModal'
import { DeleteSubjectModal } from './DeleteSubjectModal'

/**
 * Admin subject list page.
 * Coordinates search, pagination, create/edit modals, and the multi-step delete flow.
 */
export function AdminSubjectsPage() {
  const { t } = useTranslation()

  const [searchQuery, setSearchQuery] = useState('')
  const [codeFilter, setCodeFilter] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 15

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [subjectToEdit, setSubjectToEdit] = useState<SubjectDto | null>(null)
  const [subjectToDelete, setSubjectToDelete] = useState<SubjectDto | null>(
    null,
  )

  const subjectsQueryParams = {
    SearchTerm: searchQuery || undefined,
    Code: codeFilter || undefined,
    PageNumber: pageNumber,
    PageSize: pageSize,
  }
  const subjectsQueryKey = getGetApiSubjectsQueryKey(subjectsQueryParams)

  const { data: subjectsResponse, isLoading } =
    useGetApiSubjects(subjectsQueryParams)

  const subjects = subjectsResponse?.data || []
  const totalPages = Number(subjectsResponse?.totalPages || 1)
  const totalCount = subjectsResponse?.totalCount || 0

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        title={
          <span className="flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-nav-cyan/10 text-nav-cyan">
              <BookCopy className="h-6 w-6" />
            </span>
            {t('admin.subjects.subjectManagement')}
          </span>
        }
        subtitle={
          <>
            {t('admin.subjects.totalSubjects')}:{' '}
            <span className="font-bold">{totalCount}</span>
          </>
        }
        headerActions={
          <Button onClick={() => setIsCreateModalOpen(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            {t('admin.subjects.createSubject')}
          </Button>
        }
        headerControls={
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
            <DebouncedInput
              id="admin-subject-search"
              value={searchQuery}
              onChange={(val) => {
                setSearchQuery(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.subjects.searchPlaceholder')}
              icon={<Search className="h-4 w-4" />}
              className="max-w-md bg-background"
              hideErrors
            />

            <DebouncedInput
              id="admin-subject-code"
              value={codeFilter}
              onChange={(val) => {
                setCodeFilter(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.subjects.filterCode')}
              className="max-w-62.5 bg-background"
              hideErrors
            />
          </div>
        }
      >
        <AdminSubjectsTable
          subjects={subjects}
          isLoading={isLoading}
          onEditSubject={setSubjectToEdit}
          onDeleteSubject={setSubjectToDelete}
        />

        {!isLoading && totalPages > 1 && (
          <div className="mt-6 flex justify-center">
            <Pagination
              currentPage={pageNumber}
              totalPages={totalPages}
              onPageChange={setPageNumber}
            />
          </div>
        )}
      </PageLayout>

      <CreateSubjectModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />
      <EditSubjectModal
        isOpen={!!subjectToEdit}
        subject={subjectToEdit}
        onClose={() => setSubjectToEdit(null)}
      />
      <DeleteSubjectModal
        isOpen={!!subjectToDelete}
        subject={subjectToDelete}
        subjectsQueryKey={subjectsQueryKey}
        onClose={() => setSubjectToDelete(null)}
      />
    </>
  )
}
