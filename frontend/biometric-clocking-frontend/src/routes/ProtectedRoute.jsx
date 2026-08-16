import { Navigate, useLocation } from "react-router-dom";

const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function clearAuth() {
  localStorage.removeItem("token");
  localStorage.removeItem("userId");
  localStorage.removeItem("role");
}

function readTokenPayload(token) {
  try {
    const payload = token.split(".")[1];
    if (!payload) return null;

    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - normalized.length % 4) % 4), "=");
    return JSON.parse(atob(padded));
  } catch {
    return null;
  }
}

function getTokenRole(payload) {
  const role = payload?.role ?? payload?.[ROLE_CLAIM];
  return Array.isArray(role) ? role[0] : role;
}

export default function ProtectedRoute({ allowedRoles, children }) {
  const location = useLocation();
  const token = localStorage.getItem("token");
  const payload = token ? readTokenPayload(token) : null;

  // The Axios response interceptor handles expired tokens after the first
  // protected API request. This guard handles missing or malformed tokens
  // without adding time-dependent logic to the render path.
  if (!token || !payload) {
    clearAuth();
    return <Navigate to="/kiosk" replace state={{ from: location.pathname }} />;
  }

  const role = String(getTokenRole(payload) || localStorage.getItem("role") || "").toLowerCase();
  const permittedRoles = allowedRoles.map(value => String(value).toLowerCase());

  if (!permittedRoles.includes(role)) {
    return <Navigate to="/kiosk" replace />;
  }

  // A newly created HR account can authenticate only to replace its temporary
  // password. The API enforces this too; this keeps the UI aligned with it.
  if (role === "hr" && payload?.password_change_required === "true" && location.pathname !== "/hr/change-password") {
    return <Navigate to="/hr/change-password" replace />;
  }

  return children;
}
