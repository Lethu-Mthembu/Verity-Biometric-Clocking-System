import { useEffect, useRef, useState } from 'react'
import { Check } from 'lucide-react'
import Modal from '../../../shared/components/Modal'
import backGr from "../../../assests/main-background.jpeg";
import { loadFaceModels, getFaceDescriptor } from '../../../shared/lib/faceModels'
import { FaEye, FaEyeSlash } from "react-icons/fa";
import { getCurrentLocation, getLocationName } from "../../../services/locationService";
import API, { login } from '../../../services/authServices'


function AdminLogin({ onClose, onLogin }) {

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState("");
  const [error, setError] = useState('')
  const [loggingIn, setLoggingIn] = useState(false)
  const loginRole = async () => {
    if (!email.trim() || !password || loggingIn) return
    setLoggingIn(true)
    setError('')
    try {
      const auth = await login({ email: email.trim(), password })
      localStorage.setItem('token', auth.token)
      localStorage.setItem('userId', auth.userId)
      localStorage.setItem('role', auth.role)
      const coords = await getCurrentLocation();

      const locationName = await getLocationName(
        coords.latitude,
        coords.longitude
      );

      sessionStorage.setItem("currentLocation", locationName);

      onLogin(String(auth.role).toLowerCase() === 'hr' ? 'hr' : 'admin');

    } catch (error) {
      console.error(error);
      setError('Invalid email or password.')
    } finally {
      setLoggingIn(false)
    }
  };

  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">Admin access</h2>
      <p className="mt-2 text-sm leading-6 text-slate-400">Sign in to manage employee records and attendance.</p>
      <label className="mt-6 block text-[10px] font-bold">EMAIL OR USERNAME</label>
      <input value={email} onChange={event => setEmail(event.target.value)} className="mt-2 w-full rounded-lg bg-[#09192c] px-3 py-3 text-sm outline-none" placeholder="admin@verity.co" />
      <label className="mt-4 block text-[10px] font-bold">PASSWORD</label>
      <div className="password-container">
        <input
          type={showPassword ? "text" : "password"}
          name="password"
          value={password}
          onChange={event => setPassword(event.target.value)}
          placeholder="......"
          className="password-input"
        />

        <button
          type="button"
          className="password-toggle"
          onClick={() => setShowPassword(!showPassword)}
        >
          {showPassword ? <FaEyeSlash /> : <FaEye />}
        </button>
      </div>
      {error && <p className="mt-3 text-xs font-bold text-rose-400">{error}</p>}
      <button disabled={!email.trim() || !password || loggingIn} onClick={loginRole} className="mt-5 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white hover:bg-sky-500 disabled:opacity-40">{loggingIn ? 'Logging in...' : 'Log in'}</button>
    </Modal>
  )
}



function CallAdmin({ onClose, onNotify }) {
  const [employeeNumber, setEmployeeNumber] = useState('')   // the ID typed into the field
  const [submitting, setSubmitting] = useState(false)   // true while the request is in flight
  const [error, setError] = useState('')   // holds an error message if the request fails

  const handleNotify = async () => {
    const trimmedNumber = employeeNumber.trim()   // strip whitespace before sending
    if (!trimmedNumber || submitting) return   // don't submit empty or duplicate requests

    setSubmitting(true)   // show "Notifying admin…" state
    setError('')   // clear any previous error

    try {
      const response = await API.post('/admin/notify', {
        employeeNumber: trimmedNumber,
        reason: 'Face recognition failed.'
      })

      onNotify(
        trimmedNumber,
        response.data.overrideRequestId,
        response.data.requestedClockType
      )
      onClose()   // only close the modal on real success
    } catch (error) {
      setError(error.response?.data?.message || 'Could not notify admin. Please try again.')
    } finally {
      setSubmitting(false)   // reset button state regardless of outcome
    }
  }

  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">Admin notified... Please wait</h2>
      <label className="mt-6 block text-[10px] font-bold">EMPLOYEE NUMBER</label>
      <input value={employeeNumber} onChange={e => setEmployeeNumber(e.target.value)} className="mt-2 w-full rounded-lg bg-[#09192c] px-3 py-3 text-sm outline-none" placeholder="EMP-1042" />
      <div className="my-6 rounded-lg bg-[#09192c] p-4 text-sky-300">
        <b className="text-sm text-white">status must change from clocked in or out whithe user waits</b>
      </div>
      {error && <p className="mb-3 text-xs font-bold text-rose-400">{error}</p>}   {/* visible failure state instead of silently faking success */}
      <button disabled={!employeeNumber.trim() || submitting} onClick={handleNotify} className="w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white disabled:opacity-40">
        {submitting ? 'Notifying admin…' : 'Notify admin'}
      </button>
    </Modal>
  )
}

function NoticeModal({ message, onClose }) {
  return (
    <Modal onClose={onClose}>
      <h2 className="text-xl font-bold text-white">{message}</h2>
      <button onClick={onClose} className="mt-6 w-full rounded-lg bg-sky-600 py-3 text-sm font-bold text-white">Understood</button>
    </Modal>
  )
}

function Success({ employee, clockType }) {
  const [count, setCount] = useState(3);
  useEffect(() => {
    const countdown = setInterval(() => {
      setCount(value => Math.max(value - 1, 0))
    }, 1000)

    return () => clearInterval(countdown)
  }, []);

  return (
    <main className="min-h-screen bg-[#071525] px-5 pt-[14vh] text-center">
      <div className="mx-auto grid h-28 w-28 place-items-center rounded-full border-[10px] border-emerald-400/10 bg-emerald-500 text-white shadow-xl shadow-emerald-950/50">
        <Check size={65} strokeWidth={3} />
      </div>
      <p className="mt-8 text-xs font-semibold text-emerald-300">IDENTITY VERIFIED</p>
      <h1 className="mt-3 text-4xl font-bold tracking-tight text-white">Successfully Clocked {clockType === 'ClockOut' ? 'Out' : 'In'}</h1>
      <div className="mx-auto mt-7 flex w-full max-w-sm items-center gap-3 rounded-xl bg-[#eef4fc] p-4">
        <div className="flex-1 text-left">
          <b className="block text-xl text-[#071525]">{employee?.name}</b>
          <span className="text-sm text-[#071525]">{employee?.employeeNumber}</span>
        </div>
        <time className="pl-3 leading-5 text-sm text-[#071525]">
          2026-07-28<br />
          <b className="text-sm">09:47:15</b>
        </time>
      </div>
      <p className="mt-5 text-xs text-white">Returning to kiosk in {count} seconds</p>
    </main>
  )
}

export default function KioskPage({ onAdminAccess, onAdminCall }) {
  const videoRef = useRef(null)
  const canvasRef = useRef(null)
  const faceCheckStartedRef = useRef(false)
  const [now, setNow] = useState(new Date());
  const [otp, setOtp] = useState('');
  const [modal, setModal] = useState(null);
  const [verifying, setVerifying] = useState(false);
  const [cameraError, setCameraError] = useState('');
  const [cameraRetryKey, setCameraRetryKey] = useState(0);
  const [faceStatus, setFaceStatus] = useState('Looking for face...');
  const [success, setSuccess] = useState(false);
  const [successClockType, setSuccessClockType] = useState('ClockIn');
  const [otpChallengeId, setOtpChallengeId] = useState(null)
  const [matchedEmployeeNumber, setMatchedEmployeeNumber] = useState(null)
  const [matchedEmployee, setMatchedEmployee] = useState(null)

  useEffect(() => { loadFaceModels() }, [])   // loads the face-api.js models once when the kiosk page mounts

  const resetToKiosk = () => {
    setSuccess(false);
    setSuccessClockType('ClockIn');
    setOtp('');
    setVerifying(false);
    setModal(null);
    setMatchedEmployeeNumber(null)
    setMatchedEmployee(null)
    faceCheckStartedRef.current = false;
    setCameraRetryKey(value => value + 1)
  }

  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(t)
  }, []);

  useEffect(() => {
    if (success) return

    let stream
    let stopped = false
    const videoElement = videoRef.current

    const captureFrame = async () => {
      const video = videoRef.current   // grab the current video element
      if (!video || video.readyState < 2) {
        return { descriptor: null, message: 'Camera is starting. Please wait...' }   // camera not ready yet
      }

      const descriptor = await getFaceDescriptor(video)   // run face-api.js directly on the live video frame
      if (!descriptor) {
        return { descriptor: null, message: 'No face detected. Face the camera.' }   // face-api.js found no face
      }

      return { descriptor, message: 'Face detected. Verifying...' }   // got a usable descriptor
    }

    const verifyFace = async () => {
      if (stopped || faceCheckStartedRef.current) return   // skip if already checking or camera stopped
      faceCheckStartedRef.current = true   // lock so this doesn't run again until finished
      const { descriptor, message } = await captureFrame()   // get the 128-number descriptor from the current frame

      if (!descriptor) {
        faceCheckStartedRef.current = false   // unlock so the next interval tick can try again
        setFaceStatus(message)   // show why it didn't work (camera starting / no face)
        return
      }

      setFaceStatus(message)   // show "Face detected. Verifying..."

      try {
        const { data: result } = await API.post('/face/verify', { descriptor })

        if (result.matched) {
          const employee = result.employee || {}
          const name = `${result.fname || employee.fname || ''} ${result.lastname || employee.lastname || ''}`.trim()
          setFaceStatus(`Welcome, ${name}`)
          setModal('face-success')
          setOtpChallengeId(result.otpChallengeId)
          setMatchedEmployeeNumber(result.employeeNumber)
          setMatchedEmployee({ name, employeeNumber: result.employeeNumber })

          if (result.clockType === 'ClockOut' && result.clockedOut) {
            setSuccessClockType('ClockOut')
            setSuccess(true)
            return
          }
        } else {
          setFaceStatus('Face not found')   // no match — keep trying
          faceCheckStartedRef.current = false   // unlock so it retries on the next interval
        }
      } catch (error) {
        setFaceStatus(error.response?.data?.message || 'Verification error. Retrying...')
        faceCheckStartedRef.current = false   // unlock so it retries on the next interval
      }
    }

    const startCamera = async () => {
      try {
        setCameraError('')
        setFaceStatus('Looking for face...')
        faceCheckStartedRef.current = false
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' }, audio: false })
        if (!videoRef.current) return

        videoRef.current.srcObject = stream
        videoRef.current.onloadedmetadata = () => {
          videoRef.current.play()
          setFaceStatus('Looking for face...')
          const check = setInterval(() => {
            if (!stopped && !faceCheckStartedRef.current) verifyFace()
          }, 1400)
          videoRef.current.dataset.faceCheck = String(check)
        }
      } catch {
        setCameraError('Camera access is required for facial recognition.')
        setFaceStatus('Camera unavailable')
      }
    }

    startCamera()

    return () => {
      stopped = true
      const check = Number(videoElement?.dataset.faceCheck)

      if (check) clearInterval(check)
      stream?.getTracks().forEach(track => track.stop())
    }
  }, [cameraRetryKey, success]);

  useEffect(() => {
    if (!success) return

    const t = setTimeout(resetToKiosk, 3000)
    return () => clearTimeout(t)
  }, [success])

  if (success) return <Success employee={matchedEmployee} clockType={successClockType} />

  const add = n => setOtp(v => v.length < 6 ? v + n : v)

  const handleVerifyOtp = async () => {
    if (otp.length !== 6 || verifying) return   // don't submit incomplete or duplicate requests
    setVerifying(true)   // show "Checking your code…" state

    try {
      const { data: result } = await API.post('/otp/verify', { challengeId: otpChallengeId, code: otp })

      if (!result.valid) {
        setVerifying(false)   // stop the "checking" state
        setFaceStatus('Invalid OTP. Please try again.')   // show the real reason it failed
        return
      }

      setSuccessClockType('ClockIn')
      setSuccess(true)   // only fires once the backend explicitly confirmed a valid OTP
    } catch (error) {
      setVerifying(false)   // stop the "checking" state
      setFaceStatus(error.response?.data?.message || 'Could not verify OTP. Please try again.')
    }
  }

  const handleResendOtp = async () => {
    try {
      const { data } = await API.post('/otp/challenge', { employeeNumber: matchedEmployeeNumber, clockType: 'ClockIn' })
      setOtpChallengeId(data.challengeId)
      setOtp('')
      setModal('otp-valid')   // only show the "OTP is valid" notice on real success
    } catch (error) {
      setModal(null)
      setFaceStatus(error.response?.data?.message || 'Could not send a new code. Please try again.')
    }
  }

  return (
    <main className="flex min-h-screen flex-col text-slate-100" style={{ backgroundImage: `linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0.5)), url(${backGr})`, backgroundSize: 'cover', backgroundPosition: 'center' }}>
      <header className="flex h-20 items-center justify-between bg-sky-700 px-5 md:px-[5vw]">
        <b className="text-sm">VERITY ATTENDANCE TERMINAL</b>
        <div className="hidden text-center sm:block">
          <b className="text-base">{now.toLocaleTimeString()}</b>
          <span className="block text-[12px] text-white">{now.toLocaleDateString([], { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}</span>
        </div>
        <div className="flex gap-2">
          <button onClick={() => setModal('call')} className="hidden rounded-lg bg-[#10233a] px-3 py-2 text-xs font-bold md:flex">Call Admin</button>
          <button onClick={() => setModal('login')} className="rounded-lg bg-sky-600 px-3 py-2 text-xs font-bold text-white hover:bg-sky-500">
            <span className="hidden sm:inline">Admin Portal</span>
          </button>
        </div>
      </header>

      <section className="mx-auto grid w-full max-w-6xl flex-1 items-center gap-10 px-6 py-10 lg:grid-cols-[.85fr_1fr] lg:gap-28">
        <div>
          <h1 className="mt-5 text-4xl font-bold leading-tight tracking-tight text-white md:text-5xl">
            Welcome.<br />
            <span className="text-sky-300">Let's get you checked in.</span>
          </h1>
          <p className="mt-5 max-w-md text-lg font-bold leading-7 text-white">Position your face within the frame and enter the 6-digit OTP.</p>
        </div>

        <div className="overflow-hidden rounded-2xl bg-[#0d2035] border border-sky-700 shadow-2xl shadow-black/30">
          <div className="relative h-64 bg-white">
            <div className="flex items-center justify-between px-5 py-4 text-[10px] text-slate-300">
              <span>Camera</span>
              <b className="text-emerald-300">LIVE</b>
            </div>
            <video ref={videoRef} onPause={() => videoRef.current?.play().catch(() => null)} className="absolute inset-0 h-full w-full object-cover" muted playsInline autoPlay />
            <canvas ref={canvasRef} className="hidden" />
            <div className="absolute left-1/2 top-1/2 flex h-48 w-40 -translate-x-1/2 -translate-y-1/2 flex-col items-center justify-center rounded-[45%] border-2 border-sky-300 bg-sky-400/5 text-sky-200">
              <span className="text-[8px] font-bold tracking-widest">ALIGN FACE WITH FRAME</span>
            </div>
            <p className="absolute bottom-3 left-0 right-0 text-center text-[10px] font-bold tracking-widest text-sky-200">{cameraError || faceStatus}</p>
            {cameraError && (
              <button onClick={() => setCameraRetryKey(value => value + 1)} className="absolute bottom-9 left-1/2 -translate-x-1/2 rounded-lg bg-sky-600 px-3 py-2 text-xs font-bold text-white">
                Retry camera
              </button>
            )}
          </div>

          <div className="bg-[#eef4fc]">
            <div className="flex items-center justify-between p-5">
              <div>
                <p className="text-lg font-bold tracking-widest text-[#2b2f36]">ENTER ACCESS OTP</p>
                <div className="mt-3 flex gap-1.5">
                  {[0, 1, 2, 3, 4, 5].map(i => (
                    <span key={i} className={`grid h-7 w-6 place-items-center rounded border ${otp[i] ? 'border-sky-500 bg-sky-600 text-white' : 'border-slate-300 bg-white text-slate-400'}`}>
                      {otp[i] ? '•' : ''}
                    </span>
                  ))}
                </div>
              </div>
              <button disabled={otp.length !== 6 || verifying} onClick={handleVerifyOtp} className="rounded-lg bg-sky-600 px-3 py-3 text-xs font-bold text-white disabled:opacity-40">
                {verifying ? 'Checking your code…' : 'Confirm code'}
              </button>
            </div>

            <div className="px-5 pb-4">
              <button onClick={handleResendOtp} className="rounded-lg bg-sky-600 px-3 py-2 text-xs font-bold text-white hover:bg-sky-500">Resend OTP</button>
            </div>

            <div className="grid grid-cols-3 gap-2 px-5 pb-5">
              {[1, 2, 3, 4, 5, 6, 7, 8, 9].map(n => (
                <button key={n} onClick={() => add(n)} className="rounded-md bg-white py-2 text-sm font-bold text-slate-700 hover:bg-blue-100">{n}</button>
              ))}
              <button onClick={() => setOtp('')} className="rounded-md bg-white py-2 text-sm font-bold text-slate-500 hover:bg-blue-100">Clear</button>
              <button onClick={() => add(0)} className="rounded-md bg-white py-2 font-bold text-slate-700 hover:bg-blue-100">0</button>
              <button onClick={() => setOtp(v => v.slice(0, -1))} className="rounded-md bg-white text-slate-700 hover:bg-blue-100">←</button>
            </div>
          </div>
        </div>
      </section>

      <footer className="flex justify-between bg-sky-700 px-[5vw] py-4 text-sm font-bold text-white">
        <span>Biometric data is encrypted and protected</span>
      </footer>

      {modal === 'call' && <CallAdmin onClose={() => setModal(null)} onNotify={(employeeNumber, overrideRequestId, requestedClockType) => {
        onAdminCall(employeeNumber, overrideRequestId, requestedClockType);
        setModal(null)
      }} />}
      {modal === 'login' && <AdminLogin onClose={() => setModal(null)} onLogin={onAdminAccess} />}
      {modal === 'face-success' && <NoticeModal message="Face rec success...standby for otp" onClose={() => setModal(null)} />}
      {modal === 'otp-valid' && <NoticeModal message="OTP is valid for 45 seconds" onClose={() => setModal(null)} />}
    </main>
  )
}
