import { act, fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { PaginatedListView } from './PaginatedListView'

describe('PaginatedListView', () => {
  it('renders items, total count and pagination', () => {
    const setPageNumber = vi.fn()

    render(
      <PaginatedListView
        titleNode="Subjects"
        items={['Math', 'Science']}
        renderItem={(item) => <article key={item}>{item}</article>}
        totalCount={2}
        emptyStateMessage="No subjects"
        isLoading={false}
        pageNumber={1}
        totalPages={2}
        setPageNumber={setPageNumber}
      />,
    )

    expect(screen.getByText('Math')).toBeInTheDocument()
    expect(screen.getByText('Science')).toBeInTheDocument()
    expect(screen.getByText('Total: 2')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /common.next/ }))

    expect(setPageNumber).toHaveBeenCalledWith(2)
  })

  it('resets to page one when search changes', () => {
    vi.useFakeTimers()
    const setSearchQuery = vi.fn()
    const setPageNumber = vi.fn()

    render(
      <PaginatedListView
        titleNode="Subjects"
        items={[]}
        renderItem={(item) => (
          <article key={String(item)}>{String(item)}</article>
        )}
        emptyStateMessage="No subjects"
        isLoading={false}
        searchQuery=""
        setSearchQuery={setSearchQuery}
        setPageNumber={setPageNumber}
      />,
    )

    fireEvent.change(screen.getByPlaceholderText('common.search'), {
      target: { value: 'math' },
    })
    act(() => {
      vi.advanceTimersByTime(300)
    })

    expect(setSearchQuery).toHaveBeenCalledWith('math')
    expect(setPageNumber).toHaveBeenCalledWith(1)
    vi.useRealTimers()
  })
})
