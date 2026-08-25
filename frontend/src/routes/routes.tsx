import { Routes, Route } from "react-router-dom"
import { LoginPage } from "../pages/login"
import { HomePage } from "../pages/home"
import { ProtectedRoutes } from "./protectedRoutes"
import { PaletaDeCor } from "../pages/colorsTESTE"

export function Rotas() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/colors" element={<PaletaDeCor />} />

      <Route element={<ProtectedRoutes />}>
        <Route path="/" element={<HomePage />} />

      </Route>
    </Routes>
  )
}