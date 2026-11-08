import { extrairErro, ErroRequisicao } from "./erroApi";
import type { PapelUsuario } from "../context/authContext";

export interface UsuarioResposta {
    id: number;
    nome: string | null;
    email: string | null;
    telefone: string | null;
    foto: string | null;
    papel: PapelUsuario | null;
    escolaId: number | null;
    unidadeId: number | null;
    ativo: boolean;
    /** Registro no conselho de nutrição; nulo quando não informado. */
    crn: string | null;
    /** `true` quando o CRN está em formato válido (Spec 031). */
    crnValido: boolean;
}

export interface FiltroUsuarios {
    papel?: PapelUsuario | "";
    escolaId?: number | "";
    busca?: string;
    /** `undefined` = todas as situações. */
    ativo?: boolean;
}

export interface NovoUsuario {
    nome: string;
    cpf: string;
    dataNascimento: string;
    email: string;
    senha: string;
    papel: PapelUsuario;
    escolaId: number | null;
    /** Registro no conselho; usado na autoria do cardápio. */
    crn: string | null;
}

export interface AtualizacaoUsuario {
    nome: string;
    email: string;
    telefone: string | null;
    papel: PapelUsuario;
    escolaId: number | null;
    /** Unidade de atribuição: o domínio só aceita para `AdminSecretaria`. */
    unidadeId: number | null;
    crn: string | null;
}

async function ler<T>(resposta: Response, padrao: string): Promise<T> {
    if (!resposta.ok) {
        throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
    }

    return resposta.json();
}

export async function listarUsuarios(filtro: FiltroUsuarios = {}): Promise<UsuarioResposta[]> {
    const parametros = new URLSearchParams();

    if (filtro.papel) {
        parametros.set("papel", filtro.papel);
    }

    if (filtro.escolaId) {
        parametros.set("escolaId", String(filtro.escolaId));
    }

    if (filtro.busca?.trim()) {
        parametros.set("busca", filtro.busca.trim());
    }

    if (filtro.ativo !== undefined) {
        parametros.set("ativo", String(filtro.ativo));
    }

    const consulta = parametros.toString();
    const resposta = await fetch(`/api/usuarios${consulta ? `?${consulta}` : ""}`, {
        credentials: "include",
    });

    return ler(resposta, "Não foi possível carregar os usuários.");
}

export async function criarUsuario(dados: NovoUsuario): Promise<{ id: number }> {
    const resposta = await fetch("/api/usuarios", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    return ler(resposta, "Não foi possível criar o usuário.");
}

export async function atualizarUsuario(id: number, dados: AtualizacaoUsuario): Promise<UsuarioResposta> {
    const resposta = await fetch(`/api/usuarios/${id}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    return ler(resposta, "Não foi possível atualizar o usuário.");
}

export async function definirUsuarioAtivo(id: number, ativo: boolean): Promise<UsuarioResposta> {
    const acao = ativo ? "ativar" : "desativar";
    const resposta = await fetch(`/api/usuarios/${id}/${acao}`, {
        method: "POST",
        credentials: "include",
    });

    return ler(resposta, `Não foi possível ${acao} o usuário.`);
}

/** Exclusão definitiva: o backend responde `409` para conta própria, último admin ou com vínculo. */
export async function excluirUsuario(id: number): Promise<void> {
    const resposta = await fetch(`/api/usuarios/${id}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Não foi possível excluir o usuário."),
            resposta.status);
    }
}

// Regras espelhadas de `Usuario.DefinirPapel` (Spec 009): o formulário não oferece um
// campo que o domínio vai recusar.
export const exigeEscola = (papel: PapelUsuario) =>
    papel === "AdminEscola" || papel === "Merendeira" || papel === "Responsavel";

export const rotuloPapel: Record<PapelUsuario, string> = {
    Nutricionista: "Nutricionista",
    Merendeira: "Merendeira",
    Responsavel: "Responsável",
    AdminSecretaria: "Admin. Secretaria",
    AdminEscola: "Admin. Escola",
};

export const papeisDisponiveis: PapelUsuario[] = [
    "Nutricionista",
    "Merendeira",
    "Responsavel",
    "AdminEscola",
    "AdminSecretaria",
];
