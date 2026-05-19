import { createContext, useContext, useEffect, useState } from 'react'
import { type Theme } from '@/types/theme'
import { DAISY_DARK_THEMES, DAISY_THEMES } from '@/constants/themes'

export type ThemeState = Theme | 'system'

const ThemeContext = createContext<
  | {
      theme: ThemeState
      setTheme: (theme: ThemeState) => void
    }
  | undefined
>(undefined)

/**
 * Applies the selected DaisyUI theme to the document root.
 * The `system` setting follows `prefers-color-scheme` and updates when it changes.
 */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<ThemeState>(
    () => (localStorage.getItem('ui-theme') as ThemeState) || 'system',
  )

  useEffect(() => {
    const root = window.document.documentElement
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

    const applyTheme = () => {
      root.classList.remove(...DAISY_THEMES, 'dark')

      let activeDaisyTheme: Theme

      if (theme === 'system') {
        activeDaisyTheme = mediaQuery.matches ? 'dark' : 'light'
      } else {
        activeDaisyTheme = theme
      }

      root.setAttribute('data-theme', activeDaisyTheme)

      if (DAISY_DARK_THEMES.includes(activeDaisyTheme)) {
        root.classList.add('dark')
      }
    }

    applyTheme()
    localStorage.setItem('ui-theme', theme)

    if (theme === 'system') {
      const handleSystemThemeChange = () => applyTheme()
      mediaQuery.addEventListener('change', handleSystemThemeChange)
      return () =>
        mediaQuery.removeEventListener('change', handleSystemThemeChange)
    }
  }, [theme])

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

/** Accesses the current theme selection; must be rendered under ThemeProvider. */
export const useTheme = () => {
  const context = useContext(ThemeContext)
  if (!context) throw new Error('useTheme must be used within a ThemeProvider')
  return context
}
