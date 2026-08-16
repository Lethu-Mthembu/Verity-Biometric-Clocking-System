import { useState, useEffect } from "react";
import { Routes, Route, Navigate, useLocation, useNavigate } from "react-router-dom";
import { getEmployees } from "../services/employeeService";

import Dashboard from "../features/dashboard/pages/Dashboard";
import HrDashboard from "../features/hr/pages/HrDashboard";
import KioskPage from "../features/kiosk/pages/KioskPage";
import OnboardPage from "../features/onboarding/pages/OnboardPage";
import ChangePasswordPage from "../features/hr/pages/ChangePasswordPage";
import ProtectedRoute from "./ProtectedRoute";


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
    const isAdmin = String(localStorage.getItem("role") || "").toLowerCase() === "admin";
    if (!isAdmin) {
      setEmployees([]);
      return undefined;
    }

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
  }, [location.pathname]);

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
          <ProtectedRoute allowedRoles={["Admin"]}>
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
          </ProtectedRoute>
        }
      />

      {/* Admin */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute allowedRoles={["Admin"]}>
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
          </ProtectedRoute>
        }
      />

      {/* Employee onboarding + facial capture */}
      <Route
        path="/onboard"
        element={
          <ProtectedRoute allowedRoles={["Admin"]}>
            <OnboardPage
              mode={location.state?.mode || "create"}
              employee={location.state?.employee}
              onSaved={refreshEmployees}
              onBack={() => navigate(location.state?.returnPath || "/admin", { replace: true })}
            />
          </ProtectedRoute>
        }
      />

      {/* HR */}
      <Route
        path="/hr"
        element={
          <ProtectedRoute allowedRoles={["HR"]}>
            <HrDashboard
              onChangePassword={() => navigate("/hr/change-password")}
              onLogout={() => {
                localStorage.removeItem("token");
                localStorage.removeItem("role");
                navigate("/");
              }}
            />
          </ProtectedRoute>
        }
      />

      <Route
        path="/hr/change-password"
        element={
          <ProtectedRoute allowedRoles={["HR"]}>
            <ChangePasswordPage />
          </ProtectedRoute>
        }
      />

      {/* Kiosk */}
      <Route
        path="/kiosk"
        element={
          <KioskPage
            onAdminAccess={(role, mustChangePassword) => {
              if (role === "hr") {
                navigate(mustChangePassword ? "/hr/change-password" : "/hr");
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
