import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/authContext";

export function ProtectedRoutes() {
  const { isAuthenticated, loading } = useAuth();
  if (loading){
    return <p role="status">Carregando...</p>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}