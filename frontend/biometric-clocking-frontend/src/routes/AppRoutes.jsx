import { useState, useEffect } from "react";
import { Routes, Route, Navigate, useLocation, useNavigate } from "react-router-dom";
import { getEmployees } from "../services/employeeService";

import Dashboard from "../features/dashboard/pages/Dashboard";
import HrDashboard from "../features/hr/pages/HrDashboard";
import KioskPage from "../features/kiosk/pages/KioskPage";
import OnboardPage from "../features/onboarding/pages/OnboardPage";


function AppRoutes() {
  const navigate = useNavigate();
  const location = useLocation();

  const formatEmployee = employee => ({
    id: employee.employeeNumber,
    name: `${employee.firstName} ${employee.lastName}`,
    dept: employee.department,
    status: employee.isActive ? "Clocked Out" : "Absent",
    time: employee.createdAt
      ? new Date(employee.createdAt).toLocaleTimeString()
      : "-"
  });

  const [employees, setEmployees] = useState([]);
  const refreshEmployees = async () => {
    try {
      const data = await getEmployees();
      setEmployees(data.map(formatEmployee));
    } catch (error) {
      console.error("Failed to load employees:", error);
    }
  };

  useEffect(() => {
    let cancelled = false;
    const loadEmployees = async () => {
      try {
        const data = await getEmployees();
        if (!cancelled) setEmployees(data.map(formatEmployee));
      } catch (error) {
        console.error("Failed to load employees:", error);
      }
    };

    loadEmployees();
    return () => { cancelled = true };
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
                  employee,
                  returnPath: "/dashboard"
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
                  mode: "create",
                  returnPath: "/dashboard"
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
                  employee,
                  returnPath: "/admin"
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
                  mode: "create",
                  returnPath: "/admin"
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
            mode={location.state?.mode || "create"}
            employee={location.state?.employee}
            onSaved={refreshEmployees}
            onBack={() => navigate(location.state?.returnPath || "/admin", { replace: true })}
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
