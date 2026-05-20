import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FileUp, Info, Table as TableIcon } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/Card'
import { PageLayout } from '@/components/layouting/PageLayout/PageLayout'
import { ImportCsvModal } from './ImportCsvModal'
import { toast } from 'sonner'

const csvTemplate = `Class Name,Academic Year,Subject Name,Subject Code,First Name,Last Name,Email,Password
9.A,2025/2026,Mathematics,MATH-9A,Jan,Novák,jan.novak@example.com,Password123!`

const csvTemplateHeaders = [
  'Class Name',
  'Academic Year',
  'Subject Name',
  'Subject Code',
  'First Name',
  'Last Name',
  'Email',
  'Password',
]

/**
 * Admin import page for master CSV uploads.
 * Keeps the import modal separate from the explanatory page content.
 */
export function AdminImportPage() {
  const { t } = useTranslation()
  const [isModalOpen, setIsModalOpen] = useState(false)

  return (
    <>
      <PageLayout
        variant="app"
        containerClassName="max-w-5xl"
        title={t('admin.import.title')}
        subtitle={t('admin.import.subtitle')}
        headerActions={
          <Button onClick={() => setIsModalOpen(true)} className="gap-2">
            <FileUp className="h-4 w-4" />
            {t('admin.import.startBtn')}
          </Button>
        }
      >
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Info className="h-5 w-5 text-info" />
                {t('admin.import.howItWorks')}
              </CardTitle>
              <CardDescription>{t('admin.import.description')}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-3">
              <div className="rounded-lg border p-4">
                <h3 className="mb-2 flex items-center gap-2 font-bold">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                    1
                  </span>
                  {t('admin.import.steps.classes.title')}
                </h3>
                <p className="text-xs text-muted-foreground">
                  {t('admin.import.steps.classes.desc')}
                </p>
              </div>

              <div className="rounded-lg border p-4">
                <h3 className="mb-2 flex items-center gap-2 font-bold">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                    2
                  </span>
                  {t('admin.import.steps.subjects.title')}
                </h3>
                <p className="text-xs text-muted-foreground">
                  {t('admin.import.steps.subjects.desc')}
                </p>
              </div>

              <div className="rounded-lg border p-4">
                <h3 className="mb-2 flex items-center gap-2 font-bold">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-xs text-primary-foreground">
                    3
                  </span>
                  {t('admin.import.steps.students.title')}
                </h3>
                <p className="text-xs text-muted-foreground">
                  {t('admin.import.steps.students.desc')}
                </p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <TableIcon className="h-5 w-5 text-primary" />
                {t('admin.import.csvFormat')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted">
                    <tr>
                      {csvTemplateHeaders.map((header) => (
                        <th key={header} className="border-b p-3 font-semibold">
                          {header}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    <tr className="text-muted-foreground">
                      <td className="border-b p-3 italic">9.A</td>
                      <td className="border-b p-3 italic">2025/2026</td>
                      <td className="border-b p-3 italic">Mathematics</td>
                      <td className="border-b p-3 italic">MATH-9A</td>
                      <td className="border-b p-3 italic">Jan</td>
                      <td className="border-b p-3 italic">Novak</td>
                      <td className="border-b p-3 italic">
                        jan.novak@example.com
                      </td>
                      <td className="border-b p-3 italic">Password123!</td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <p className="mt-4 text-xs text-muted-foreground">
                * {t('admin.import.passwordNote')}
              </p>

              <div className="mt-6 space-y-3">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium">
                    {t('admin.import.csvRawExample')}
                  </p>

                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      navigator.clipboard.writeText(csvTemplate)
                      toast.success(t('common.copied'))
                    }}
                  >
                    {t('admin.import.copyTemplate')}
                  </Button>
                </div>

                <pre className="overflow-x-auto rounded-md bg-muted p-4 text-xs">
                  <code>{csvTemplate}</code>
                </pre>
              </div>
            </CardContent>
          </Card>
        </div>
      </PageLayout>

      <ImportCsvModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
      />
    </>
  )
}
