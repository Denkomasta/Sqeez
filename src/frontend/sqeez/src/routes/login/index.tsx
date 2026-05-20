import { createFileRoute } from '@tanstack/react-router'
import { LoginForm } from './-/LoginForm'
import { BrandingPanel } from '@/components/layouting/BrandingPanel'

export const Route = createFileRoute('/login/')({
  validateSearch: (search: Record<string, unknown>): { redirect?: string } => {
    return {
      redirect: search.redirect as string | undefined,
    }
  },
  component: Login,
})

/** Public login route with redirect support after successful authentication. */
function Login() {
  return (
    <>
      <div className="flex min-h-screen">
        <div className="hidden lg:flex lg:w-1/2">
          <BrandingPanel />
        </div>

        <div className="flex w-full flex-col items-center justify-center bg-background px-6 py-12 lg:w-1/2 lg:px-16">
          <LoginForm />
        </div>
      </div>
    </>
  )
}
