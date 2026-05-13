import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { BrandingPanel } from './BrandingPanel'

describe('BrandingPanel', () => {
  it('renders translated branding copy and achievement stats', () => {
    render(<BrandingPanel />)

    expect(screen.getByText('system.name')).toBeInTheDocument()
    expect(screen.getByText('brandingPanel.title')).toBeInTheDocument()
    expect(
      screen.getByText('brandingPanel.learningLoop.title'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('brandingPanel.learningLoop.quiz'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('brandingPanel.stats.badges.value'),
    ).toBeInTheDocument()
    expect(screen.getByText('brandingPanel.stats.xp.label')).toBeInTheDocument()
    expect(
      screen.getByText('brandingPanel.badges.speedDemon'),
    ).toBeInTheDocument()
    expect(
      screen.getByText('brandingPanel.badges.quizExplorer'),
    ).toBeInTheDocument()
  })
})
