import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export interface ColumnDef<T> {
  header: ReactNode
  cell: (item: T) => ReactNode
  className?: string
}

interface DataTableProps<T> {
  data: T[]
  columns: ColumnDef<T>[]
  isLoading?: boolean
  emptyMessage?: string
  keyExtractor: (item: T) => string | number
}

/**
 * Lightweight generic table for admin/statistic pages.
 * Consumers own cell rendering and must provide a stable key for each row.
 *
 * @param props.data - Rows to render.
 * @param props.columns - Column definitions that own header and cell rendering.
 * @param props.keyExtractor - Provides a stable key for each row item.
 */
export function DataTable<T>({
  data,
  columns,
  isLoading,
  emptyMessage,
  keyExtractor,
}: DataTableProps<T>) {
  const { t } = useTranslation()
  const colSpan = columns.length

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-card shadow-sm">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-border bg-muted text-foreground shadow-sm">
            <tr>
              {columns.map((col, index) => (
                <th
                  key={index}
                  className={`p-4 font-medium ${col.className || ''}`}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {isLoading ? (
              <tr>
                <td
                  colSpan={colSpan}
                  className="p-8 text-center text-muted-foreground"
                >
                  <div className="flex justify-center">
                    <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                  </div>
                </td>
              </tr>
            ) : data.length === 0 ? (
              <tr>
                <td
                  colSpan={colSpan}
                  className="p-12 text-center text-muted-foreground"
                >
                  {emptyMessage || t('common.noDataFound')}
                </td>
              </tr>
            ) : (
              data.map((item) => (
                <tr
                  key={keyExtractor(item)}
                  className="transition-colors hover:bg-muted/50"
                >
                  {columns.map((col, index) => (
                    <td key={index} className={`p-4 ${col.className || ''}`}>
                      {col.cell(item)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
