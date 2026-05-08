import { act, fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { DebouncedInput } from './DebouncedInput'

describe('DebouncedInput', () => {
  it('calls onChange after the debounce delay', () => {
    vi.useFakeTimers()
    const onChange = vi.fn()

    render(
      <DebouncedInput
        value="math"
        onChange={onChange}
        debounceTime={250}
        id="search"
        label="Search"
      />,
    )

    fireEvent.change(screen.getByLabelText('Search'), {
      target: { value: 'science' },
    })

    expect(onChange).not.toHaveBeenCalled()

    act(() => {
      vi.advanceTimersByTime(250)
    })

    expect(onChange).toHaveBeenCalledWith('science')
    vi.useRealTimers()
  })

  it('updates local value when the controlled value changes', () => {
    const { rerender } = render(
      <DebouncedInput value="first" onChange={vi.fn()} label="Name" />,
    )

    rerender(
      <DebouncedInput
        value="second"
        onChange={vi.fn()}
        id="name"
        label="Name"
      />,
    )

    expect(screen.getByLabelText('Name')).toHaveValue('second')
  })

  it('keeps in-progress typing when a stale controlled value arrives', () => {
    const { rerender } = render(
      <DebouncedInput
        id="name"
        value="first"
        onChange={vi.fn()}
        label="Name"
      />,
    )

    fireEvent.change(screen.getByLabelText('Name'), {
      target: { value: 'fresh local text' },
    })

    rerender(
      <DebouncedInput
        id="name"
        value="stale server text"
        onChange={vi.fn()}
        label="Name"
      />,
    )

    expect(screen.getByLabelText('Name')).toHaveValue('fresh local text')
  })

  it('flushes the current local value on blur', () => {
    const onChange = vi.fn()

    render(
      <DebouncedInput
        id="name"
        value="first"
        onChange={onChange}
        debounceTime={1000}
        label="Name"
      />,
    )

    const input = screen.getByLabelText('Name')
    fireEvent.change(input, { target: { value: 'saved on blur' } })
    fireEvent.blur(input)

    expect(onChange).toHaveBeenCalledWith('saved on blur')
  })
})
