import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { Navbar } from './Navbar'

vi.mock('@/hooks/useResponsiveMaxVisibleTabs', () => ({
  useResponsiveMaxVisible: () => 4,
}))

vi.mock('@/components/settings/LanguageSwitcher/LanguageSwitcher', () => ({
  LanguageSwitcher: () => <span>Language switcher</span>,
}))

vi.mock('@/components/settings/ThemeSwitcher/ThemeSwitcher', () => ({
  ThemeSwitcher: ({ title }: { title?: string }) => <span>{title}</span>,
}))

describe('Navbar', () => {
  it('renders public navigation and auth actions', () => {
    render(
      <Navbar
        title="Sqeez"
        loginButtonText="Log in"
        registerButtonText="Register"
        navigationText="Navigation"
        isRegisterEnabled
        navLinks={[
          { to: '/', label: 'Home' },
          { to: '/about', label: 'About' },
        ]}
      />,
    )

    expect(screen.getByRole('link', { name: /Sqeez/ })).toHaveAttribute(
      'href',
      '/',
    )
    const logo = screen.getByLabelText('Sqeez Logo')
    expect(logo).toHaveAttribute('style', 'background-color: transparent;')
    expect(logo.querySelector('mask#q-cutout-mask')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Home' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'About' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login',
    )
    expect(screen.getByRole('link', { name: 'Register' })).toHaveAttribute(
      'href',
      '/register',
    )
  })

  it('opens the public mobile menu', async () => {
    const { container } = render(
      <Navbar
        title="Sqeez"
        loginButtonText="Log in"
        registerButtonText="Register"
        navigationText="Navigation"
        isRegisterEnabled
        navLinks={[
          { to: '/', label: 'Home' },
          { to: '/about', label: 'About' },
        ]}
      />,
    )

    fireEvent.click(container.querySelector('[data-slot="sheet-trigger"]')!)

    expect((await screen.findAllByText('Navigation')).length).toBeGreaterThan(0)
    expect(
      screen.getAllByRole('link', { name: 'Home' }).length,
    ).toBeGreaterThan(0)
    expect(
      screen.getAllByRole('link', { name: 'Log in' }).length,
    ).toBeGreaterThan(0)
    expect(
      screen.getAllByRole('link', { name: 'Register' }).length,
    ).toBeGreaterThan(0)
  })

  it('renders profile and logout actions for authenticated users', () => {
    render(
      <Navbar
        title="Sqeez"
        logoutButtonText="Log out"
        navigationText="Navigation"
        isAuthenticated
        user={{
          id: 1,
          username: 'dana',
          email: 'dana@example.com',
          avatarUrl: null,
          currentXP: '500',
          role: 'Student',
        }}
      />,
    )

    expect(screen.getByRole('link', { name: /DA/ })).toHaveAttribute(
      'href',
      '/app/profile',
    )
    expect(screen.getByRole('link', { name: 'Log out' })).toHaveAttribute(
      'href',
      '/logout',
    )
  })

  it('opens the authenticated mobile menu with profile and logout actions', async () => {
    const { container } = render(
      <Navbar
        title="Sqeez"
        logoutButtonText="Log out"
        navigationText="Navigation"
        isAuthenticated
        user={{
          id: 1,
          username: 'dana',
          email: 'dana@example.com',
          avatarUrl: null,
          currentXP: '500',
          role: 'Student',
        }}
      />,
    )

    fireEvent.click(container.querySelector('[data-slot="sheet-trigger"]')!)

    expect(await screen.findByText('common.viewProfile')).toBeInTheDocument()
    expect(
      screen.getAllByRole('link', { name: 'Log out' }).length,
    ).toBeGreaterThan(0)
  })
})
