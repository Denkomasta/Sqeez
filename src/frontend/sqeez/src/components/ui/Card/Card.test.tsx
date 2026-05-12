import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from './Card'
import { ErrorCard } from './ErrorCard'
import { FeatureCard } from './FeatureCard'
import { SubjectCard } from './SubjectCard'

describe('Card primitives', () => {
  it('renders the expected card regions', () => {
    render(
      <Card>
        <CardHeader>
          <CardTitle>Title</CardTitle>
          <CardDescription>Description</CardDescription>
          <CardAction>Action</CardAction>
        </CardHeader>
        <CardContent>Content</CardContent>
        <CardFooter>Footer</CardFooter>
      </Card>,
    )

    expect(screen.getByText('Title')).toHaveAttribute('data-slot', 'card-title')
    expect(screen.getByText('Description')).toHaveAttribute(
      'data-slot',
      'card-description',
    )
    expect(screen.getByText('Action')).toHaveAttribute(
      'data-slot',
      'card-action',
    )
    expect(screen.getByText('Content')).toHaveAttribute(
      'data-slot',
      'card-content',
    )
    expect(screen.getByText('Footer')).toHaveAttribute(
      'data-slot',
      'card-footer',
    )
  })
})

describe('FeatureCard', () => {
  it('renders icon, title and description', () => {
    render(
      <FeatureCard
        icon={<span data-testid="feature-icon" />}
        visual={<span data-testid="feature-visual" />}
        title="Practice"
        description="Sharpen skills"
      />,
    )

    expect(screen.getByTestId('feature-icon')).toBeInTheDocument()
    expect(screen.getByTestId('feature-visual')).toBeInTheDocument()
    expect(screen.getByText('Practice')).toBeInTheDocument()
    expect(screen.getByText('Sharpen skills')).toBeInTheDocument()
  })
})

describe('SubjectCard', () => {
  it('renders subject details and optional slots', () => {
    render(
      <SubjectCard
        title="Mathematics"
        code="MATH"
        url="/subjects/$subjectId"
        params={{ subjectId: '1' }}
        borderColorClass="border-primary"
        description="Algebra basics"
        topRightSlot={<span>Top</span>}
        metricsSlot={<span>12 quizzes</span>}
        actionsSlot={<button>Open actions</button>}
      />,
    )

    expect(screen.getByText('MATH')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Mathematics' })).toHaveAttribute(
      'href',
      '/subjects/$subjectId',
    )
    expect(screen.getByText('Algebra basics')).toBeInTheDocument()
    expect(screen.getByText('Top')).toBeInTheDocument()
    expect(screen.getByText('12 quizzes')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Open actions' }),
    ).toBeInTheDocument()
  })
})

describe('ErrorCard', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders fallback action text from translations', () => {
    render(<ErrorCard title="Missing" description="Could not load item" />)

    expect(screen.getByText('Missing')).toBeInTheDocument()
    expect(screen.getByText('Could not load item')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /common.goBack/ }),
    ).toBeInTheDocument()
  })

  it('runs the supplied action when clicked', () => {
    const onAction = vi.fn()

    render(
      <ErrorCard
        title="Missing"
        description="Could not load item"
        actionText="Retry"
        icon={<span data-testid="custom-icon" />}
        actionIcon={<span data-testid="custom-action-icon" />}
        onAction={onAction}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Retry/ }))

    expect(screen.getByTestId('custom-icon')).toBeInTheDocument()
    expect(screen.getByTestId('custom-action-icon')).toBeInTheDocument()
    expect(onAction).toHaveBeenCalled()
  })

  it('falls back to browser history when no action is supplied', () => {
    const backSpy = vi
      .spyOn(history, 'back')
      .mockImplementation(() => undefined)

    render(<ErrorCard title="Missing" description="Could not load item" />)

    fireEvent.click(screen.getByRole('button', { name: /common.goBack/ }))

    expect(backSpy).toHaveBeenCalled()
  })
})
