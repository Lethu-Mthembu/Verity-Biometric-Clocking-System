import { useMemo, useState } from 'react'
import Panel from '../../../shared/components/Panel'
import { attendanceLogs } from '../data/attendanceLogs'

const statusClass = {
  'Clocked In': 'text-[#22ff3c]',
  'Clocked Out': 'text-slate-300',
  'Late': 'text-[#ffe600]',
  'Absent': 'text-[#ff2d2d]'
}

const currentLocation =
  sessionStorage.getItem("currentLocation") || "Location unavailable";

export default function HrDashboardPage({ onLogout }) {
  const [query, setQuery] = useState('')
  const rows = useMemo(() => {
    const search = query.trim().toLowerCase()
    if (!search) return attendanceLogs

    return attendanceLogs.filter(log => `${log.employeeName} ${log.employeeId}`.toLowerCase().includes(search))
  }, [query])

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
            <span className="text-sm font-bold tracking-widest text-sky-50">MONDAY, 28 JULY 2026</span>
            <span className="text-slate-600">|</span>
            <span className="text-sm font-bold text-sky-50">09:47 AM</span>
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
                  <th>CLOCK TIME</th>
                  <th>TIMESTAMP</th>
                  <th>METHOD</th>
                </tr>
              </thead>
              <tbody>
                {rows.map(log => (
                  <tr key={`${log.employeeId}-${log.createdAt}`} className="border-b border-[#345a7c]">
                    <td className="p-4">
                      <b className="block text-slate-100">{log.employeeName}</b>
                    </td>
                    <td>{log.employeeId}</td>
                    <td>{log.department}</td>
                    <td><span className={`font-semibold ${statusClass[log.status]}`}>{log.status}</span></td>
                    <td><b>{log.clockTime}</b></td>
                    <td>{log.createdAt}</td>
                    <td>{log.method}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Panel>
      </div>
    </main>
  )
}

