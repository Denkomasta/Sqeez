import { useState, useRef, useEffect, useMemo } from 'react'
import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { Trophy, Medal, Wifi, Search, ChevronDown } from 'lucide-react'
import { SimpleAvatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge/Badge'
import { DebouncedInput } from '@/components/ui/Input/DebouncedInput'
import { getImageUrl } from '@/lib/imageHelpers'
import { formatName } from '@/lib/userHelpers'
import { useAuthStore } from '@/store/useAuthStore'
import { cn } from '@/lib/utils'
import { useGetApiUsers } from '@/api/generated/endpoints/user/user'
import { PaginatedListView } from '@/components/layouting/PaginatedListView/PaginatedListView'
import type { StudentDto, UserRole } from '@/api/generated/model'
import { ScrollableSelectList } from '@/components/ui/ScrollableSelectList/ScrollableSelectList'
import { useGetApiSubjectsInfinite } from '@/hooks/useGetApiSubjectsInfinite'
import { useGetApiClassesInfinite } from '@/hooks/useGetApiClassesInfinite'

export function LeaderboardPage() {
  const { t } = useTranslation()
  const user = useAuthStore((s) => s.user)

  const [searchQuery, setSearchQuery] = useState('')
  const [pageNumber, setPageNumber] = useState(1)
  const pageSize = 10

  const [role, setRole] = useState<UserRole | ''>('Student')
  const [schoolClassId, setSchoolClassId] = useState<string | number>('')
  const [subjectId, setSubjectId] = useState<string | number>('')
  const [isOnlineOnly, setIsOnlineOnly] = useState<boolean>(false)

  const [openDropdown, setOpenDropdown] = useState<
    'role' | 'class' | 'subject' | null
  >(null)

  const roleRef = useRef<HTMLDivElement>(null)
  const classRef = useRef<HTMLDivElement>(null)
  const subjectRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      const target = event.target as Node

      if (
        openDropdown === 'role' &&
        roleRef.current &&
        !roleRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      } else if (
        openDropdown === 'class' &&
        classRef.current &&
        !classRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      } else if (
        openDropdown === 'subject' &&
        subjectRef.current &&
        !subjectRef.current.contains(target)
      ) {
        setOpenDropdown(null)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [openDropdown])

  const {
    data: usersResponse,
    isLoading,
    isFetching,
  } = useGetApiUsers(
    {
      SearchTerm: searchQuery || undefined,
      PageNumber: pageNumber,
      PageSize: pageSize,
      SortBy: 'XP',
      IsDescending: true,
      Role: role !== '' ? role : undefined,
      StrictRoleOnly: role !== '',
      SchoolClassId: schoolClassId ? Number(schoolClassId) : undefined,
      SubjectId: subjectId ? Number(subjectId) : undefined,
      IsOnline: isOnlineOnly ? true : undefined,
    },
    {
      query: {
        placeholderData: (prev) => prev,
      },
    },
  )

  const students = usersResponse?.data || []
  const totalPages = Number(usersResponse?.totalPages || 1)
  const totalCount = Number(usersResponse?.totalCount || 0)
  const scrollableSelectListWrapperClassName =
    'absolute top-full left-0 z-50 mt-1 w-64 rounded-md border border-border bg-card shadow-lg'

  const {
    data: subjectsData,
    isLoading: isLoadingSubjects,
    fetchNextPage: fetchNextSubjects,
    hasNextPage: hasNextSubjects,
    isFetchingNextPage: isFetchingNextSubjects,
  } = useGetApiSubjectsInfinite({ PageSize: pageSize })

  const subjectOptions = useMemo(() => {
    const defaultOption = {
      id: '',
      title: t('leaderboard.allSubjects'),
    }

    if (!subjectsData) return [defaultOption]

    const fetchedOptions = subjectsData.pages
      .flatMap((page) => page.data || [])
      .map((subject) => ({
        id: subject.id,
        title: subject.name,
        subtitle: subject.code,
      }))

    return [defaultOption, ...fetchedOptions]
  }, [subjectsData, t])

  const {
    data: classesData,
    isLoading: isLoadingClasses,
    fetchNextPage: fetchNextClasses,
    hasNextPage: hasNextClasses,
    isFetchingNextPage: isFetchingNextClasses,
  } = useGetApiClassesInfinite({ PageSize: pageSize })

  const classOptions = useMemo(() => {
    const defaultOption = {
      id: '',
      title: t('leaderboard.allClasses'),
    }

    if (!classesData) return [defaultOption]

    const fetchedOptions = classesData.pages
      .flatMap((page) => page.data || [])
      .map((schoolClass) => ({
        id: schoolClass.id,
        title: schoolClass.name,
      }))

    return [defaultOption, ...fetchedOptions]
  }, [classesData, t])

  const getRankBadge = (globalRank: number) => {
    switch (globalRank) {
      case 1:
        return <Trophy className="h-6 w-6 text-nav-yellow drop-shadow-sm" />
      case 2:
        return (
          <Medal className="h-6 w-6 text-muted-foreground drop-shadow-sm" />
        )
      case 3:
        return <Medal className="h-6 w-6 text-nav-orange drop-shadow-sm" />
      default:
        return (
          <span className="text-lg font-bold text-muted-foreground">
            {globalRank}
          </span>
        )
    }
  }

  const roleOptions: { id: UserRole | ''; title: string }[] = [
    { id: '', title: t('leaderboard.allRoles') },
    { id: 'Student', title: t('common.student') },
    { id: 'Teacher', title: t('common.teacher') },
    { id: 'Admin', title: t('common.admin') },
  ]

  const unifiedToolbarNode = (
    <div className="flex w-full flex-col gap-4 rounded-xl border border-border bg-card p-3 shadow-sm md:flex-row md:items-center md:justify-between">
      <div className="w-full shrink-0 md:max-w-xs">
        <DebouncedInput
          id="unified-search"
          value={searchQuery}
          onChange={(val) => {
            setSearchQuery(val)
            setPageNumber(1)
          }}
          placeholder={t('leaderboard.searchPlaceholder')}
          icon={<Search className="h-4 w-4 text-muted-foreground" />}
          className="w-full bg-background"
          hideErrors
        />
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div ref={roleRef} className="relative">
          <button
            type="button"
            onClick={() =>
              setOpenDropdown(openDropdown === 'role' ? null : 'role')
            }
            className="flex h-9 items-center gap-2 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors hover:bg-muted focus:border-primary focus:outline-none"
          >
            {roleOptions.find((o) => o.id === role)?.title ||
              t('leaderboard.allRoles')}
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </button>
          {openDropdown === 'role' && (
            <div className={scrollableSelectListWrapperClassName}>
              <ScrollableSelectList
                options={roleOptions}
                selectedId={role}
                onSelect={(id) => {
                  setRole(id as UserRole | '')
                  setPageNumber(1)
                  setOpenDropdown(null)
                }}
                maxHeight="max-h-[200px]"
              />
            </div>
          )}
        </div>

        <div ref={classRef} className="relative">
          <button
            type="button"
            onClick={() =>
              setOpenDropdown(openDropdown === 'class' ? null : 'class')
            }
            className="flex h-9 items-center gap-2 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors hover:bg-muted focus:border-primary focus:outline-none"
          >
            {classOptions.find((o) => o.id === schoolClassId)?.title ||
              t('leaderboard.allClasses')}
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </button>
          {openDropdown === 'class' && (
            <div className={scrollableSelectListWrapperClassName}>
              <ScrollableSelectList
                options={classOptions}
                selectedId={schoolClassId}
                onSelect={(id) => {
                  setSchoolClassId(id)
                  setPageNumber(1)
                  setOpenDropdown(null)
                }}
                maxHeight="max-h-[240px]"
                isLoading={isLoadingClasses}
                hasMore={hasNextClasses}
                onLoadMore={() => fetchNextClasses()}
                isFetchingNextPage={isFetchingNextClasses}
                loadingText={t('common.loading')}
              />
            </div>
          )}
        </div>

        <div ref={subjectRef} className="relative">
          <button
            type="button"
            onClick={() =>
              setOpenDropdown(openDropdown === 'subject' ? null : 'subject')
            }
            className="flex h-9 items-center gap-2 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors hover:bg-muted focus:border-primary focus:outline-none"
          >
            {subjectOptions.find((o) => o.id === subjectId)?.title ||
              t('leaderboard.allSubjects')}
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </button>
          {openDropdown === 'subject' && (
            <div className={scrollableSelectListWrapperClassName}>
              <ScrollableSelectList
                options={subjectOptions}
                selectedId={subjectId}
                onSelect={(id) => {
                  setSubjectId(id)
                  setPageNumber(1)
                  setOpenDropdown(null)
                }}
                maxHeight="max-h-[240px]"
                isLoading={isLoadingSubjects}
                hasMore={hasNextSubjects}
                onLoadMore={() => fetchNextSubjects()}
                isFetchingNextPage={isFetchingNextSubjects}
                loadingText={t('common.loading')}
              />
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={() => {
            setIsOnlineOnly(!isOnlineOnly)
            setPageNumber(1)
          }}
          className={cn(
            'inline-flex h-9 items-center justify-center gap-2 rounded-md border px-3 text-xs font-medium shadow-sm transition-colors focus-visible:outline-none',
            isOnlineOnly
              ? 'border-transparent bg-success text-success-foreground hover:bg-success/90'
              : 'border-input bg-background hover:bg-accent hover:text-accent-foreground',
          )}
        >
          <Wifi
            className={cn(
              'h-3.5 w-3.5',
              isOnlineOnly
                ? 'text-success-foreground'
                : 'text-muted-foreground',
            )}
          />
          {t('common.online')}
        </button>
      </div>
    </div>
  )

  return (
    <PaginatedListView<StudentDto>
      layoutVariant="app"
      containerClassName="max-w-5xl"
      gridClassName="flex flex-col gap-3"
      filtersNode={unifiedToolbarNode}
      searchQuery={searchQuery}
      titleNode={
        <>
          <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <Trophy className="h-6 w-6" />
          </span>
          {t('leaderboard.title')}
        </>
      }
      subtitleNode={t('leaderboard.subtitle')}
      pageNumber={pageNumber}
      totalPages={totalPages}
      setPageNumber={setPageNumber}
      isLoading={isLoading && !usersResponse}
      isFetching={isFetching}
      items={students}
      totalCount={totalCount}
      emptyStateMessage={
        <div className="mx-auto flex max-w-4xl flex-col items-center justify-center rounded-2xl border-2 border-dashed border-muted p-12 text-center">
          <Trophy className="mb-4 h-12 w-12 text-muted-foreground/30" />
          <h3 className="text-lg font-semibold">
            {t('leaderboard.noStudentsFound')}
          </h3>
          <p className="mt-2 text-sm text-muted-foreground">
            {t('leaderboard.tryDifferentSearch')}
          </p>
        </div>
      }
      renderItem={(student, index) => {
        const globalRank = (pageNumber - 1) * pageSize + index + 1
        const isMe = student.id === user?.id

        return (
          <div
            key={student.id}
            className={cn(
              'flex items-center justify-between rounded-2xl border p-4 transition-all hover:shadow-md sm:px-6',
              isMe
                ? 'border-primary bg-card shadow-sm ring-1 ring-primary/30'
                : 'border-border bg-card',
            )}
          >
            <div className="flex items-center gap-4 sm:gap-6">
              <div className="flex w-8 shrink-0 items-center justify-center">
                {getRankBadge(globalRank)}
              </div>

              <Link
                to="/app/profile/$userId"
                params={{ userId: (student.id ?? 0).toString() }}
                className="shrink-0 transition-opacity hover:opacity-80"
              >
                <SimpleAvatar
                  url={getImageUrl(student.avatarUrl)}
                  firstName={student.firstName}
                  lastName={student.lastName}
                  wrapperClassName="size-12"
                />
              </Link>

              <div className="flex flex-col">
                <span className="flex items-center gap-2 font-bold text-foreground sm:text-lg">
                  <Link
                    to="/app/profile/$userId"
                    params={{ userId: (student.id ?? 0).toString() }}
                    className="hover:underline"
                  >
                    {student.username}
                  </Link>
                  {isMe && (
                    <Badge variant="default" className="h-5 px-1.5 text-[10px]">
                      {t('class.me')}
                    </Badge>
                  )}
                </span>
                <span className="text-xs text-muted-foreground sm:text-sm">
                  {formatName(student.firstName, student.lastName)}
                </span>
              </div>
            </div>

            <div className="ml-4 flex shrink-0 flex-col items-end">
              <span className="text-lg font-black text-primary sm:text-2xl">
                {student.currentXP || 0}
              </span>
              <span className="text-[10px] font-bold tracking-widest text-muted-foreground uppercase sm:text-xs">
                {t('common.xp')}
              </span>
            </div>
          </div>
        )
      }}
    />
  )
}
