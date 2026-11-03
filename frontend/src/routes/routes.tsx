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
import { ListarCardapios } from "../pages/nutricao/cardapios/ListarCardapios"
import { FormularioCardapio } from "../pages/nutricao/cardapios/FormularioCardapio"
import { DetalheCardapio } from "../pages/nutricao/cardapios/DetalheCardapio"
import { ParametrosGeracao } from "../pages/nutricao/cardapios/ParametrosGeracao"
import { ListarAlimentos } from "../pages/nutricao/alimentos/ListarAlimentos"
import { FormularioAlimento } from "../pages/nutricao/alimentos/FormularioAlimento"

// Merendeiras
import { HomeMerendeiras } from "../pages/merendeiras/HomeMerendeiras"
import { AlteracoesCardapios } from "../pages/merendeiras/AlteracoesCardapios"

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
        </Route>

        {/* Cardápios — nutrição escreve, merenda lê a própria unidade (Spec 012, RF8) */}
        <Route
          element={
            <ProtegidaPorPapel papeis={["Nutricionista", "AdminSecretaria", "Merendeira"]} />
          }
        >
          <Route path="/cardapios" element={<ListarCardapios />} />
          <Route path="/cardapios/:id" element={<DetalheCardapio />} />
        </Route>

        {/* Feed de alterações — nutrição vê a rede, merenda vê a própria unidade (Spec 015, RF11) */}
        <Route element={<ProtegidaPorPapel papeis={["Nutricionista", "Merendeira"]} />}>
          <Route path="/cardapios/alteracoes" element={<AlteracoesCardapios />} />
        </Route>

        {/* Escrita de cardápio — exclusiva da nutrição (Spec 012, RF8) */}
        <Route element={<ProtegidaPorPapel papeis={["Nutricionista"]} />}>
          <Route path="/cardapios/novo" element={<FormularioCardapio />} />
          <Route path="/cardapios/:id/editar" element={<FormularioCardapio />} />
          <Route path="/cardapios/parametros-geracao" element={<ParametrosGeracao />} />
        </Route>

        {/* Catálogo de alimentos — nutrição escreve, merenda lê (Spec 011, RF9) */}
        <Route element={<ProtegidaPorPapel papeis={["Nutricionista", "Merendeira"]} />}>
          <Route path="/alimentos" element={<ListarAlimentos />} />
          <Route path="/alimentos/novo" element={<FormularioAlimento />} />
          <Route path="/alimentos/editar/:id" element={<FormularioAlimento />} />
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
