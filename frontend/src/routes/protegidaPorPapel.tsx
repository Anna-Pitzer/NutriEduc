import { Navigate, Outlet } from "react-router-dom";
import { useAuth, type PapelUsuario } from "../context/authContext";
import { homePorPapel } from "./homePorPapel";

/**
 * Guarda de rota por papel. É conveniência de navegação, nunca controle de acesso: o
 * backend valida a política em cada requisição (RF10), então esconder a rota aqui só
 * evita uma tela inútil.
 */
export function ProtegidaPorPapel({ papeis }: { papeis: PapelUsuario[] }) {
  const { userLogin, loading } = useAuth();

  if (loading) {
    return <p role="status">Carregando...</p>;
  }

  if (!userLogin) {
    return <Navigate to="/login" replace />;
  }

  if (!userLogin.papel || !papeis.includes(userLogin.papel)) {
    return <Navigate to={userLogin.papel ? homePorPapel[userLogin.papel] : "/login"} replace />;
  }

  return <Outlet />;
}
