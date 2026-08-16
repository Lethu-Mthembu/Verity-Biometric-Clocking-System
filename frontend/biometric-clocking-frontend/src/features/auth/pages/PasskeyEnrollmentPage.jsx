import { useState } from 'react'
import { clearAuthState, completePasskeyRegistration, getPasskeyRegistrationOptions } from '../../../services/authServices'
import { createPasskey, supportsPasskeys } from '../../../shared/lib/passkeys'

export default function PasskeyEnrollmentPage({ onComplete }) {
  const [working, setWorking] = useState(false)
  const [error, setError] = useState('')

  const enroll = async () => {
    if (!supportsPasskeys()) {
      setError('This browser does not support passkeys. Use a current Chrome, Edge, Safari, or Firefox browser on a device with screen lock enabled.')
      return
    }

    setWorking(true)
    setError('')
    try {
      const options = await getPasskeyRegistrationOptions()
      const credential = await createPasskey(options)
      const auth = await completePasskeyRegistration(credential)
      onComplete(auth)
    } catch (requestError) {
      clearAuthState()
      setError(requestError.response?.data?.message || requestError.message || 'Passkey setup was not completed.')
    } finally {
      setWorking(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[#071525] p-5 text-slate-100">
      <section className="w-full max-w-md rounded-2xl border border-sky-800 bg-[#10233a] p-7 shadow-2xl shadow-black/30">
        <p className="text-xs font-bold tracking-[0.2em] text-sky-300">VERITY SECURITY SETUP</p>
        <h1 className="mt-3 text-2xl font-bold text-white">Set up your passkey</h1>
        <p className="mt-4 text-sm leading-6 text-slate-300">Use Windows Hello, your phone, or your password manager’s passkey. This protects the Admin and HR consoles after password sign-in.</p>
        {error && <p className="mt-4 text-sm font-semibold text-rose-300">{error}</p>}
        <button onClick={enroll} disabled={working} className="mt-7 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white hover:bg-sky-500 disabled:opacity-40">
          {working ? 'Waiting for passkey...' : 'Create passkey'}
        </button>
      </section>
    </main>
  )
}
