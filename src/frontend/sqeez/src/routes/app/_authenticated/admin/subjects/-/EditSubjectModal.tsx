import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

import { usePatchApiSubjectsId } from '@/api/generated/endpoints/subjects/subjects'
import { getGetApiSubjectsQueryKey } from '@/api/generated/endpoints/subjects/subjects'
import { useGetApiUsersInfinite } from '@/hooks/useGetApiUsersInfinite'
import { useGetApiClassesInfinite } from '@/hooks/useGetApiClassesInfinite'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import { DateTimePicker, Input } from '@/components/ui/Input'
import { TextArea } from '@/components/ui/TextArea'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { formatName } from '@/lib/userHelpers'
import { toUtcIsoString } from '@/lib/dateHelpers'
import type { SubjectDto } from '@/api/generated/model'

interface EditSubjectModalProps {
  isOpen: boolean
  onClose: () => void
  subject: SubjectDto | null
}

export function EditSubjectModal({
  isOpen,
  onClose,
  subject,
}: EditSubjectModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const editSubjectSchema = z.object({
    name: z.string().min(1, t('common.required')),
    code: z.string().min(1, t('common.required')),
    description: z.string().optional(),
    startDate: z.string().nullable().optional(),
    endDate: z.string().nullable().optional(),
    teacherId: z.union([z.number(), z.literal('')]).optional(),
    schoolClassId: z.union([z.number(), z.literal('')]).optional(),
  })

  type EditSubjectFormValues = z.infer<typeof editSubjectSchema>

  const defaultEmptyValues: EditSubjectFormValues = {
    name: '',
    code: '',
    description: '',
    startDate: null,
    endDate: null,
    teacherId: '',
    schoolClassId: '',
  }

  const mappedSubjectValues: EditSubjectFormValues | undefined = subject
    ? {
        name: subject.name,
        code: subject.code,
        description: subject.description || '',
        startDate: subject.startDate,
        endDate: subject.endDate,
        teacherId: subject.teacherId ? Number(subject.teacherId) : '',
        schoolClassId: subject.schoolClassId
          ? Number(subject.schoolClassId)
          : '',
      }
    : undefined

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isValid, isDirty },
  } = useForm<EditSubjectFormValues>({
    resolver: zodResolver(editSubjectSchema),
    mode: 'onChange',
    defaultValues: defaultEmptyValues,
    values: mappedSubjectValues,
    resetOptions: {
      keepDirtyValues: false,
    },
  })

  const [teacherSearch, setTeacherSearch] = useState('')
  const [classSearch, setClassSearch] = useState('')

  const {
    data: teacherData,
    isLoading: isLoadingTeachers,
    isFetchingNextPage: isFetchingTeachersNext,
    hasNextPage: hasTeacherNext,
    fetchNextPage: fetchTeachersNext,
  } = useGetApiUsersInfinite(
    {
      Role: 'Teacher',
      ...(teacherSearch ? { SearchTerm: teacherSearch } : {}),
      PageSize: 20,
    },
    { enabled: isOpen },
  )

  const {
    data: classData,
    isLoading: isLoadingClasses,
    isFetchingNextPage: isFetchingClassesNext,
    hasNextPage: hasClassNext,
    fetchNextPage: fetchClassesNext,
  } = useGetApiClassesInfinite(
    { ...(classSearch ? { SearchTerm: classSearch } : {}), PageSize: 20 },
    { enabled: isOpen },
  )

  const editSubjectMutation = usePatchApiSubjectsId({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiSubjectsQueryKey() })
        toast.success(t('admin.subjects.subjectUpdated'))
        handleClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleClose = () => {
    setTeacherSearch('')
    setClassSearch('')
    onClose()
  }

  const onSubmit = async (data: EditSubjectFormValues) => {
    if (!subject) return

    try {
      await editSubjectMutation.mutateAsync({
        id: subject.id.toString(),
        data: {
          name: data.name,
          code: data.code,
          description: data.description || null,
          startDate: toUtcIsoString(data.startDate),
          endDate: toUtcIsoString(data.endDate),
          teacherId: data.teacherId === '' ? null : data.teacherId,
          schoolClassId: data.schoolClassId === '' ? null : data.schoolClassId,
        },
      })
      handleClose()
    } catch (error) {
      console.error('Failed to update subject:', error)
    }
  }

  const teachers = teacherData?.pages.flatMap((page) => page.data || []) || []
  const teacherOptions = [
    {
      id: '',
      title: t('admin.unassignedTeacher'),
      subtitle: t('common.leaveEmpty'),
    },
    ...teachers.map((t) => ({
      id: Number(t.id),
      title: formatName(t.firstName, t.lastName),
      subtitle: `@${t.username}`,
    })),
  ]

  const classes = classData?.pages.flatMap((page) => page.data || []) || []
  const classOptions = [
    {
      id: '',
      title: t('admin.unassignedClass'),
      subtitle: t('common.leaveEmpty'),
    },
    ...classes.map((c) => ({
      id: Number(c.id),
      title: c.name,
      subtitle: `${t('admin.section')}: ${c.section} (${c.academicYear})`,
    })),
  ]

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('admin.subjects.editSubjectTitle')}
      description={t('admin.subjects.editSubjectDesc', {
        subjectName: subject?.name,
      })}
      className="max-w-2xl"
      footer={
        <div className="flex w-full justify-center gap-4 sm:space-x-0">
          <Button
            variant="outline"
            size="lg"
            onClick={handleClose}
            className="min-w-32"
          >
            {t('common.cancel')}
          </Button>
          <AsyncButton
            size="lg"
            onClick={handleSubmit(onSubmit)}
            disabled={!isValid || !isDirty}
            isLoading={editSubjectMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.save')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex max-h-[60vh] flex-col gap-6 overflow-y-auto py-4 pr-2">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <Input
              label={t('admin.subjects.nameLabel')}
              placeholder={t('admin.subjects.namePlaceholder')}
              error={errors.name?.message}
              required
              {...register('name')}
            />
          </div>

          <div className="sm:col-span-2">
            <Input
              label={t('admin.subjects.codeLabel')}
              placeholder={t('admin.subjects.codePlaceholder')}
              error={errors.code?.message}
              required
              {...register('code')}
            />
          </div>

          <div className="sm:col-span-2">
            <TextArea
              label={`${t('admin.subjects.descriptionLabel')} (${t('common.optional')})`}
              placeholder={t('admin.subjects.descriptionPlaceholder')}
              error={errors.description?.message}
              {...register('description')}
            />
          </div>
        </div>

        <hr className="border-border" />

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <Controller
              name="startDate"
              control={control}
              render={({ field }) => (
                <DateTimePicker
                  label={t('admin.subjects.startDateLabel')}
                  error={errors.startDate?.message}
                  value={field.value}
                  onChange={field.onChange}
                />
              )}
            />
          </div>
          <div>
            <Controller
              name="endDate"
              control={control}
              render={({ field }) => (
                <DateTimePicker
                  label={t('admin.subjects.endDateLabel')}
                  error={errors.endDate?.message}
                  value={field.value}
                  onChange={field.onChange}
                  min={control._formValues.startDate}
                />
              )}
            />
          </div>
        </div>

        <hr className="border-border" />

        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div className="flex flex-col gap-3">
            <DebouncedInput
              id="edit-subject-teacher-search"
              label={`${t('common.teacher')} (${t('common.optional')})`}
              value={teacherSearch}
              onChange={setTeacherSearch}
              placeholder={t('admin.classes.searchTeachersPlaceholder')}
              icon={<Search className="size-4" />}
              hideErrors
            />
            <Controller
              name="teacherId"
              control={control}
              render={({ field }) => (
                <ScrollableSelectList
                  options={teacherOptions}
                  selectedId={field.value ?? ''}
                  onSelect={(id) => field.onChange(id === '' ? '' : Number(id))}
                  isLoading={isLoadingTeachers}
                  loadingText={`${t('common.loading')}...`}
                  emptyText={t('common.noResults')}
                  hasMore={!!hasTeacherNext}
                  isFetchingNextPage={isFetchingTeachersNext}
                  onLoadMore={() => fetchTeachersNext()}
                  maxHeight="max-h-50"
                />
              )}
            />
          </div>

          <div className="flex flex-col gap-3">
            <DebouncedInput
              id="edit-subject-class-search"
              value={classSearch}
              label={`${t('admin.schoolClass')} (${t('common.optional')})`}
              onChange={setClassSearch}
              placeholder={t('admin.searchClasses')}
              icon={<Search className="size-4" />}
              hideErrors
            />
            <Controller
              name="schoolClassId"
              control={control}
              render={({ field }) => (
                <ScrollableSelectList
                  options={classOptions}
                  selectedId={field.value ?? ''}
                  onSelect={(id) => field.onChange(id === '' ? '' : Number(id))}
                  isLoading={isLoadingClasses}
                  loadingText={`${t('common.loading')}...`}
                  emptyText={t('common.noResults')}
                  hasMore={!!hasClassNext}
                  isFetchingNextPage={isFetchingClassesNext}
                  onLoadMore={() => fetchClassesNext()}
                  maxHeight="max-h-50"
                />
              )}
            />
          </div>
        </div>
      </div>
    </BaseModal>
  )
}
