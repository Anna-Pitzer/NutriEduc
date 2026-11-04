import { extrairErro, ErroRequisicao } from "./erroApi";

export interface EscolaResposta {
    id: number;
    nome: string;
    unidadeId: number | null;
    ativa: boolean;
}

async function ler<T>(resposta: Response, padrao: string): Promise<T> {
    if (!resposta.ok) {
        throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
    }

    return resposta.json();
}

async function enviar<T>(caminho: string, metodo: "POST" | "PUT", corpo: object, padrao: string): Promise<T> {
    const resposta = await fetch(caminho, {
        method: metodo,
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(corpo),
    });

    return ler<T>(resposta, padrao);
}

/** Exclusão definitiva: o backend responde `409` quando há referência. */
async function excluir(caminho: string, padrao: string): Promise<void> {
    const resposta = await fetch(caminho, { method: "DELETE", credentials: "include" });

    if (!resposta.ok) {
        throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
    }
}

// ------------------------------------------------------------------ escolas

export async function listarEscolas(): Promise<EscolaResposta[]> {
    const resposta = await fetch("/api/escolas", { credentials: "include" });

    return ler(resposta, "Não foi possível carregar as escolas.");
}

/** Spec 027: a escola pertence a uma unidade; o backend confere contra a sessão. */
export async function criarEscola(nome: string, unidadeId: number): Promise<EscolaResposta> {
    return enviar("/api/escolas", "POST", { nome, unidadeId }, "Não foi possível criar a escola.");
}

export async function renomearEscola(id: number, nome: string): Promise<EscolaResposta> {
    return enviar(`/api/escolas/${id}`, "PUT", { nome }, "Não foi possível renomear a escola.");
}

export async function definirEscolaAtiva(id: number, ativa: boolean): Promise<EscolaResposta> {
    const acao = ativa ? "reativar" : "desativar";

    return enviar(`/api/escolas/${id}/${acao}`, "POST", {}, `Não foi possível ${acao} a escola.`);
}

export async function excluirEscola(id: number): Promise<void> {
    return excluir(`/api/escolas/${id}`, "Não foi possível excluir a escola.");
}

// ----------------------------------------------------------------- unidades

/**
 * Unidade é da rede e nasce no bootstrap do primeiro administrador; a tela só lista,
 * renomeia, ativa/desativa e exclui.
 */
export interface UnidadeResposta {
    id: number;
    nome: string;
    ativa: boolean;
}

/** Unidades da rede: não pertencem a escola. */
export async function listarUnidades(): Promise<UnidadeResposta[]> {
    const resposta = await fetch("/api/unidades", { credentials: "include" });

    return ler(resposta, "Não foi possível carregar as unidades.");
}

export async function excluirUnidade(id: number): Promise<void> {
    return excluir(`/api/unidades/${id}`, "Não foi possível excluir a unidade.");
}

export async function renomearUnidade(id: number, nome: string): Promise<UnidadeResposta> {
    return enviar(`/api/unidades/${id}`, "PUT", { nome }, "Não foi possível renomear a unidade.");
}

export async function definirUnidadeAtiva(id: number, ativa: boolean): Promise<UnidadeResposta> {
    const acao = ativa ? "reativar" : "desativar";

    return enviar(`/api/unidades/${id}/${acao}`, "POST", {}, `Não foi possível ${acao} a unidade.`);
}
