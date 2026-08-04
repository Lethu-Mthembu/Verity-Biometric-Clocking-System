import { useEffect, useMemo, useState } from 'react'
import Panel from '../../../shared/components/Panel'
import API from '../../../services/authServices'

const statusClass = {
  'Clocked In': 'text-[#22ff3c]',
  'Clocked Out': 'text-slate-300',
  'Open': 'text-[#ffe600]'
}

const currentLocation =
  sessionStorage.getItem("currentLocation") || "Location unavailable";

const formatTime = value => value
  ? new Intl.DateTimeFormat(undefined, { timeStyle: 'medium' }).format(new Date(value))
  : '-'

const formatMethod = value => value === 'FingerprintOverride' ? 'Admin override' : value || '-'

const formatDuration = (clockIn, clockOut, now) => {
  if (!clockIn) return '-'

  const start = new Date(clockIn)
  const end = clockOut ? new Date(clockOut) : now
  const totalMinutes = Math.max(0, Math.floor((end - start) / 60000))
  const days = Math.floor(totalMinutes / 1440)
  const hours = Math.floor((totalMinutes % 1440) / 60)
  const minutes = totalMinutes % 60
  const parts = []

  if (days) parts.push(`${days}d`)
  if (hours || days) parts.push(`${hours}h`)
  parts.push(`${minutes}m`)

  return parts.join(' ')
}

export default function HrDashboardPage({ onLogout }) {
  const [query, setQuery] = useState('')
  const [attendanceLogs, setAttendanceLogs] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [now, setNow] = useState(() => new Date())

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 1000)
    return () => clearInterval(timer)
  }, [])

  useEffect(() => {
    let cancelled = false

    const loadAttendanceLogs = async () => {
      try {
        setLoading(true)
        setError('')
        const response = await API.get('/Attendance/logs')
        if (!cancelled) setAttendanceLogs(Array.isArray(response.data) ? response.data : [])
      } catch (loadError) {
        console.error('Failed to load attendance logs:', loadError)
        if (!cancelled) setError('Unable to load attendance logs.')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    loadAttendanceLogs()
    return () => { cancelled = true }
  }, [])

  const rows = useMemo(() => {
    const search = query.trim().toLowerCase()
    if (!search) return attendanceLogs

    return attendanceLogs.filter(log => `${log.employeeName} ${log.employeeNumber}`.toLowerCase().includes(search))
  }, [attendanceLogs, query])

  const currentDate = new Intl.DateTimeFormat(undefined, {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
  }).format(now).toUpperCase()
  const currentTime = new Intl.DateTimeFormat(undefined, { timeStyle: 'short' }).format(now)

  return (
    <main className="flex min-h-screen bg-[#12304c] text-slate-200">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-[#284968] bg-[#10233a] p-5 md:flex">
        <div className="flex items-center gap-3">
          <div>
            <b className="text-xl tracking-widest text-white">VERITY</b>
            <span className="block text-[9px] tracking-widest text-slate-400">HR CONSOLE</span>
          </div>
        </div>
        <nav className="mt-10 grid gap-1 text-sm font-semibold">
          <a className="rounded-lg bg-[#245a84] px-3 py-3 text-sky-100">Attendance logs</a>
        </nav>
        <button onClick={onLogout} className="mt-auto px-3 py-2 text-xs font-bold text-slate-300 hover:text-white">Log out</button>
      </aside>

      <div className="min-w-0 flex-1 p-5 md:p-9">
        <header className="mb-7 flex items-center justify-between">
          <h1 className="text-3xl font-bold tracking-tight text-white">Attendance logs</h1>

          <p className="mt-2 text-sm text-slate-300">
            📍 {currentLocation}
          </p>

          <div className="hidden items-center gap-3 rounded-lg bg-[#173a5d] px-4 py-2.5 sm:flex">
            <span className="text-sm font-bold tracking-widest text-sky-50">{currentDate}</span>
            <span className="text-slate-600">|</span>
            <span className="text-sm font-bold text-sky-50">{currentTime}</span>
          </div>
        </header>

        <Panel className="overflow-hidden bg-[#173a5d] text-slate-200">
          <div className="flex items-start justify-between p-5">
            <div>
              <h2 className="font-bold text-white">Attendance logs</h2>
            </div>
          </div>

          <div className="flex gap-2 px-5 pb-4">
            <div className="flex max-w-sm flex-1 items-center rounded-lg bg-[#10233a] px-3">
           <input id="hr-search" name="search" autoComplete="off" aria-label="Search name or employee ID" value={query} onChange={event => setQuery(event.target.value)} className="w-full bg-transparent px-2 py-2.5 text-xs outline-none" placeholder="Search name or employee ID..." />            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full min-w-[860px] text-left text-xs">
              <thead className="border-y border-[#345a7c] bg-[#10233a] text-[10px] tracking-wider font-bold text-white">
                <tr>
                  <th className="p-4">EMPLOYEE</th>
                  <th>EMPLOYEE ID</th>
                  <th>DEPARTMENT</th>
                  <th>STATUS</th>
                  <th>CLOCK IN</th>
                  <th>CLOCK OUT</th>
                  <th>DURATION</th>
                  <th>METHOD</th>
                </tr>
              </thead>
              <tbody>
                {loading && <tr><td colSpan="8" className="p-6 text-center text-slate-400">Loading attendance logs...</td></tr>}
                {!loading && error && <tr><td colSpan="8" className="p-6 text-center text-rose-300">{error}</td></tr>}
                {!loading && !error && rows.length === 0 && <tr><td colSpan="8" className="p-6 text-center text-slate-400">No attendance logs found.</td></tr>}
                {!loading && !error && rows.map(log => {
                  const status = log.isActive ? 'Clocked In' : log.clockOut ? 'Clocked Out' : 'Open'
                  const methods = [log.clockInAuthMethod, log.clockOutAuthMethod]
                    .filter(Boolean)
                    .map(formatMethod)
                    .filter((method, index, values) => values.indexOf(method) === index)
                    .join(' / ') || '-'

                  return (
                  <tr key={log.attendanceId} className="border-b border-[#345a7c]">
                    <td className="p-4">
                      <b className="block text-slate-100">{log.employeeName || 'Unknown employee'}</b>
                    </td>
                    <td>{log.employeeNumber}</td>
                    <td>{log.department}</td>
                    <td><span className={`font-semibold ${statusClass[status]}`}>{status}</span></td>
                    <td><b>{formatTime(log.clockIn)}</b></td>
                    <td><b>{formatTime(log.clockOut)}</b></td>
                    <td>{formatDuration(log.clockIn, log.clockOut, now)}</td>
                    <td>{methods}</td>
                  </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>
    </main>
  )
}

