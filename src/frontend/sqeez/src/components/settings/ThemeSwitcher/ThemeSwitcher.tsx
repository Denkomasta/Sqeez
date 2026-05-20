import { Check, Palette } from 'lucide-react'
import { useTheme } from '@/context/ThemeContext'
import { Button } from '@/components/ui'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/DropdownMenu'
import { ScrollArea } from '@/components/ui/ScrollArea'
import { DAISY_THEMES } from '@/constants/themes'

/**
 * Theme dropdown backed by the shared theme context.
 *
 * @param props.title - Accessible label and dropdown heading.
 */
export function ThemeSwitcher({ title = 'Themes' }: { title?: string }) {
  const { theme: currentTheme, setTheme } = useTheme()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label={title}>
          <Palette className="size-5" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel>{title}</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <ScrollArea className="max-h-72">
          {DAISY_THEMES.map((themeName) => (
            <DropdownMenuItem
              key={themeName}
              onClick={() => setTheme(themeName)}
              className="flex items-center justify-between capitalize"
            >
              {themeName}
              {currentTheme === themeName && (
                <Check className="size-4 opacity-50" />
              )}
            </DropdownMenuItem>
          ))}
        </ScrollArea>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
