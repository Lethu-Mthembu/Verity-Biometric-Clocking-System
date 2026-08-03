import { useState, useEffect } from "react";
import { Routes, Route, Navigate, useNavigate } from "react-router-dom";
import { getEmployees } from "../services/employeeService";

import Dashboard from "../features/dashboard/pages/Dashboard";
import HrDashboard from "../features/hr/pages/HrDashboard";
import KioskPage from "../features/kiosk/pages/KioskPage";
import OnboardPage from "../features/onboarding/pages/OnboardPage";

import { initialEmployees } from "../features/dashboard/data/employees";

function AppRoutes() {
  const navigate = useNavigate();

  const [employees, setEmployees] = useState(initialEmployees);
  useEffect(() => {
    const loadEmployees = async () => {
      try {
        const data = await getEmployees();

        const formattedEmployees = data.map(employee => ({
          id: employee.employeeNumber,
          name: `${employee.firstName} ${employee.lastName}`,
          dept: employee.department,
          status: employee.isActive ? "Clocked Out" : "Absent",
          time: employee.createdAt
            ? new Date(employee.createdAt).toLocaleTimeString()
            : "-"
        }));

        setEmployees(formattedEmployees);
      } catch (error) {
        console.error("Failed to load employees:", error);
      }
    };

    loadEmployees();
  }, []);

  const [pendingAdminRequest, setPendingAdminRequest] = useState(null);

  return (
    <Routes>


      {/* Default page */}
      <Route
        path="/"
        element={<Navigate to="/kiosk" replace />}
      />
      {/* Existing dashboard */}
      <Route
        path="/dashboard"
        element={
          <Dashboard
            employees={employees}
            pendingAdminRequest={pendingAdminRequest}

            onAdminRequest={setPendingAdminRequest}

            onClearAdminRequest={() =>
              setPendingAdminRequest(null)
            }

            onEmployeesChange={setEmployees}

            onEditEmployee={(employee) =>
              navigate("/onboard", {
                state: {
                  mode: "edit",
                  employee
                }
              })
            }

            onLogout={() => {
              localStorage.removeItem("token");
              localStorage.removeItem("role");
              navigate("/");
            }}

            onOnboard={() =>
              navigate("/onboard", {
                state: {
                  mode: "create"
                }
              })
            }
          />
        }
      />

      {/* Admin */}
      <Route
        path="/admin"
        element={
          <Dashboard
            employees={employees}
            pendingAdminRequest={pendingAdminRequest}

            onAdminRequest={setPendingAdminRequest}

            onClearAdminRequest={() =>
              setPendingAdminRequest(null)
            }

            onEmployeesChange={setEmployees}

            onEditEmployee={(employee) =>
              navigate("/onboard", {
                state: {
                  mode: "edit",
                  employee
                }
              })
            }

            onLogout={() => {
              localStorage.removeItem("token");
              localStorage.removeItem("role");
              navigate("/");
            }}

            onOnboard={() =>
              navigate("/onboard", {
                state: {
                  mode: "create"
                }
              })
            }
          />
        }
      />

      {/* Employee onboarding + facial capture */}
      <Route
        path="/onboard"
        element={
          <OnboardPage
            mode="create"
            onBack={() => navigate("/dashboard")}
          />
        }
      />

      {/* HR */}
      <Route
        path="/hr"
        element={
          <HrDashboard
            onLogout={() => {
              localStorage.removeItem("token");
              localStorage.removeItem("role");
              navigate("/");
            }}
          />
        }
      />

      {/* Kiosk */}
      <Route
        path="/kiosk"
        element={
          <KioskPage
            employees={employees}

            onAdminAccess={(role) => {
              if (role === "hr") {
                navigate("/hr");
              } else {
                navigate("/admin");
              }
            }}

            onAdminCall={(employeeNumber, overrideRequestId, requestedClockType) => {
              setPendingAdminRequest({
                employeeNumber,
                overrideRequestId,
                requestedClockType
              });

              console.log(
                "Admin request for:",
                employeeNumber
              );
            }}
          />
        }
      />

    </Routes>
  );
}

export default AppRoutes;
