import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export interface Unidade {
    id: number;
    nome: string;
    ativa: boolean;
}

/**
 * Unidades da rede. Usado pela tela de cardápios para escolher a unidade e pela leitura
 * pública, que precisa de `unidadeId` (Spec 012, RF9).
 */
export async function listarUnidades(): Promise<Unidade[]> {
    const resposta = await fetch(`${ApiURL}/api/unidades`, { credentials: "include" });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao carregar as unidades"),
            resposta.status,
        );
    }

    return resposta.json();
}

export interface UnidadeAtiva {
    id: number;
    nome: string;
}

/**
 * Consulta pública mínima de unidades ativas para o cadastro (Spec 033): anônima, apenas
 * id e nome.
 */
export async function listarUnidadesAtivas(): Promise<UnidadeAtiva[]> {
    const resposta = await fetch(`${ApiURL}/api/unidades/ativas`);

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao carregar as unidades"),
            resposta.status,
        );
    }

    return resposta.json();
}
