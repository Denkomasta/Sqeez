import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, beforeEach } from 'vitest'
import { ThemeProvider } from '@/context/ThemeContext'
import { ThemeSwitcher } from './ThemeSwitcher'

describe('ThemeSwitcher', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-theme')
    document.documentElement.className = ''
    localStorage.clear()
  })

  it('opens a menu with theme choices', async () => {
    render(
      <ThemeProvider>
        <ThemeSwitcher title="Theme choices" />
      </ThemeProvider>,
    )

    const user = userEvent.setup()
    await user.click(screen.getByRole('button'))

    expect(await screen.findByText('Theme choices')).toBeInTheDocument()
    expect(screen.getByText('light')).toBeInTheDocument()
  })

  it('updates the HTML document when a new theme is selected', async () => {
    render(
      <ThemeProvider>
        <ThemeSwitcher title="Theme choices" />
      </ThemeProvider>,
    )

    const user = userEvent.setup()

    await user.click(screen.getByRole('button'))

    const darkOption = await screen.findByText('dark')
    await user.click(darkOption)

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('ui-theme')).toBe('dark')
  })
})
