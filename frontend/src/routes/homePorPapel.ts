import type { PapelUsuario } from "../context/authContext";

/**
 * Painel inicial de cada papel. Fonte única para o login e para a guarda de rota:
 * depois de autenticar, o usuário cai na área que realmente usa.
 */
export const homePorPapel: Record<PapelUsuario, string> = {
  Nutricionista: "/",
  AdminSecretaria: "/secretaria",
  AdminEscola: "/escolas",
  Merendeira: "/merendeiras",
  Responsavel: "/pais",
};
