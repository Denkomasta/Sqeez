import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import SqeezLogo from './SqeezLogo'

describe('SqeezLogo', () => {
  it('renders an accessible svg with configured size and background', () => {
    render(<SqeezLogo size={48} backgroundColor="#ffffff" className="logo" />)

    const logo = screen.getByLabelText('Sqeez Logo')
    expect(logo).toHaveAttribute('width', '48')
    expect(logo).toHaveAttribute('height', '48')
    expect(logo).toHaveClass('logo')
    expect(logo).toHaveStyle({ backgroundColor: '#ffffff' })
  })

  it('renders a transparent container with a masked Q cutout', () => {
    render(<SqeezLogo size={48} backgroundColor="transparent" />)

    const logo = screen.getByLabelText('Sqeez Logo')
    expect(logo).toHaveAttribute('style', 'background-color: transparent;')
    expect(
      logo.querySelector('path[fill="transparent"]'),
    ).not.toBeInTheDocument()
    expect(logo.querySelector('mask#q-cutout-mask')).toBeInTheDocument()
    expect(
      logo.querySelector('path[mask="url(#q-cutout-mask)"]'),
    ).toHaveAttribute('fill', 'url(#gradient_0)')
  })
})
