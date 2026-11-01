import { Routes, Route } from "react-router-dom"
import { ProtectedRoutes } from "./protectedRoutes"
import { ProtegidaPorPapel } from "./protegidaPorPapel"

// Comum — telas compartilhadas por todas as áreas
import { LoginPage } from "../pages/comum/login"
import { BootstrapAdmin } from "../pages/comum/BootstrapAdmin"
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
import { Escolas } from "../pages/secretaria/Escolas"
import { Unidades } from "../pages/secretaria/Unidades"
import { Usuarios } from "../pages/secretaria/Usuarios"

// Administração Escolar
import { HomeEscolas } from "../pages/escolas/HomeEscolas"

export function Rotas() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Rota de configuração: link + token, fora do ProtectedRoutes (Spec 022). */}
      <Route path="/bootstrap" element={<BootstrapAdmin />} />

      <Route element={<ProtectedRoutes />}>
        {/* Nutrição — áreas de gestão nutricional */}
        <Route element={<ProtegidaPorPapel papeis={["Nutricionista", "AdminSecretaria"]} />}>
          <Route path="/" element={<HomeNutricao />} />
          <Route path="/aluno/registrar" element={<CadastroAlunos />} />
          <Route path="/aluno/vizualizar" element={<VisualizarAlunos />} />
          <Route path="/alunos/editar/:id" element={<CadastroAlunos />} />
          <Route path="/cardapio" element={<Cardapio />} />
          <Route path="/cardapio/montar/:refeicao" element={<MontarCardapio />} />
        </Route>

        {/* Demais áreas — guarda por papel; conteúdo entra em etapa posterior */}
        <Route element={<ProtegidaPorPapel papeis={["Merendeira"]} />}>
          <Route path="/merendeiras" element={<HomeMerendeiras />} />
        </Route>

        <Route element={<ProtegidaPorPapel papeis={["Responsavel"]} />}>
          <Route path="/pais" element={<HomePais />} />
        </Route>

        <Route element={<ProtegidaPorPapel papeis={["AdminSecretaria"]} />}>
          <Route path="/secretaria" element={<HomeSecretaria />} />
          <Route path="/secretaria/escolas" element={<Escolas />} />
          <Route path="/secretaria/unidades" element={<Unidades />} />
          <Route path="/secretaria/usuarios" element={<Usuarios />} />
        </Route>

        <Route element={<ProtegidaPorPapel papeis={["AdminEscola"]} />}>
          <Route path="/escolas" element={<HomeEscolas />} />
        </Route>

        {/* Comum */}
        <Route path="/perfil" element={<Perfil />} />
        <Route path="/termosdeuso" element={<TermosDeUso />} />
        <Route path="/politicadeprivacidade" element={<PoliticaDePrivaciade />} />
        <Route path="/suporte" element={<Suporte />} />
      </Route>
    </Routes>
  )
}
