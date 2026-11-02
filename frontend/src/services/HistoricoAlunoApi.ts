import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export type TipoEventoAluno =
    | "Criado"
    | "Alterado"
    | "RestricaoAdicionada"
    | "RestricaoRemovida";

export interface EventoAluno {
    tipo: TipoEventoAluno;
    campo: string | null;
    valorAnterior: string | null;
    valorNovo: string | null;
    autor: string;
    criadoEm: string;
}

export interface PaginaHistorico {
    pagina: number;
    tamanho: number;
    total: number;
    itens: EventoAluno[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

/**
 * Trilha completa de alterações do aluno (Spec 023, RF8): criação do cadastro, cada campo
 * alterado com valor anterior e novo, e as restrições adicionadas/removidas.
 */
export async function buscarHistoricoAluno(
    alunoId: number,
    pagina = 1,
    tamanho = 20,
): Promise<PaginaHistorico> {
    const parametros = new URLSearchParams({
        pagina: String(pagina),
        tamanho: String(tamanho),
    });

    const resposta = await fetch(
        `${ApiURL}/api/aluno/${alunoId}/historico?${parametros.toString()}`,
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao buscar histórico do aluno");

    return resposta.json();
}

/** Rótulo curto do evento, usado na linha do tempo. */
export function descreverEvento(evento: EventoAluno): string {
    switch (evento.tipo) {
        case "Criado":
            return "Cadastro criado";

        case "Alterado":
            return `${evento.campo ?? "Campo"} alterado`;

        case "RestricaoAdicionada":
            return `Restrição adicionada: ${evento.valorNovo ?? ""}`;

        case "RestricaoRemovida":
            return `Restrição removida: ${evento.valorAnterior ?? ""}`;

        default:
            return "Alteração registrada";
    }
}
