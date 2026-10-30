import { Routes, Route } from "react-router-dom"
import { ProtectedRoutes } from "./protectedRoutes"

// Comum — telas compartilhadas por todas as áreas
import { LoginPage } from "../pages/comum/login"
import { Perfil } from "../pages/comum/Perfil"
import { TermosDeUso } from "../pages/comum/TermosDeUso"
import { PoliticaDePrivaciade } from "../pages/comum/PoliticaDePrivacidade"
import { Suporte } from "../pages/comum/Suporte"

// Nutrição
import { HomeNutricao } from "../pages/nutricao/HomeNutricao"
import { CadastroAlunos } from "../pages/nutricao/alunos/CadastrarAlunos"
import { VisualizarAlunos } from "../pages/nutricao/alunos/VisualizarAlunos"
import { Cardapio } from "../pages/nutricao/cardapios/Cardapio"
import { MontarCardapio } from "../pages/nutricao/cardapios/MontarCardapio"

// Merendeiras
import { HomeMerendeiras } from "../pages/merendeiras/HomeMerendeiras"

// Pais / Responsáveis
import { HomePais } from "../pages/pais/HomePais"

// Secretaria
import { HomeSecretaria } from "../pages/secretaria/HomeSecretaria"

// Administração Escolar
import { HomeEscolas } from "../pages/escolas/HomeEscolas"

export function Rotas() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoutes />}>
        {/* Nutrição — área principal do sistema hoje */}
        <Route path="/" element={<HomeNutricao />} />
        <Route path="/aluno/registrar" element={<CadastroAlunos />} />
        <Route path="/aluno/vizualizar" element={<VisualizarAlunos />} />
        <Route path="/alunos/editar/:id" element={<CadastroAlunos />} />
        <Route path="/cardapio" element={<Cardapio />} />
        <Route path="/cardapio/montar/:refeicao" element={<MontarCardapio />} />

        {/* Demais áreas — homes vazias, conteúdo entra em etapa posterior */}
        <Route path="/merendeiras" element={<HomeMerendeiras />} />
        <Route path="/pais" element={<HomePais />} />
        <Route path="/secretaria" element={<HomeSecretaria />} />
        <Route path="/escolas" element={<HomeEscolas />} />

        {/* Comum */}
        <Route path="/perfil" element={<Perfil />} />
        <Route path="/termosdeuso" element={<TermosDeUso />} />
        <Route path="/politicadeprivacidade" element={<PoliticaDePrivaciade />} />
        <Route path="/suporte" element={<Suporte />} />
      </Route>
    </Routes>
  )
}
