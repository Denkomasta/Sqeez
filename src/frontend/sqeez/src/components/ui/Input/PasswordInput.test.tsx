import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { PasswordInput } from './PasswordInput'

describe('PasswordInput', () => {
  it('toggles password visibility', () => {
    render(<PasswordInput label="Password" id="password" />)

    const input = screen.getByLabelText('Password')
    expect(input).toHaveAttribute('type', 'password')

    fireEvent.click(screen.getByRole('button', { name: 'login.showPassword' }))
    expect(input).toHaveAttribute('type', 'text')

    fireEvent.click(screen.getByRole('button', { name: 'login.hidePassword' }))
    expect(input).toHaveAttribute('type', 'password')
  })

  it('disables the visibility button when the input is disabled', () => {
    render(<PasswordInput label="Password" id="password" disabled />)

    expect(
      screen.getByRole('button', { name: 'login.showPassword' }),
    ).toBeDisabled()
  })
})
