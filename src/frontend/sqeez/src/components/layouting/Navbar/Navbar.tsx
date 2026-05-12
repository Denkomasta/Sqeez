import { useState } from 'react'
import { Link } from '@tanstack/react-router'
import { Menu } from 'lucide-react'
import { Button } from '@/components/ui'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/Sheet'
import { LanguageSwitcher } from '@/components/settings/LanguageSwitcher/LanguageSwitcher'
import { ThemeSwitcher } from '@/components/settings/ThemeSwitcher/ThemeSwitcher'
import SqeezLogo from '@/components/icons/logos/SqeezLogo'
import { SimpleAvatar } from '@/components/ui/Avatar'
import type { UserDTO } from '@/api/generated/model'
import { getImageUrl } from '@/lib/imageHelpers'
import { useTranslation } from 'react-i18next'
import { TabsWithMore } from '@/components/ui/Tabs'
import { useResponsiveMaxVisible } from '@/hooks/useResponsiveMaxVisibleTabs'

export interface LinkProps {
  to: string
  label: string
}

interface NavbarProps {
  navLinks?: LinkProps[]
  title?: string
  loginButtonText?: string
  registerButtonText?: string
  logoutButtonText?: string
  user?: UserDTO
  navigationText?: string
  isAuthenticated?: boolean
  isRegisterEnabled?: boolean
}

export function Navbar({
  navLinks = [],
  title,
  loginButtonText,
  registerButtonText,
  navigationText,
  logoutButtonText,
  isAuthenticated,
  user,
  isRegisterEnabled,
}: NavbarProps) {
  const { t } = useTranslation()
  const avatarUrl = getImageUrl(user?.avatarUrl)

  const dynamicMaxVisible = useResponsiveMaxVisible()

  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  const tabItems = navLinks.map((link) => ({
    id: link.to,
    label: link.label,
    to: link.to,
  }))

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-backdrop-filter:bg-background/60">
      <div className="flex h-16 items-center justify-between pr-4 pl-4">
        <div className="flex items-center gap-8">
          <Link
            to={isAuthenticated ? '/app' : '/'}
            className="flex items-center gap-2 text-xl font-bold"
          >
            <SqeezLogo size={64} backgroundColor="var(--background)" />
            <span>{title}</span>
          </Link>

          <div className="hidden md:block">
            <TabsWithMore
              tabs={tabItems}
              maxVisible={dynamicMaxVisible}
              className="gap-2 border-transparent"
            />
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="hidden gap-2 sm:flex">
            {isAuthenticated ? (
              <>
                <Link
                  to="/app/profile"
                  className="text-lg font-semibold text-muted-foreground transition-opacity hover:opacity-80"
                >
                  <SimpleAvatar
                    wrapperClassName="size-9 border-2"
                    url={avatarUrl}
                    username={user?.username}
                  />
                </Link>
                <Button variant="ghost" size="sm" asChild>
                  <Link to="/logout">{logoutButtonText}</Link>
                </Button>
              </>
            ) : (
              <>
                <Button variant="ghost" size="sm" asChild>
                  <Link to="/login">{loginButtonText}</Link>
                </Button>
                {isRegisterEnabled && (
                  <Button size="sm" asChild>
                    <Link to="/register">{registerButtonText}</Link>
                  </Button>
                )}
              </>
            )}
            <LanguageSwitcher />
            <ThemeSwitcher title={t('common.themes')} />
          </div>

          <Sheet open={isMobileMenuOpen} onOpenChange={setIsMobileMenuOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="md:hidden">
                <Menu className="h-5 w-5" />
              </Button>
            </SheetTrigger>
            <SheetContent
              side="left"
              className="h-svh max-h-svh w-[85vw] max-w-87.5 gap-0 overflow-hidden p-0"
            >
              <div className="grid h-full min-h-0 grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden">
                <SheetHeader className="sticky top-0 z-10 shrink-0 border-b bg-background p-6 text-left">
                  <SheetTitle>{navigationText}</SheetTitle>
                  <SheetDescription className="sr-only">
                    {navigationText}
                  </SheetDescription>
                </SheetHeader>

                <div className="min-h-0 overflow-y-auto overscroll-contain p-4">
                  <nav className="flex flex-col gap-1">
                    {navLinks.map((link) => {
                      const isExact = link.to === '/app' || link.to === '/'

                      return (
                        <Link
                          key={link.to}
                          to={link.to}
                          onClick={() => setIsMobileMenuOpen(false)}
                          className="rounded-lg px-4 py-3 text-base font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
                          activeProps={{
                            className:
                              'bg-primary/10 text-primary hover:bg-primary/15 hover:text-primary',
                          }}
                          activeOptions={{ exact: isExact }}
                        >
                          {link.label}
                        </Link>
                      )
                    })}
                  </nav>
                </div>

                <div className="sticky bottom-0 z-10 shrink-0 border-t bg-muted/20 p-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))]">
                  {isAuthenticated ? (
                    <div className="flex flex-col gap-2">
                      <Link
                        to="/app/profile"
                        onClick={() => setIsMobileMenuOpen(false)}
                        className="flex items-center gap-3 rounded-xl border bg-background px-3 py-2 shadow-sm transition-colors hover:bg-accent"
                      >
                        <SimpleAvatar
                          url={avatarUrl}
                          username={user?.username}
                          wrapperClassName="size-9 shrink-0"
                          imageClassName="object-cover"
                        />
                        <div className="flex flex-col overflow-hidden">
                          <span className="truncate text-sm font-semibold text-foreground">
                            {user?.username || t('common.user')}
                          </span>
                          <span className="truncate text-xs text-muted-foreground">
                            {t('common.viewProfile')}
                          </span>
                        </div>
                      </Link>

                      <div className="flex h-11 items-center justify-between rounded-xl border bg-background px-4 shadow-sm">
                        <LanguageSwitcher />
                        <div className="h-6 w-px bg-border"></div>{' '}
                        <ThemeSwitcher title={t('common.themes')} />
                      </div>

                      <Button
                        variant="destructive"
                        size="sm"
                        className="h-10 w-full"
                        asChild
                      >
                        <Link
                          to="/logout"
                          onClick={() => setIsMobileMenuOpen(false)}
                        >
                          {logoutButtonText}
                        </Link>
                      </Button>
                    </div>
                  ) : (
                    <div className="flex flex-col gap-3">
                      <div className="flex items-center justify-between rounded-xl border bg-background px-4 py-2 shadow-sm">
                        <LanguageSwitcher />
                        <div className="h-6 w-px bg-border"></div>
                        <ThemeSwitcher title={t('common.themes')} />
                      </div>
                      <Button className="w-full" asChild>
                        <Link
                          to="/login"
                          onClick={() => setIsMobileMenuOpen(false)}
                        >
                          {loginButtonText}
                        </Link>
                      </Button>
                      {isRegisterEnabled && (
                        <Button variant="outline" className="w-full" asChild>
                          <Link
                            to="/register"
                            onClick={() => setIsMobileMenuOpen(false)}
                          >
                            {registerButtonText}
                          </Link>
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </div>
    </header>
  )
}
