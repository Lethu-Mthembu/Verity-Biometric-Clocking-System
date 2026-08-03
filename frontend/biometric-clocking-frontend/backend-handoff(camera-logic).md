# Biometric Attendance Frontend Backend Handoff

## Overview

This frontend is a Vite + React biometric attendance terminal. It currently contains:

- Kiosk page for facial verification and OTP entry.
- Admin dashboard for employee management.
- HR dashboard for attendance logs.
- Onboarding/edit employee page with camera capture.

The frontend now uses `face-api.js` to detect a face and generate a face descriptor before calling the backend.

## Most Important Change

The frontend now produces a face descriptor with **exactly 128 numeric values**.

This descriptor is generated in:

```txt
src/shared/lib/faceModels.js
```

The helper validates:

- descriptor exists
- descriptor length is exactly `128`
- every value is a finite JavaScript number

If that validation fails, the helper returns `null`.

## Face Model Files

The frontend loads face-api models from:

```txt
public/models
```

Required files:

```txt
tiny_face_detector_model-weights_manifest.json
tiny_face_detector_model-shard1
face_landmark_68_model-weights_manifest.json
face_landmark_68_model-shard1
face_recognition_model-weights_manifest.json
face_recognition_model-shard1
face_recognition_model-shard2
```

The models are loaded with:

```js
faceapi.nets.tinyFaceDetector.loadFromUri('/models')
faceapi.nets.faceLandmark68Net.loadFromUri('/models')
faceapi.nets.faceRecognitionNet.loadFromUri('/models')
```

## Frontend Routes And Screens

Routing is currently handled in frontend state, not React Router.

Main routing file:

```txt
src/routes/AppRoutes.jsx
```

Screens:

```txt
src/features/kiosk/pages/KioskPage.jsx
src/features/dashboard/pages/DashboardPage.jsx
src/features/hr/pages/HrDashboardPage.jsx
src/features/onboarding/pages/OnboardPage.jsx
```

## Temporary Login Rules

Temporary login emails are configured here:

```txt
src/config/auth.js
```

Current frontend values:

```js
export const LOGIN_EMAILS = {
  admin: 'admin@verity.co',
  hr: 'hr@verity.co'
}
```

Behavior:

- `admin@verity.co` opens the Admin dashboard.
- `hr@verity.co` opens the HR dashboard.
- Anything else currently falls back to Admin.

Suggested future auth endpoint:

```txt
POST /api/auth/login
Content-Type: application/json
```

## Employee Onboarding Endpoint

Used by:

```txt
src/features/onboarding/pages/OnboardPage.jsx
```

Create employee:

```txt
POST /api/employees
Content-Type: multipart/form-data
```

Edit employee:

```txt
PUT /api/employees/:id
Content-Type: multipart/form-data
```

Form fields:

```txt
firstName
lastName
employeeId
email
phone
department
role
mode
faceDescriptor
```

`faceDescriptor` is appended to the `FormData` as a JSON string.

Example:

```js
formData.append('faceDescriptor', JSON.stringify(faceDescriptor))
```

Backend parsing example:

```js
const faceDescriptor = JSON.parse(req.body.faceDescriptor)
```

Backend must validate:

```js
Array.isArray(faceDescriptor) &&
faceDescriptor.length === 128 &&
faceDescriptor.every(value => typeof value === 'number' && Number.isFinite(value))
```

Important:

- The current onboarding flow sends the 128-number descriptor.
- It does **not** currently send the JPEG image file in this latest flow.
- The preview image is only used in the browser UI.

Backend responsibilities:

1. Validate all employee fields.
2. Parse `faceDescriptor`.
3. Confirm it is exactly 128 finite numeric values.
4. Store the descriptor for future matching.
5. Return the saved employee.

Suggested response:

```json
{
  "employee": {
    "id": "EMP-1042",
    "firstName": "Amara",
    "lastName": "Mensah",
    "department": "Product",
    "status": "Clocked In"
  }
}
```

## Kiosk Face Verification Endpoint

Used by:

```txt
src/features/kiosk/pages/KioskPage.jsx
```

Endpoint:

```txt
POST /api/face/verify
Content-Type: application/json
```

Request body:

```json
{
  "descriptor": [0.0123, -0.044, 0.091]
}
```

The example above is shortened. The real `descriptor` array contains exactly **128 numeric values**.

Backend must validate:

```js
Array.isArray(descriptor) &&
descriptor.length === 128 &&
descriptor.every(value => typeof value === 'number' && Number.isFinite(value))
```

Backend responsibilities:

1. Receive descriptor.
2. Validate it has exactly 128 finite numeric values.
3. Compare it against stored employee descriptors.
4. Return match/no-match.
5. If matched, generate/send OTP server-side.

Expected success response:

```json
{
  "matched": true,
  "name": "Amara Mensah",
  "employee": {
    "id": "EMP-1042",
    "name": "Amara Mensah",
    "department": "Product"
  },
  "otpSent": true
}
```

Expected no-match response:

```json
{
  "matched": false,
  "message": "Face not found"
}
```

Important:

- The frontend currently expects `result.matched`.
- If `matched` is true, it shows the success popup.
- If `matched` is false, it shows `Face not found` and keeps retrying.
- If the request errors, it shows `Verification error. Retrying...`.

## Database Recommendation

If using PostgreSQL with `pgvector`, store the descriptor as:

```sql
face_embedding vector(128)
```

Example employee table:

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE employees (
  id TEXT PRIMARY KEY,
  first_name TEXT NOT NULL,
  last_name TEXT NOT NULL,
  email TEXT,
  phone TEXT,
  department TEXT,
  role TEXT,
  face_embedding vector(128),
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

Example similarity search:

```sql
SELECT id, first_name, last_name
FROM employees
ORDER BY face_embedding <-> $1
LIMIT 1;
```

The backend must decide the similarity threshold. Do not accept the nearest match unless it is close enough.

## OTP Verification Endpoint

The OTP UI exists, but confirmation is currently simulated.

Suggested endpoint:

```txt
POST /api/otp/verify
Content-Type: application/json
```

Request:

```json
{
  "employeeId": "EMP-1042",
  "otp": "123456"
}
```

Response:

```json
{
  "valid": true,
  "attendance": {
    "employeeId": "EMP-1042",
    "status": "Clocked In",
    "clockTime": "08:42:11",
    "createdAt": "2026-07-28T08:42:11.000Z",
    "method": "Normal"
  }
}
```

## Attendance Logs

HR dashboard uses attendance logs as its landing page.

Frontend file:

```txt
src/features/hr/pages/HrDashboardPage.jsx
```

Current frontend displays:

```txt
employeeName
employeeId
department
status
clockTime
createdAt
method
```

`method` should be:

```txt
Normal
Overridden by admin
```

Suggested endpoint:

```txt
GET /api/attendance-logs?search=amara
```

Response:

```json
{
  "logs": [
    {
      "employeeName": "Amara Mensah",
      "employeeId": "EMP-1042",
      "department": "Product",
      "status": "Clocked In",
      "clockTime": "08:42:11",
      "createdAt": "2026-07-28T08:42:11.000Z",
      "method": "Normal"
    }
  ]
}
```

Search should filter by:

- Employee name
- Employee ID

## Admin Attendance Override Flow

The kiosk has a `Call Admin` popup.

User enters employee number. Admin sees a popup that says the employee is trying to clock in/out. Admin can approve.

Suggested backend endpoints:

```txt
POST /api/admin-attendance-requests
GET /api/admin-attendance-requests/pending
POST /api/admin-attendance-requests/:id/approve
```

Admin-approved attendance should create logs with:

```txt
method = "Overridden by admin"
```

## Employee Removal

The frontend currently uses browser WebAuthn / Windows Hello as a local confirmation before removing an employee from local state.

Backend must still enforce proper authorization.

Suggested endpoint:

```txt
DELETE /api/employees/:id
```

Backend should verify:

- Authenticated user exists.
- User role is Admin.
- Optional audit log is created.

## Current Test Result

Automated browser testing showed:

- Kiosk camera flow works in Chrome.
- Kiosk can generate a face descriptor when a face is detected.
- Onboarding camera opens in Chrome.
- Onboarding capture stores a JPEG preview and a 128-number descriptor.
- Re-capture removes the preview and clears the descriptor.

Real matching depends on `/api/face/verify` comparing the descriptor against stored employee descriptors.

