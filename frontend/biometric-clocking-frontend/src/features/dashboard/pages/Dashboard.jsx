import { useEffect, useMemo, useState } from "react"
import Modal from '../../../shared/components/Modal'
import Avatar from '../../../shared/components/Avatar'
import Panel from '../../../shared/components/Panel'
import API, { getApiUrl } from '../../../services/authServices'

const statusClass = {
  'Clocked In': 'text-[#22ff3c]',
  'Clocked Out': 'text-[ #ff2d2d]',
  'Late': 'text-[#ffe600]',
  'Absent': 'text-[#ff2d2d]'
}

const LOCALHOST_PASSKEY_ID = 'verity-localhost-passkey-id'

const currentLocation = sessionStorage.getItem("currentLocation") || "Location unavailable";

function bufferToBase64Url(buffer) {
  const bytes = new Uint8Array(buffer)
  let binary = ''
  bytes.forEach(byte => {
    binary += String.fromCharCode(byte)
  })
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

function base64UrlToBuffer(value) {
  const base64 = value.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=')
  const binary = atob(padded)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i)
  }
  return bytes.buffer
}

async function createLocalhostPasskey() {
  const credential = await navigator.credentials.create({
    publicKey: {
      challenge: crypto.getRandomValues(new Uint8Array(32)),
      rp: {
        name: 'Verity Attendance',
        id: window.location.hostname
      },
      user: {
        id: crypto.getRandomValues(new Uint8Array(16)),
        name: 'authenticated-admin@verity.local',
        displayName: 'Verity Admin'
      },
      pubKeyCredParams: [
        { type: 'public-key', alg: -7 },
        { type: 'public-key', alg: -257 }
      ],
      authenticatorSelection: {
        residentKey: 'preferred',
        userVerification: 'required'
      },
      timeout: 60000,
      attestation: 'none'
    }
  })

  localStorage.setItem(LOCALHOST_PASSKEY_ID, bufferToBase64Url(credential.rawId))
}

async function scanFingerprint() {
  if (!window.PublicKeyCredential || !navigator.credentials) {
    throw new Error('Windows Hello is not available in this browser.')
  }

  if (!localStorage.getItem(LOCALHOST_PASSKEY_ID)) {
    await createLocalhostPasskey()
  }

  await navigator.credentials.get({
    publicKey: {
      challenge: crypto.getRandomValues(new Uint8Array(32)),
      rpId: window.location.hostname,
      allowCredentials: [{
        id: base64UrlToBuffer(localStorage.getItem(LOCALHOST_PASSKEY_ID)),
        type: 'public-key'
      }],
      timeout: 60000,
      userVerification: 'required'
    }
  })
}

function DeleteModal({ employee, onClose, onRemove }) {
  const [verifying, setVerifying] = useState(false)
  const [error, setError] = useState('')

  const handleRemove = async () => {
    setVerifying(true)
    setError('')

    try {
      await scanFingerprint()
      await onRemove(employee.id)
      onClose()
    } catch (err) {
      setError(err.message || 'Fingerprint scan was not completed.')
      setVerifying(false)
    }
  }

  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">Remove employee?</h2>
      <p className="mt-2 text-sm leading-6 text-slate-400">This will permanently remove <b>{employee.name}</b> from the directory and revoke their terminal access.</p>
      <p className="mt-4 text-xs leading-5 text-slate-400">Use the localhost Windows Hello passkey to confirm removal.</p>
      {error && <p className="mt-3 text-xs font-bold text-rose-300">{error}</p>}
      <div className="mt-6 flex gap-3">
        <button onClick={onClose} className="flex-1 rounded-lg py-3 text-sm font-bold">Cancel</button>
        <button onClick={handleRemove} disabled={verifying} className="flex-1 rounded-lg bg-rose-600 py-3 text-sm font-bold text-white disabled:opacity-40">{verifying ? 'Scanning fingerprint...' : 'Remove employee'}</button>
      </div>
    </Modal>
  )
}

function AdminRequestModal({ request, onApprove, onClose }) {
  const [verifying, setVerifying] = useState(false)
  const [error, setError] = useState('')
  const isClockOut = request.requestedClockType === 'ClockOut'
  const action = isClockOut ? 'clock out' : 'clock in'

  const handleApprove = async () => {
    setVerifying(true)
    setError('')

    try {
      await scanFingerprint()
      await onApprove(request.employeeNumber, request.overrideRequestId, request.requestedClockType)
    } catch (err) {
      setError(err.message || 'Fingerprint scan was not completed.')
      setVerifying(false)
    }
  }

  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">Attendance request</h2>
      <p className="mt-4 text-sm leading-6 text-slate-400">Employee number {request.employeeNumber} is trying to {action}.</p>
      <p className="mt-4 text-xs leading-5 text-slate-400">Use the localhost Windows Hello passkey to confirm.</p>
      {error && <p className="mt-3 text-xs font-bold text-rose-300">{error}</p>}
      <button onClick={handleApprove} disabled={verifying} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white disabled:opacity-40">{verifying ? 'Scanning fingerprint...' : `Clock them ${isClockOut ? 'out' : 'in'}`}</button>
    </Modal>
  )
}

function ProfileModal({ employee, onClose, onEdit }) {
  const [firstName, ...rest] = employee.name.split(' ')
  const lastName = rest.join(' ')

  const profileRows = [
    ['First name', firstName],
    ['Last name', lastName],
    ['Employee ID', employee.id],
    ['Department', employee.dept],
    ['Attendance status', employee.status],
    ['Last timestamp', employee.time]
  ]

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#06101de6] p-5 backdrop-blur-sm">
      <section className="relative w-full max-w-md rounded-3xl bg-[#10233a] p-7 shadow-2xl shadow-black/50">
        <button onClick={onClose} aria-label="Close" className="absolute right-4 top-4 rounded-lg px-2 py-1 text-m font-bold text-slate-400 hover:bg-slate-700 hover:text-white">x</button>
        <div className="flex items-center gap-3">
          <Avatar {...employee} />
          <div>
            <h2 className="text-xl font-bold text-white">{employee.name}</h2>
            <p className="text-xs font-bold text-slate-400">{employee.id}</p>
          </div>
        </div>

        <div className="mt-6 grid grid-cols-2 gap-3">
          {profileRows.map(([label, value]) => (
            <div key={label} className="p-3">
              <span className="block text-[14px] font-bold tracking-widest text-slate-400">{label}</span>
              <b className="mt-1 block text-m text-white">{value}</b>
            </div>
          ))}
        </div>

        <button onClick={() => onEdit(employee)} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white hover:bg-sky-500">Edit profile</button>
      </section>
    </div>
  )
}

export default function Dashboard({ employees, pendingAdminRequest, onAdminRequest, onClearAdminRequest, onEmployeesChange, onEditEmployee, onLogout, onOnboard }) {
  const [query, setQuery] = useState('');
  const [action, setAction] = useState(null);
  const rows = useMemo(() => employees.filter(e => `${e.name} ${e.id}`.toLowerCase().includes(query.toLowerCase())), [employees, query]);
  useEffect(() => {
    let stream
    const loadPendingRequests = async () => {
      try {
        const { data } = await API.get('/admin/override-requests')
        if (data.length) onAdminRequest(data[0])
      } catch (error) {
        console.error('Could not load pending admin requests:', error)
      }
    }

    const token = localStorage.getItem('token')
    if (token) {
      loadPendingRequests()
      stream = new EventSource(`${getApiUrl('/admin/stream')}?access_token=${encodeURIComponent(token)}`)
      stream.addEventListener('override-request', event => onAdminRequest(JSON.parse(event.data)))
    }

    return () => stream?.close()
  }, [onAdminRequest])

  const approveAdminRequest = async (employeeNumber, overrideRequestId, requestedClockType) => {
    if (!Number.isInteger(overrideRequestId)) throw new Error('The override request is missing its ID.')
    await API.post(`/admin/override-requests/${overrideRequestId}/resolve`)

    onEmployeesChange(currentEmployees => currentEmployees.map(employee => {
      if (employee.id.toLowerCase() !== employeeNumber.toLowerCase()) return employee

      return {
        ...employee,
        status: requestedClockType === 'ClockOut' ? 'Clocked Out' : 'Clocked In',
        time: new Date().toLocaleTimeString()
      }
    }))
    onClearAdminRequest()

  }

  const removeEmployee = async employeeId => {
    await API.delete(`/Employee/${encodeURIComponent(employeeId)}`)
    onEmployeesChange(currentEmployees => currentEmployees.filter(employee => employee.id !== employeeId))
  }

  return (
    <main className="flex min-h-screen bg-[#12304c] text-slate-200">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-[#284968] bg-[#10233a] p-5 md:flex">
        <div className="flex items-center gap-3">
          <div>
            <b className="text-xl tracking-widest text-white">VERITY</b>
            <span className="block text-[9px] tracking-widest text-slate-400">ADMIN CONSOLE</span>
          </div>
        </div>
        <nav className="mt-10 grid gap-1 text-sm font-semibold">
          <a className="rounded-lg bg-[#245a84] px-3 py-3 text-sky-100">Overview</a>
          
        </nav>
        <button onClick={onLogout} className="mt-auto px-3 py-2 text-xs font-bold text-slate-300 hover:text-white">Log out</button>
      </aside>

      <div className="min-w-0 flex-1 p-5 md:p-9">
        <header className="mb-7 flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-white">
              Good morning, Alex
            </h1>

            <p className="mt-2 text-sm text-slate-300">
              📍 {currentLocation}
            </p>
          </div>

          <div className="hidden items-center gap-3 rounded-lg bg-[#173a5d] px-4 py-2.5 sm:flex">
            <span className="text-sm font-bold tracking-widest text-sky-50">
              MONDAY, 28 JULY 2026
            </span>

            <span className="text-slate-600">|</span>

            <span className="text-sm font-bold text-sky-50">
              09:47 AM
            </span>
          </div>
        </header>

        <section className="grid grid-cols-2 gap-3 xl:grid-cols-4">
          {[
            ['Total employees', '248'],
            ['Currently clocked in', '187'],
            ['Out / absent today', '47'],
            ['Late arrivals', '14']
          ].map(([label, num]) => (
            <Panel key={label} className="bg-[#173a5d] p-4">
              <span className="mt-4 block text-lg text-slate-300">{label}</span>
              <b className="mt-1 block text-4xl text-white">{num}</b>
            </Panel>
          ))}
        </section>

        <Panel className="mt-6 overflow-hidden bg-[#173a5d] text-slate-200">
          <div className="flex items-start justify-between p-5">
            <div>
              <h2 className="font-bold text-white">Employee directory</h2>
            </div>
            <button onClick={onOnboard} className="rounded-lg bg-sky-600 px-3 py-2 text-xs font-bold text-white hover:bg-sky-500">
              <span className="hidden sm:inline">+ Onboard new employee</span>
            </button>
          </div>

          <div className="flex gap-2 px-5 pb-4">
            <div className="flex max-w-sm flex-1 items-center rounded-lg bg-[#10233a] px-3">
              <input value={query} onChange={e => setQuery(e.target.value)} className="w-full bg-transparent px-2 py-2.5 text-xs outline-none" placeholder="Search name or employee ID..." />
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[670px] text-left text-xs">
              <thead className="border-y border-[#345a7c] bg-[#10233a] text-[10px] tracking-wider font-bold text-white">
                <tr>
                  <th className="p-4">EMPLOYEE</th>
                  <th>DEPARTMENT</th>
                  <th>STATUS</th>
                  <th>LAST TIMESTAMP</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {rows.map(e => (
                  <tr key={e.id} className="border-b border-[#345a7c]">
                    <td className="p-4">
                      <div className="flex items-center gap-3">
                        <button onClick={() => setAction({ type: 'profile', employee: e })} className="rounded-full">
                          <Avatar {...e} small />
                        </button>
                        <div>
                          <b className="block text-slate-100">{e.name}</b>
                          <span className="text-[10px] text-slate-300">{e.id}</span>
                        </div>
                      </div>
                    </td>
                    <td>{e.dept}</td>
                    <td>
                      <span className={`text-xs font-semibold ${statusClass[e.status]}`}>
                        {e.status}
                      </span>
                    </td>
                    <td>
                      <b>{e.time}</b>
                    </td>
                    <td>
                      <div className="flex justify-end gap-2">
                        <button onClick={() => onEditEmployee(e)} className="rounded-lg px-5 py-3 bg-sky-600 text-xs font-bold hover:bg-slate-500">Edit</button>
                        <button onClick={() => setAction({ type: 'delete', employee: e })} className="rounded-lg bg-rose-800 px-5 py-3 mr-8 text-xs font-bold text-white">Remove</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>

      {action?.type === 'delete' && <DeleteModal employee={action.employee} onClose={() => setAction(null)} onRemove={removeEmployee} />}
      {action?.type === 'profile' && <ProfileModal employee={action.employee} onClose={() => setAction(null)} onEdit={employee => {
        setAction(null)
        onEditEmployee(employee)
      }} />}
      {pendingAdminRequest && <AdminRequestModal request={pendingAdminRequest} onApprove={approveAdminRequest} onClose={onClearAdminRequest} />}
    </main>
  )
}
