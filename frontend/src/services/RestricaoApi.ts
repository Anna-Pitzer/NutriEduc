import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export type CategoriaRestricao = "Alergia" | "Restricao";

/**
 * Item do catálogo de restrições e alergias (Spec 010). O catálogo vem semeado pela
 * migration e não é editável pela interface (ADR-015): a tela de alimento apenas escolhe
 * entre as opções ativas.
 */
export interface Restricao {
    id: number;
    nome: string;
    categoria: CategoriaRestricao;
    ativa: boolean;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function listarRestricoes(incluirInativas = false): Promise<Restricao[]> {
    const consulta = incluirInativas ? "?incluirInativas=true" : "";

    const resposta = await fetch(`${ApiURL}/api/restricoes${consulta}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as restrições");

    return resposta.json();
}
