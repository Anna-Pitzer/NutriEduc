import { Navigate, Outlet } from "react-router-dom"
import { useAuth } from "../context/authContext"

export function ProtectedRoutes() {
  const { isAuthenticated } = useAuth()

  if (!isAuthenticated){
    return <Navigate to={'/login'} replace/>
  }
  return <Outlet/>
}