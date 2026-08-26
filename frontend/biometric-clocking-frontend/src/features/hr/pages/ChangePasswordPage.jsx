import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { changeHrPassword } from '../../../services/authServices'

export default function ChangePasswordPage() {
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const submit = async event => {
    event.preventDefault()
    if (newPassword !== confirmPassword) {
      setError('The new passwords do not match.')
      return
    }

    setSaving(true)
    setError('')
    try {
      await changeHrPassword({ currentPassword, newPassword })
      navigate('/hr', { replace: true })
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Unable to change the password.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[#12304c] p-5 text-slate-200">
      <form onSubmit={submit} className="w-full max-w-md rounded-2xl bg-[#173a5d] p-7 shadow-2xl">
        <p className="text-xs font-bold tracking-widest text-sky-300">VERITY HR CONSOLE</p>
        <h1 className="mt-2 text-2xl font-bold text-white">Create your private password</h1>
        <p className="mt-2 text-sm leading-6 text-slate-300">Your temporary password can only be used to reach this page.</p>

        <label className="mt-6 block text-xs font-bold">TEMPORARY PASSWORD</label>
        <input type="password" value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} required className="mt-2 w-full rounded-lg bg-[#10233a] px-3 py-3 outline-none" />
        <label className="mt-4 block text-xs font-bold">NEW PASSWORD</label>
        <input type="password" value={newPassword} onChange={event => setNewPassword(event.target.value)} minLength="12" required className="mt-2 w-full rounded-lg bg-[#10233a] px-3 py-3 outline-none" />
        <label className="mt-4 block text-xs font-bold">CONFIRM NEW PASSWORD</label>
        <input type="password" value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} minLength="12" required className="mt-2 w-full rounded-lg bg-[#10233a] px-3 py-3 outline-none" />
        <p className="mt-2 text-xs text-slate-400">Use at least 12 characters.</p>
        {error && <p className="mt-4 text-sm font-semibold text-rose-300">{error}</p>}
        <button disabled={saving} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white disabled:opacity-40">{saving ? 'Saving...' : 'Save password'}</button>
      </form>
    </main>
  )
}
