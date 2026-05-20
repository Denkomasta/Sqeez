import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { School, Search, Plus } from 'lucide-react'

import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { Pagination } from '@/components/ui/Pagination'
import { Button } from '@/components/ui/Button'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { useGetApiClasses } from '@/api/generated/endpoints/school-classes/school-classes'
import type { SchoolClassDto } from '@/api/generated/model'

import { AdminSchoolClassTable } from './AdminSchoolClassTable'
import { TeacherModificationModal } from './TeacherModificationModal'
import { CreateSchoolClassModal } from './CreateSchoolClassModal'
import { DeleteSchoolClassModal } from './DeleteSchoolClassModal'

/**
 * Admin school-class list page.
 * Coordinates class search, pagination, creation, teacher reassignment, and deletion.
 */
export function AdminSchoolClassPage() {
  const { t } = useTranslation()

  const [searchQuery, setSearchQuery] = useState('')
  const [academicYearFilter, setAcademicYearFilter] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 15

  const [selectedClassForTeacher, setSelectedClassForTeacher] =
    useState<SchoolClassDto | null>(null)
  const [selectedClassForDeletion, setSelectedClassForDeletion] =
    useState<SchoolClassDto | null>(null)
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)

  const { data: classesResponse, isLoading } = useGetApiClasses({
    SearchTerm: searchQuery || undefined,
    AcademicYear: academicYearFilter || undefined,
    PageNumber: pageNumber,
    PageSize: pageSize,
  })

  const classes = classesResponse?.data || []
  const totalPages = Number(classesResponse?.totalPages || 1)
  const totalCount = classesResponse?.totalCount || 0

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-7xl"
        title={
          <span className="flex items-center gap-3">
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <School className="h-6 w-6" />
            </span>
            {t('admin.classes.classManagement')}
          </span>
        }
        subtitle={
          <>
            {t('admin.classes.totalClasses')}:{' '}
            <span className="font-bold">{totalCount}</span>
          </>
        }
        headerActions={
          <Button onClick={() => setIsCreateModalOpen(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            {t('admin.classes.createClass')}
          </Button>
        }
        headerControls={
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center">
            <DebouncedInput
              id="admin-class-search"
              value={searchQuery}
              onChange={(val) => {
                setSearchQuery(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.classes.searchClasses')}
              icon={<Search className="h-4 w-4" />}
              className="max-w-md bg-background"
              hideErrors
            />

            <DebouncedInput
              id="admin-academic-year"
              value={academicYearFilter}
              onChange={(val) => {
                setAcademicYearFilter(val)
                setPageNumber(1)
              }}
              placeholder={t('admin.classes.filterYear')}
              className="max-w-50 bg-background"
              hideErrors
            />
          </div>
        }
      >
        <AdminSchoolClassTable
          classes={classes}
          isLoading={isLoading}
          onEditTeacher={setSelectedClassForTeacher}
          onDeleteClass={setSelectedClassForDeletion}
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

      <TeacherModificationModal
        key={selectedClassForTeacher?.id ?? 'teacher-modal'}
        isOpen={!!selectedClassForTeacher}
        onClose={() => setSelectedClassForTeacher(null)}
        schoolClass={selectedClassForTeacher}
      />

      <CreateSchoolClassModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
      />

      <DeleteSchoolClassModal
        key={selectedClassForDeletion?.id ?? 'delete-modal'}
        isOpen={!!selectedClassForDeletion}
        onClose={() => setSelectedClassForDeletion(null)}
        schoolClass={selectedClassForDeletion}
      />
    </>
  )
}
