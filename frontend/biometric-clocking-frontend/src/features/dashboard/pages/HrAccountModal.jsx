import { useEffect, useState } from 'react'
import Modal from '../../../shared/components/Modal'
import { createHrAccount, getHrAccountStatus } from '../../../services/authServices'

export default function HrAccountModal({ onClose }) {
  const [status, setStatus] = useState(null)
  const [email, setEmail] = useState('')
  const [temporaryPassword, setTemporaryPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    getHrAccountStatus().then(setStatus).catch(() => setError('Unable to load the HR account status.'))
  }, [])

  const submit = async event => {
    event.preventDefault()
    if (temporaryPassword !== confirmPassword) {
      setError('The temporary passwords do not match.')
      return
    }
    setSaving(true)
    setError('')
    try {
      await createHrAccount({ email, temporaryPassword })
      setStatus({ configured: true, email })
      setTemporaryPassword('')
      setConfirmPassword('')
    } catch (requestError) {
      setError(requestError.response?.data?.message || 'Unable to create the HR account.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">HR account</h2>
      {status?.configured ? (
        <p className="mt-3 text-sm leading-6 text-slate-300">The one HR account is configured for <b>{status.email}</b>. The HR user can change their own password from the HR console.</p>
      ) : (
        <form onSubmit={submit}>
          <p className="mt-2 text-sm leading-6 text-slate-400">Create the one HR login. Employees do not receive passwords.</p>
          <label className="mt-5 block text-xs font-bold">HR EMAIL</label>
          <input type="email" value={email} onChange={event => setEmail(event.target.value)} required className="mt-2 w-full rounded-lg bg-[#09192c] px-3 py-3 text-sm outline-none" />
          <label className="mt-4 block text-xs font-bold">TEMPORARY PASSWORD</label>
          <input type="password" value={temporaryPassword} onChange={event => setTemporaryPassword(event.target.value)} minLength="12" required className="mt-2 w-full rounded-lg bg-[#09192c] px-3 py-3 text-sm outline-none" />
          <label className="mt-4 block text-xs font-bold">CONFIRM TEMPORARY PASSWORD</label>
          <input type="password" value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} minLength="12" required className="mt-2 w-full rounded-lg bg-[#09192c] px-3 py-3 text-sm outline-none" />
          <p className="mt-2 text-xs text-slate-400">The HR user must replace this with a private password at first sign-in.</p>
          {error && <p className="mt-4 text-sm font-semibold text-rose-300">{error}</p>}
          <button disabled={saving || status === null} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white disabled:opacity-40">{saving ? 'Creating...' : 'Create HR account'}</button>
        </form>
      )}
      {status?.configured && <button onClick={onClose} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white">Close</button>}
    </Modal>
  )
}
