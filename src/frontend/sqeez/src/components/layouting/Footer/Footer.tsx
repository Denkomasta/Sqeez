import { Link } from '@tanstack/react-router'
import type { LinkProps } from '../Navbar/Navbar'

interface FooterProps {
  links?: LinkProps[]
  rightsText?: string
}

/**
 * Footer for authenticated and public layouts.
 *
 * @param props.links - Optional router links shown above the rights text.
 * @param props.rightsText - Localized copyright or ownership text.
 */
export function Footer({ links, rightsText }: FooterProps) {
  return (
    <footer className="w-full border-t bg-background">
      <div className="flex flex-col items-center justify-center gap-4 py-4 text-sm text-muted-foreground">
        <div className="flex flex-row gap-4">
          {links?.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className="transition-colors hover:text-foreground"
            >
              {link.label}
            </Link>
          ))}
        </div>

        <p className="text-center text-sm leading-loose text-muted-foreground md:text-left">
          {rightsText}
        </p>
      </div>
    </footer>
  )
}
