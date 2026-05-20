import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/DropdownMenu'
import { Button } from '@/components/ui'
import { Globe } from 'lucide-react'

interface LanguageItem {
  code: string
  label: string
  flag: string
}

/**
 * Loads available locales from the public locale manifest and switches i18next language.
 */
export function LanguageSwitcher() {
  const { i18n } = useTranslation()
  const [languages, setLanguages] = useState<LanguageItem[]>([])

  useEffect(() => {
    fetch('/locales/languages.json')
      .then((res) => res.json())
      .then((data) => setLanguages(data))
      .catch((err) => console.error('Could not load languages', err))
  }, [])

  const currentLang = languages.find((l) => l.code === i18n.resolvedLanguage)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          {currentLang ? (
            <>
              <span>{currentLang.flag}</span>
              <span className="hidden sm:inline">{currentLang.label}</span>
            </>
          ) : (
            <Globe className="h-4 w-4" />
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {languages.map((lang) => (
          <DropdownMenuItem
            key={lang.code}
            onClick={() => i18n.changeLanguage(lang.code)}
            className={i18n.resolvedLanguage === lang.code ? 'bg-accent' : ''}
          >
            <span className="mr-2">{lang.flag}</span>
            {lang.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
