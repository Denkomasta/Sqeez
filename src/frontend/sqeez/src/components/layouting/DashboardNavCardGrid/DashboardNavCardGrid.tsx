import type { ReactNode } from 'react'
import { Link } from '@tanstack/react-router'
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
}

interface DashboardNavCardGridProps {
  items: DashboardNavCardItem[]
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
          <Card className="group h-full cursor-pointer transition-all hover:border-primary hover:shadow-md">
            <CardHeader>
              <div
                className={`mb-4 inline-flex rounded-lg p-3 transition-transform group-hover:scale-110 ${item.iconPanelClassName}`}
              >
                {item.icon}
              </div>
              <CardTitle className="text-xl">{item.title}</CardTitle>
              <CardDescription className="line-clamp-2">
                {item.description}
              </CardDescription>
            </CardHeader>
          </Card>
        </Link>
      ))}
    </div>
  )
}
