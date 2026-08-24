import { Routes, Route } from "react-router-dom"
import { LoginPage } from "../pages/login"
import { HomePage } from "../pages/home"
import { ProtectedRoutes } from "./protectedRoutes"

export function Rotas() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoutes />}>
        <Route path="/" element={<HomePage />} />
      </Route>
    </Routes>
  )
}