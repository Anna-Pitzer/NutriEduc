import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/authContext";

export function ProtectedRoutes() {
    const { isAuthenticated } = useAuth();

    const BYPASS_LOGIN = true;

    if (BYPASS_LOGIN) {
        return <Outlet />;
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    return <Outlet />;
}