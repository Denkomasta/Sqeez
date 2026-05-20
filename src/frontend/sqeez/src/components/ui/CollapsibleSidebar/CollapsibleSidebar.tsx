import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { cn } from '@/lib/utils'

interface CollapsibleSidebarProps {
  title?: React.ReactNode
  icon?: React.ReactNode
  actions?: React.ReactNode
  children: React.ReactNode
  defaultExpanded?: boolean
  expandedWidth?: string
  collapsedWidth?: string
  expandTooltip?: string
  collapseTooltip?: string
  className?: string
  contentClassName?: string
}

/**
 * Fixed-height sidebar that can collapse to an icon rail.
 * Children scroll inside the expanded content area instead of growing the page.
 *
 * @param props.defaultExpanded - Initial expanded state; later toggles stay local to the sidebar.
 * @param props.expandedWidth - Tailwind width class used while expanded.
 * @param props.collapsedWidth - Tailwind width class used while collapsed.
 * @param props.contentClassName - Extra classes for the scrollable content area.
 */
export function CollapsibleSidebar({
  title,
  icon,
  actions,
  children,
  defaultExpanded = true,
  expandedWidth = 'w-80',
  collapsedWidth = 'w-14',
  expandTooltip = 'Expand',
  collapseTooltip = 'Collapse',
  className,
  contentClassName,
}: CollapsibleSidebarProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded)

  if (!isExpanded) {
    return (
      <aside
        className={cn(
          'flex max-h-full min-h-0 shrink-0 flex-col items-center self-stretch overflow-hidden border-r bg-muted/5 py-4 transition-all duration-300',
          collapsedWidth,
          className,
        )}
      >
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-muted-foreground hover:text-foreground"
          onClick={() => setIsExpanded(true)}
          title={expandTooltip}
        >
          <ChevronRight className="h-5 w-5" />
        </Button>
      </aside>
    )
  }

  return (
    <aside
      className={cn(
        'flex max-h-full min-h-0 shrink-0 flex-col self-stretch overflow-hidden border-r bg-muted/5 transition-all duration-300',
        expandedWidth,
        className,
      )}
    >
      <div className="flex items-center justify-between border-b bg-background p-4 shadow-sm">
        <div className="flex items-center gap-2">
          {icon}
          {typeof title === 'string' ? (
            <h2 className="text-xs font-bold tracking-widest text-foreground uppercase">
              {title}
            </h2>
          ) : (
            title
          )}
        </div>

        <div className="flex items-center gap-1">
          {actions}
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0 text-muted-foreground hover:text-foreground"
            onClick={() => setIsExpanded(false)}
            title={collapseTooltip}
          >
            <ChevronLeft className="h-5 w-5" />
          </Button>
        </div>
      </div>

      <div
        className={cn(
          'min-h-0 flex-1 space-y-2 overflow-y-auto p-3',
          contentClassName,
        )}
      >
        {children}
      </div>
    </aside>
  )
}
