import { useEffect, useRef, useState } from 'react'
import Panel from '../../../shared/components/Panel'
import { loadFaceModels, getFaceDescriptor } from '../../../shared/lib/faceModels'   // face-api.js helpers
import API from '../../../services/authServices'

const Field = ({ name, label, placeholder, select, defaultValue }) => (   // reusable form field component
  <label className="block text-[10px] font-bold tracking-widest text-slate-400">
    {label}
    {select ? (
      <select name={name} defaultValue={defaultValue || 'Select department'} className="mt-2 w-full rounded-lg bg-[#09192c] p-3 text-sm text-slate-300">
        <option>Select department</option>
        <option>Engineering</option>
        <option>Product</option>
        <option>Operations</option>
        <option>Finance</option>
        <option>People</option>
      </select>
    ) : (
      <input name={name} defaultValue={defaultValue} className="mt-2 w-full rounded-lg bg-[#09192c] p-3 text-sm outline-none focus:border-sky-500" placeholder={placeholder}/>
    )}
  </label>
)

export default function OnboardPage({ mode = 'create', employee, onSaved, onBack }) {
  const formRef = useRef(null)   // ref to read all the form field values on save
  const videoRef = useRef(null)   // ref to the live camera video element
  const canvasRef = useRef(null)   // ref to the hidden canvas used only for the preview thumbnail
  const [capturedPhoto, setCapturedPhoto] = useState('');   // data URL used just to show the preview image
  const [faceDescriptor, setFaceDescriptor] = useState(null);   // the 128-number descriptor that actually gets saved
  const [cameraError, setCameraError] = useState('');   // camera/face error message shown to the user
  const [cameraRetryKey, setCameraRetryKey] = useState(0);   // bumping this re-runs the camera-start effect
  const [saving, setSaving] = useState(false);   // tracks whether the save request is in flight
  const [registeredEmployeeNumber, setRegisteredEmployeeNumber] = useState('')
  const [employeeRecord, setEmployeeRecord] = useState(employee || null)
  const [loadingEmployee, setLoadingEmployee] = useState(mode === 'edit')
  const captured = Boolean(capturedPhoto);   // true once a photo has been taken
  const isEdit = mode === 'edit'
  const existingEmployee = employeeRecord || employee
  const [firstNameFromSummary, ...lastNameParts] = existingEmployee?.name?.split(' ') || []
  const firstName = existingEmployee?.firstName || firstNameFromSummary || ''
  const lastName = existingEmployee?.lastName || lastNameParts.join(' ')
  const displayedPhoto = capturedPhoto

  useEffect(() => { loadFaceModels() }, [])   // loads the face-api.js models once when the page mounts

  useEffect(() => {
    if (!isEdit || !employee?.id) return

    let cancelled = false
    const loadEmployee = async () => {
      try {
        setLoadingEmployee(true)
        const response = await API.get(`/Employee/number/${encodeURIComponent(employee.id)}`)
        if (!cancelled) {
          setEmployeeRecord(response.data)
        }
      } catch {
        if (!cancelled) setCameraError('Unable to load employee details.')
      } finally {
        if (!cancelled) setLoadingEmployee(false)
      }
    }

    loadEmployee()
    return () => { cancelled = true }
  }, [employee, isEdit])

  useEffect(() => {
    let stream   // holds the camera stream so it can be stopped on cleanup

    const startCamera = async () => {
      try {
        setCameraError('')   // clear any previous error before trying again
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' }, audio: false })   // request camera access
        if (!videoRef.current) return   // bail if the video element isn't mounted

        videoRef.current.srcObject = stream   // attach the camera stream to the video element
        videoRef.current.onloadedmetadata = () => videoRef.current.play()   // start playback once ready
      } catch {
        setCameraError('Camera access is required to capture a facial image.')   // camera denied/unavailable
      }
    }

    startCamera()

    return () => {
      stream?.getTracks().forEach(track => track.stop())   // stop the camera when leaving the page or retrying
    }
  }, [cameraRetryKey])

  const capturePhoto = async () => {
    if (capturedPhoto) {
      setCapturedPhoto('')   // clear the preview so it goes back to live video
      setFaceDescriptor(null)   // clear the stored descriptor — the old capture is fully discarded here
      return   // this click was a "re-capture" trigger, so stop here — next click takes a new photo
    }

    const video = videoRef.current   // the live camera element
    if (!video) return   // bail if not ready

    const descriptor = await getFaceDescriptor(video)   // run face-api.js on the current video frame
    if (!descriptor) {
      setCameraError('No face detected. Face the camera and try again.')   // face-api.js found no face
      return
    }

    const canvas = canvasRef.current   // hidden canvas, used only to snapshot a preview image
    canvas.width = video.videoWidth || 640   // match canvas size to video width
    canvas.height = video.videoHeight || 480   // match canvas size to video height
    canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height)   // draw the current frame onto the canvas

    setCameraError('')   // clear any previous error now that capture succeeded
    setFaceDescriptor(descriptor)   // store the new descriptor, replacing whatever was there before
    setCapturedPhoto(canvas.toDataURL('image/jpeg', 0.86))   // store a JPEG data URL just for the on-screen preview
  }

  const saveEmployee = async () => {
    const formValues = Object.fromEntries(new FormData(formRef.current))   // collect all the text/select field values

    // Shaped to match CreateEmployeeDto / UpdateEmployeeDto on the backend exactly.
    const payload = {
      firstName: formValues.firstName || '',
      lastName: formValues.lastName || '',
      department: formValues.department || '',
      position: formValues.role || '',           // backend field is "Position", form field is "role"
      phoneNumber: formValues.phone || '',        // backend field is "PhoneNumber", form field is "phone"
      email: formValues.email || '',
      faceImageBase64: capturedPhoto,
      faceDescriptor: faceDescriptor || []
    }

    setSaving(true)   // show the "Saving..." state on the button

    try {
      const response = isEdit
        ? await API.put(`/Employee/${existingEmployee.employeeNumber || existingEmployee.id}`, payload)
        : await API.post('/Employee', payload)

      if (isEdit) {
        await onSaved?.()
        onBack()
      } else {
        setRegisteredEmployeeNumber(response.data.employeeNumber)
      }
    } catch {
      // The existing form state remains available for a retry.
    } finally {
      setSaving(false)   // reset saving state regardless of outcome
    }
  }

  return (
    <main className="min-h-screen bg-[#071525] text-slate-200">
      <header className="flex h-18 items-center justify-between bg-[#0a1a2e] px-[5vw] py-4">
        <button onClick={onBack} className="text-xs font-bold text-sky-300">← Back to directory</button>
        <b className="text-sm tracking-widest text-white">VERITY</b>
      </header>

      <section className="mx-auto max-w-5xl px-5 py-10">
        <h1 className="mt-2 text-3xl font-bold tracking-tight text-white">{isEdit ? 'Edit employee details' : 'Onboard new employee'}</h1>

        <div className="mt-7 grid gap-5 lg:grid-cols-[1.25fr_.75fr]">
          <Panel className="p-6">
            <h2 className="font-bold text-white">Personal details</h2>
            <form key={`${existingEmployee?.employeeNumber || existingEmployee?.id || 'new'}-${loadingEmployee ? 'loading' : 'ready'}`} ref={formRef} className="mt-6 grid gap-x-4 gap-y-5 sm:grid-cols-2">
              <Field name="firstName" label="FIRST NAME" placeholder="e.g. Samira" defaultValue={firstName}/>
              <Field name="lastName" label="LAST NAME" placeholder="e.g. Patel" defaultValue={lastName}/>
              <Field name="email" label="WORK EMAIL" placeholder="samira@company.com" defaultValue={existingEmployee?.email}/>
              <Field name="phone" label="PHONE NUMBER" placeholder="+27 00 000 0000" defaultValue={existingEmployee?.phoneNumber}/>
              <Field name="department" label="DEPARTMENT" select defaultValue={existingEmployee?.department || existingEmployee?.dept}/>
              <Field name="role" label="ROLE" placeholder="e.g. Product Designer" defaultValue={existingEmployee?.position}/>
            </form>
          </Panel>

          <Panel className="flex flex-col p-6">
            <h2 className="font-bold text-white">Biometric enrollment</h2>

            <div className={`relative mt-6 flex min-h-44 flex-col items-center justify-center overflow-hidden rounded-xl border border-dashed ${captured ? 'border-emerald-400 bg-emerald-500/5 text-emerald-300' : 'border-slate-600 bg-[#09192c] text-sky-300'}`}>
              <video ref={videoRef} onPause={() => videoRef.current?.play().catch(() => null)} className="h-44 w-full object-cover" muted playsInline autoPlay/>
              {displayedPhoto && (
                <img src={displayedPhoto} alt="Employee facial image" className="absolute inset-0 h-44 w-full object-cover"/>
              )}
              <canvas ref={canvasRef} className="hidden"/>

              {faceDescriptor && (
                <span className="absolute right-2 top-2 rounded-full bg-emerald-500 px-2 py-1 text-[9px] font-bold text-white shadow">   {/* only shows once a valid descriptor is captured */}
                  ✓ Ready to save
                </span>
              )}

              <div className="absolute inset-x-0 bottom-0 bg-[#071525cc] p-3 text-center">
                <b className="text-sm text-slate-100">{captured ? 'Face captured' : 'Camera ready'}</b>
                <span className="mt-1 block text-[10px] text-slate-400">{cameraError || (captured ? 'New biometric image ready' : (isEdit ? 'Capture a new facial image to save edits' : 'Ensure even lighting and clear face visibility'))}</span>
              </div>
              {cameraError && (
                <button onClick={() => setCameraRetryKey(value => value + 1)} className="absolute left-1/2 top-4 -translate-x-1/2 rounded-lg bg-sky-600 px-3 py-2 text-xs font-bold text-white">
                  Retry camera
                </button>
              )}
            </div>
            <button onClick={capturePhoto} className="mt-4 w-full rounded-lg  bg-[#10233a] py-3 text-xs font-bold hover:bg-slate-500">
              {isEdit ? 'Re-capture image' : (captured ? 'Re-capture photo' : 'Capture facial image')}
            </button>
            <p className="mt-5 text-[10px] leading-4 text-slate-400">
              A new facial image is required for every edit. Stored biometric data is never sent back to the browser.
            </p>
          </Panel>
        </div>

        {registeredEmployeeNumber && (
          <div className="mt-6 rounded-lg bg-emerald-500/10 p-4 text-sm text-emerald-200">
            Employee registered. Their generated employee number is <b>{registeredEmployeeNumber}</b>.
          </div>
        )}
        <footer className="mt-6 flex justify-end gap-3">
          <button onClick={onBack} className="rounded-lg px-5 py-3 text-xs font-bold  hover:bg-slate-500">Cancel</button>
          <button onClick={saveEmployee} disabled={saving || loadingEmployee || !displayedPhoto || !faceDescriptor || faceDescriptor.length !== 128 || Boolean(registeredEmployeeNumber)} className="rounded-lg bg-sky-600 px-5 py-3 text-xs font-bold text-white hover:bg-slate-700 disabled:opacity-40">{saving ? 'Saving...' : (registeredEmployeeNumber ? 'Employee registered' : (isEdit ? 'Save changes' : 'Register employee'))}</button>
        </footer>
      </section>
    </main>
  )
}
