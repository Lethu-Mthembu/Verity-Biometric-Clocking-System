# Biometric Attendance System

This is an independent copy of the biometric attendance system for separate development and hosting.

It has its own Git history, backend service, database, and deployment configuration. The original team repository and hosting are not used by this project.

## Live application

[Open Verity Attendance](https://biometric-attendance-side-web.onrender.com/kiosk)

## Backend

The ASP.NET Core API is in `backend/ClockingSystem/BiometricClockingSystem.Api` and uses the included Entity Framework migrations.

Configure `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, and the `Twilio__*` settings through the hosting provider or local secret configuration. Do not commit credentials.

## Frontend

The React frontend is in `frontend/biometric-clocking-frontend`.
# Biometric-Attendance-Management-System
