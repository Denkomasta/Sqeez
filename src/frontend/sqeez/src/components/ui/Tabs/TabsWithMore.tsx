import * as React from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { Link } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'

import { Button } from '@/components/ui/Button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/DropdownMenu'

interface TabItem {
  id: string
  label: string
  to?: string
  onClick?: () => void
  isActive?: boolean
}

interface TabsWithMoreProps {
  tabs: TabItem[]
  maxVisible?: number
  className?: string
}

/**
 * Responsive tab list that moves overflow items into a dropdown.
 *
 * @param props.maxVisible - Number of tabs shown before overflow starts.
 */
export function TabsWithMore({
  tabs,
  maxVisible = 4,
  className = '',
}: TabsWithMoreProps) {
  const { t } = useTranslation()

  const [isOpen, setIsOpen] = React.useState(false)

  const visibleTabs = tabs.slice(0, maxVisible)
  const hiddenTabs = tabs.slice(maxVisible)

  const renderTab = (tab: TabItem, isDropdownItem = false) => {
    const isExact = tab.to === '/app' || tab.to === '/'

    if (tab.to) {
      return (
        <Link
          key={tab.id}
          to={tab.to}
          onClick={tab.onClick}
          className={
            isDropdownItem
              ? 'w-full cursor-pointer'
              : 'relative px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:text-primary'
          }
          activeProps={{
            className: isDropdownItem
              ? 'text-primary bg-accent'
              : 'text-primary after:absolute after:bottom-0 after:left-0 after:right-0 after:h-0.5 after:bg-primary',
          }}
          activeOptions={{ exact: isExact }}
        >
          {tab.label}
        </Link>
      )
    }

    return (
      <button
        key={tab.id}
        onClick={tab.onClick}
        className={
          isDropdownItem
            ? 'w-full cursor-pointer px-2 py-1.5 text-left text-sm'
            : `relative px-4 py-2 text-sm font-medium transition-colors hover:text-primary ${
                tab.isActive
                  ? 'text-primary after:absolute after:right-0 after:bottom-0 after:left-0 after:h-0.5 after:bg-primary'
                  : 'text-muted-foreground'
              }`
        }
      >
        {tab.label}
      </button>
    )
  }

  return (
    <div
      className={`flex items-center gap-1 border-b border-border ${className}`}
    >
      {visibleTabs.map((tab) => renderTab(tab))}

      {hiddenTabs.length > 0 && (
        <DropdownMenu onOpenChange={setIsOpen}>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className="gap-1 px-4 py-2 text-muted-foreground hover:text-primary"
            >
              <span className="text-sm font-medium">{t('common.more')}</span>
              {isOpen ? (
                <ChevronUp className="h-4 w-4" />
              ) : (
                <ChevronDown className="h-4 w-4" />
              )}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {hiddenTabs.map((tab) => {
              if (tab.to) {
                return (
                  <DropdownMenuItem key={tab.id} asChild>
                    {renderTab(tab, true)}
                  </DropdownMenuItem>
                )
              }

              return (
                <DropdownMenuItem key={tab.id} onClick={tab.onClick}>
                  {tab.label}
                </DropdownMenuItem>
              )
            })}
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  )
}
