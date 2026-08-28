import { Routes, Route } from "react-router-dom"
import { LoginPage } from "../pages/login"
import { HomePage } from "../pages/home"
import { ProtectedRoutes } from "./protectedRoutes"
import { PaletaDeCor } from "../pages/colorsTESTE"
import { CadastroAlunos } from "../pages/CadastrarAlunos"
import { Perfil } from "../pages/Perfil"
import { TermosDeUso } from "../pages/TermosDeUso"
import { PoliticaDePrivaciade } from "../pages/PoliticaDePrivacidade"
import { Suporte } from "../pages/Suporte"

export function Rotas() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/colors" element={<PaletaDeCor />} />

      <Route element={<ProtectedRoutes />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/cadastro" element={<CadastroAlunos />} />
        <Route path="/perfil" element={<Perfil />} />
        <Route path="/termosdeuso" element={<TermosDeUso />} />
        <Route path="/politicadeprivacidade" element={<PoliticaDePrivaciade />} />
        <Route path="/suporte" element={<Suporte />} />

      </Route>
    </Routes>
  )
}