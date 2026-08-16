using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiometricClockingSystem.Api.Data;
using BiometricClockingSystem.Api.Models;
using BiometricClockingSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace BiometricClockingSystem.Api.Controllers
{
    // This runs on the shared kiosk device - no employee login needed, since
    // it's their own face (or an admin's fingerprint override) that
    // authenticates the action.
    [AllowAnonymous]
    public class ClockingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFacialRecognitionService _facialRecognitionService;
        private readonly IOtpService _otpService; // <-- implemented by your teammate

        public ClockingController(
            ApplicationDbContext context,
            IFacialRecognitionService facialRecognitionService,
            IOtpService otpService)
        {
            _context = context;
            _facialRecognitionService = facialRecognitionService;
            _otpService = otpService;
        }

        // GET: /Clocking
        // Shows the kiosk screen: employee number entry + webcam capture.
        [HttpGet]
        public IActionResult Index()
        {
            return View(new ClockInViewModel());
        }

        // POST: /Clocking/ScanFace
        // Step 1 of the employee's flow: they enter their Employee ID and a
        // photo is captured, then compared against their registration photo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanFace(ClockInViewModel model)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == model.EmployeeNumber && e.IsActive);

            if (employee == null)
            {
                model.ErrorMessage = "Employee ID not found.";
                return View(nameof(Index), model);
            }

            if (string.IsNullOrWhiteSpace(model.ScannedFaceImageBase64))
            {
                model.ErrorMessage = "Please capture your photo before continuing.";
                return View(nameof(Index), model);
            }

            byte[] scannedBytes;
            try
            {
                scannedBytes = ConvertBase64ToBytes(model.ScannedFaceImageBase64);
            }
            catch (FormatException)
            {
                model.ErrorMessage = "The captured photo could not be processed. Please try again.";
                return View(nameof(Index), model);
            }

            // Compare the freshly scanned face against the photo captured at registration.
            if (employee.FaceImage is null || employee.FaceImage.Length == 0)
            {
                return BadRequest("No stored face image found for this employee.");
            }

            var matchResult = await _facialRecognitionService.VerifyAsync(employee.FaceImage, scannedBytes);

            if (!matchResult.IsMatch)
            {
                // Facial recognition failed - employee should now use the
                // "Call Administrator" button (see RequestAdminAssistance below).
                model.ErrorMessage = "Facial recognition did not succeed. " +
                    "You can retry, or tap \"Call Administrator\" below for assistance.";
                return View(nameof(Index), model);
            }

            // --------------------------------------------------------------
            // Facial recognition succeeded - hand off to the OTP service.
            // Everything past this point (generating the code, sending it,
            // letting the employee enter it, and actually recording the
            // clock-in/out) is owned by your teammate's IOtpService
            // implementation - this call is the full extent of this file's
            // responsibility.
            // --------------------------------------------------------------
            await _otpService.GenerateAndSendAsync(employee.EmployeeNumber);

            return View("OtpTriggered", model);
        }

        // POST: /Clocking/RequestAdminAssistance
        // The "Call Administrator" button. Queues a request that shows up on
        // the admin's dashboard (see AdminOverrideController) so an admin can
        // come over and use their fingerprint to override for this employee.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAdminAssistance(string employeeNumber, ClockType clockType)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber && e.IsActive);

            if (employee == null)
            {
                return Json(new { success = false, message = "Employee ID not found." });
            }

            var request = new OverrideRequest
            {

                EmployeeId = employee.EmployeeNumber,
                RequestedClockType = clockType,
                RequestedAt = DateTime.UtcNow,
                Status = OverrideRequestStatus.Pending
            };

            _context.OverrideRequests.Add(request);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "An administrator has been notified and will assist you shortly." });
        }

        private static byte[] ConvertBase64ToBytes(string base64String)
        {
            var commaIndex = base64String.IndexOf(',');
            var rawBase64 = commaIndex >= 0 ? base64String.Substring(commaIndex + 1) : base64String;
            return Convert.FromBase64String(rawBase64);
        }
    }
}
