import type { ReactNode } from 'react'
import { Link } from '@tanstack/react-router'
import { ArrowRight } from 'lucide-react'
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/Card'

export type DashboardNavCardItem = {
  title: string
  description: string
  icon: ReactNode
  href: string
  params?: Record<string, string | number>
  iconPanelClassName: string
  accentClassName: string
}

interface DashboardNavCardGridProps {
  items: DashboardNavCardItem[]
}

function DashboardNavCard({ item }: { item: DashboardNavCardItem }) {
  return (
    <Card className="group relative h-full cursor-pointer overflow-hidden transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/70 hover:shadow-lg">
      <div className={`absolute inset-x-0 top-0 h-1 ${item.accentClassName}`} />
      <CardHeader className="relative gap-4 px-5">
        <div className="flex items-start justify-between gap-3">
          <div
            className={`inline-flex rounded-xl p-3 shadow-sm transition-transform duration-200 group-hover:scale-105 ${item.iconPanelClassName}`}
          >
            {item.icon}
          </div>
          <span className="mt-1 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border bg-secondary text-muted-foreground transition-colors group-hover:border-primary/40 group-hover:bg-primary group-hover:text-primary-foreground">
            <ArrowRight
              className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5"
              aria-hidden="true"
            />
          </span>
        </div>
        <div className="space-y-2">
          <CardTitle className="text-xl">{item.title}</CardTitle>
          <CardDescription className="line-clamp-2 leading-relaxed">
            {item.description}
          </CardDescription>
        </div>
      </CardHeader>
    </Card>
  )
}

export function DashboardNavCardGrid({ items }: DashboardNavCardGridProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {items.map((item) => (
        <Link
          key={item.href}
          to={item.href}
          params={item.params}
          className="block rounded-xl ring-primary outline-none focus-visible:ring-2"
        >
          <DashboardNavCard item={item} />
        </Link>
      ))}
    </div>
  )
}
