import { useEffect, useState } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { getSession } from "../services/authServices";

export default function ProtectedRoute({ allowedRoles, children }) {
  const location = useLocation();
  const [session, setSession] = useState(undefined);

  useEffect(() => {
    let cancelled = false;
    getSession()
      .then(value => {
        if (!cancelled) setSession(value);
      })
      .catch(() => {
        if (!cancelled) setSession(null);
      });
    return () => { cancelled = true; };
  }, []);

  if (session === undefined) return null;
  if (!session) return <Navigate to="/kiosk" replace state={{ from: location.pathname }} />;

  const role = String(session.role || "").toLowerCase();
  const permittedRoles = allowedRoles.map(value => String(value).toLowerCase());
  if (!permittedRoles.includes(role)) return <Navigate to="/kiosk" replace />;

  if (role === "hr" && session.mustChangePassword && location.pathname !== "/hr/change-password") {
    return <Navigate to="/hr/change-password" replace />;
  }

  return children;
}
