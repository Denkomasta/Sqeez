import '@testing-library/jest-dom'
import React from 'react'
import { afterEach, vi } from 'vitest'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (
      key: string,
      optionsOrDefault?: string | { defaultValue?: string; count?: number },
    ) => {
      if (typeof optionsOrDefault === 'string') return optionsOrDefault
      return optionsOrDefault?.defaultValue ?? key
    },
    i18n: {
      resolvedLanguage: 'en',
      changeLanguage: vi.fn(),
    },
  }),
}))

vi.mock('@tanstack/react-router', () => ({
  Link: ({
    to,
    children,
    ...props
  }: React.PropsWithChildren<
    React.AnchorHTMLAttributes<HTMLAnchorElement> & {
      to?: string
      activeProps?: unknown
      activeOptions?: unknown
      params?: unknown
      search?: unknown
    }
  >) => {
    const anchorProps = { ...props }
    delete anchorProps.activeProps
    delete anchorProps.activeOptions
    delete anchorProps.params
    delete anchorProps.search

    return React.createElement(
      'a',
      { href: to ?? '#', ...anchorProps },
      children,
    )
  },
}))

vi.mock('next-themes', () => ({
  useTheme: () => ({ theme: 'light' }),
}))

class ResizeObserverMock {
  observe = vi.fn()
  unobserve = vi.fn()
  disconnect = vi.fn()
}

vi.stubGlobal('ResizeObserver', ResizeObserverMock)

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false, // Defaulting to light mode in tests
    media: query,
    onchange: null,
    addListener: vi.fn(), // Deprecated but often required by UI libs
    removeListener: vi.fn(), // Deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

if (!window.PointerEvent) {
  vi.stubGlobal('PointerEvent', MouseEvent)
}

if (!window.HTMLElement.prototype.scrollIntoView) {
  window.HTMLElement.prototype.scrollIntoView = vi.fn()
}

if (!window.HTMLElement.prototype.hasPointerCapture) {
  window.HTMLElement.prototype.hasPointerCapture = vi.fn()
}

if (!window.HTMLElement.prototype.releasePointerCapture) {
  window.HTMLElement.prototype.releasePointerCapture = vi.fn()
}

afterEach(() => {
  vi.useRealTimers()
})
