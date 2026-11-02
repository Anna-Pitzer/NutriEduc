import { extrairErro, ErroRequisicao } from "./erroApi";

export interface NovoAdminBootstrap {
    token: string;
    /** Nome exato da unidade. Obrigatório só para o token de ambiente. */
    unidade?: string;
    nome: string;
    cpf: string;
    dataNascimento: string;
    email: string;
    senha: string;
}

export interface StatusBootstrap {
    disponivel: boolean;
    /** `ok` | `sem-token` | `desativado-ambiente` */
    motivo: string;
}

/** Token de 30 minutos emitido pelo painel. O valor aparece uma única vez. */
export interface TokenGerado {
    token: string;
    expiraEm: string;
}

export interface TokenAtual {
    ativo: boolean;
    expiraEm: string | null;
}

async function ler<T>(resposta: Response, padrao: string): Promise<T> {
    if (!resposta.ok) {
        throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
    }

    return resposta.json();
}

/** Informa apenas se a rota está habilitada; não revela token. */
export async function consultarBootstrap(): Promise<StatusBootstrap> {
    const resposta = await fetch("/api/bootstrap/status", { credentials: "include" });

    return ler(resposta, "Não foi possível consultar o bootstrap.");
}

export async function criarAdminBootstrap(dados: NovoAdminBootstrap): Promise<{ id: number }> {
    const resposta = await fetch("/api/bootstrap/admin", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    return ler(resposta, "Não foi possível criar o administrador.");
}

/** Emite o token de 30 minutos da unidade de quem está logado. */
export async function gerarTokenCadastro(): Promise<TokenGerado> {
    const resposta = await fetch("/api/bootstrap/token", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: "{}",
    });

    return ler(resposta, "Não foi possível gerar o token de cadastro.");
}

/** Estado do token da unidade, para desabilitar o botão e mostrar a contagem. */
export async function consultarTokenCadastro(): Promise<TokenAtual> {
    const resposta = await fetch("/api/bootstrap/token", { credentials: "include" });

    return ler(resposta, "Não foi possível consultar o token de cadastro.");
}
