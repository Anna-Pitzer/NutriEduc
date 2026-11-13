import { ErroRequisicao, extrairErro } from "./erroApi";

const ApiURL = "";

export type TipoOcorrencia = "Aluno" | "Refeicao" | "Alimento" | "Outro";

export type StatusOcorrencia = "Aberta" | "EmAnalise" | "Resolvida" | "Arquivada" | "Cancelada";

export interface OcorrenciaResumo {
    id: number;
    tipo: TipoOcorrencia;
    status: StatusOcorrencia;
    unidadeId: number;
    escolaId: number | null;
    alunoId: number | null;
    aluno: string | null;
    cardapioId: number | null;
    cardapio: string | null;
    refeicaoId: number | null;
    refeicao: string | null;
    alimentoId: number | null;
    alimento: string | null;
    descricao: string;
    autor: string;
    criadoEm: string;
}

export interface HistoricoOcorrencia {
    status: StatusOcorrencia;
    autor: string;
    observacao: string;
    criadoEm: string;
}

export interface OcorrenciaDetalhe extends OcorrenciaResumo {
    historico: HistoricoOcorrencia[];
    podeCancelar: boolean;
}

export interface CriarOcorrenciaEntrada {
    tipo: TipoOcorrencia;
    descricao: string;
    alunoId?: number;
    cardapioId?: number;
    refeicaoId?: number;
    alimentoId?: number;
}

export interface FiltrosOcorrencia {
    tipo?: TipoOcorrencia | "";
    status?: StatusOcorrencia | "";
    alunoId?: number;
    de?: string;
    ate?: string;
    pagina?: number;
    tamanho?: number;
}

interface PaginaOcorrencias {
    pagina: number;
    tamanho: number;
    total: number;
    itens: OcorrenciaResumo[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

function comoParametros(filtros: FiltrosOcorrencia): string {
    const parametros = new URLSearchParams();

    if (filtros.tipo) parametros.set("tipo", filtros.tipo);
    if (filtros.status) parametros.set("status", filtros.status);
    if (filtros.alunoId) parametros.set("alunoId", String(filtros.alunoId));
    if (filtros.de) parametros.set("de", filtros.de);
    if (filtros.ate) parametros.set("ate", filtros.ate);
    parametros.set("pagina", String(filtros.pagina ?? 1));
    parametros.set("tamanho", String(filtros.tamanho ?? 100));

    return `?${parametros.toString()}`;
}

export async function listarOcorrencias(
    filtros: FiltrosOcorrencia = {},
): Promise<PaginaOcorrencias> {
    const resposta = await fetch(`${ApiURL}/api/ocorrencias${comoParametros(filtros)}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as ocorrências");

    return resposta.json();
}

export async function obterOcorrencia(id: number): Promise<OcorrenciaDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/ocorrencias/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar a ocorrência");

    return resposta.json();
}

export async function criarOcorrencia(
    entrada: CriarOcorrenciaEntrada,
): Promise<OcorrenciaDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/ocorrencias`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao criar a ocorrência");

    return resposta.json();
}

export async function mudarStatusOcorrencia(
    id: number,
    status: StatusOcorrencia,
    observacao: string,
): Promise<OcorrenciaDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/ocorrencias/${id}/status`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ status, observacao }),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao mudar o status da ocorrência");

    return resposta.json();
}

export async function cancelarOcorrencia(id: number): Promise<OcorrenciaDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/ocorrencias/${id}/cancelar`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao cancelar a ocorrência");

    return resposta.json();
}
