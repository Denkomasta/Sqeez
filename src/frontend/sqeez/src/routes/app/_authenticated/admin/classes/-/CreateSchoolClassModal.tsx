import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useQueryClient } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

import { usePostApiClasses } from '@/api/generated/endpoints/school-classes/school-classes'
import { getGetApiClassesQueryKey } from '@/api/generated/endpoints/school-classes/school-classes'
import { useGetApiUsersInfinite } from '@/hooks/useGetApiUsersInfinite'

import { BaseModal } from '@/components/ui/Modal'
import { Button, AsyncButton } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { formatName } from '@/lib/userHelpers'

interface CreateSchoolClassModalProps {
  isOpen: boolean
  onClose: () => void
}

/**
 * Modal for creating a school class and optionally assigning a teacher.
 * Teacher options are fetched through the shared user selector query.
 */
export function CreateSchoolClassModal({
  isOpen,
  onClose,
}: CreateSchoolClassModalProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const createClassSchema = z.object({
    name: z.string().min(1, t('common.required')),
    academicYear: z
      .string()
      .min(1, t('common.required'))
      .regex(/^\d{4}\/\d{4}$/, t('admin.classes.invalidYearFormat')),
    section: z.string().min(1, t('common.required')),
    teacherId: z.union([z.number(), z.literal('')]).nullable(),
  })

  type CreateClassFormValues = z.infer<typeof createClassSchema>

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isValid },
  } = useForm<CreateClassFormValues>({
    resolver: zodResolver(createClassSchema),
    mode: 'onChange',
    defaultValues: {
      name: '',
      academicYear: '',
      section: '',
      teacherId: '',
    },
  })

  const [teacherSearch, setTeacherSearch] = useState('')

  const {
    data: infiniteData,
    isLoading: isLoadingTeachers,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
  } = useGetApiUsersInfinite(
    {
      Role: 'Teacher',
      HasAssignedClass: false,
      ...(teacherSearch ? { SearchTerm: teacherSearch } : {}),
      PageSize: 20,
    },
    {
      enabled: isOpen,
    },
  )

  const createClassMutation = usePostApiClasses({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetApiClassesQueryKey() })
        toast.success(t('admin.classes.classCreated'))
        handleClose()
      },
      onError: () => toast.error(t('common.error')),
    },
  })

  const handleClose = () => {
    reset()
    setTeacherSearch('')
    onClose()
  }

  const onSubmit = async (data: CreateClassFormValues) => {
    try {
      await createClassMutation.mutateAsync({
        data: {
          name: data.name,
          academicYear: data.academicYear,
          section: data.section,
          teacherId: data.teacherId === '' ? null : data.teacherId,
        },
      })
    } catch (error) {
      console.error('Failed to create class:', error)
    }
  }

  const teachers = infiniteData?.pages.flatMap((page) => page.data || []) || []
  const teacherOptions = [
    {
      id: '',
      title: t('admin.classes.unassigned'),
      subtitle: t('admin.classes.leaveUnassigned'),
    },
    ...teachers.map((teacher) => ({
      id: Number(teacher.id),
      title: formatName(teacher.firstName, teacher.lastName),
      subtitle: `@${teacher.username}`,
    })),
  ]

  return (
    <BaseModal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('admin.classes.createClassTitle')}
      description={t('admin.classes.createClassDesc')}
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
            disabled={!isValid}
            isLoading={createClassMutation.isPending}
            loadingText={t('common.saving') + '...'}
            className="min-w-32"
          >
            {t('common.create')}
          </AsyncButton>
        </div>
      }
    >
      <div className="flex flex-col gap-6 py-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <Input
              label={t('admin.classes.nameLabel')}
              placeholder={t('admin.classes.namePlaceholder')}
              error={errors.name?.message}
              required
              {...register('name')}
            />
          </div>

          <div>
            <Input
              label={t('admin.classes.yearLabel')}
              placeholder={t('admin.classes.yearPlaceholder')}
              error={errors.academicYear?.message}
              required
              {...register('academicYear')}
            />
          </div>

          <div>
            <Input
              label={t('admin.classes.sectionLabel')}
              placeholder={t('admin.classes.sectionPlaceholder')}
              error={errors.section?.message}
              required
              {...register('section')}
            />
          </div>
        </div>

        <hr className="border-border" />

        <div className="flex flex-col gap-3">
          <label className="text-sm font-medium">
            {t('admin.classes.selectTeacher')} ({t('common.optional')})
          </label>

          <DebouncedInput
            id="create-teacher-search"
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
                hasMore={!!hasNextPage}
                isFetchingNextPage={isFetchingNextPage}
                onLoadMore={() => fetchNextPage()}
                maxHeight="max-h-60"
              />
            )}
          />
        </div>
      </div>
    </BaseModal>
  )
}
