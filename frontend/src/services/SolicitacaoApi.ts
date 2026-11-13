import { ErroRequisicao, extrairErro } from "./erroApi";

const ApiURL = "";

export type TipoSolicitacao =
    | "MudancaCardapio"
    | "Ingrediente"
    | "FaltaIngrediente"
    | "InclusaoAlergia"
    | "AlteracaoRestricao"
    | "DadosCadastrais";

export type StatusSolicitacao =
    | "Aberta"
    | "EmAnalise"
    | "Aprovada"
    | "Recusada"
    | "Concluida"
    | "Cancelada";

export interface SolicitacaoResumo {
    id: number;
    tipo: TipoSolicitacao;
    status: StatusSolicitacao;
    unidadeId: number;
    escolaId: number | null;
    alunoId: number | null;
    aluno: string | null;
    cardapioId: number | null;
    cardapio: string | null;
    restricaoNome: string | null;
    ingrediente: string | null;
    descricao: string;
    solicitanteId: number;
    solicitante: string;
    resposta: string | null;
    respondidoPor: string | null;
    criadoEm: string;
}

export interface HistoricoSolicitacao {
    status: StatusSolicitacao;
    autor: string;
    resposta: string | null;
    criadoEm: string;
}

export interface SolicitacaoDetalhe extends SolicitacaoResumo {
    historico: HistoricoSolicitacao[];
    alteracoes: AlteracaoDeDados[];
    aviso?: string | null;
    /** Solicitante informou reação anafilática para a restrição. */
    anafilaxia: boolean;
}

export interface AlteracaoDeDados {
    campo: string;
    valorSolicitado: string;
    valorNoPedido: string;
}

export interface CriarSolicitacaoEntrada {
    tipo: TipoSolicitacao;
    descricao: string;
    alunoId?: number;
    cardapioId?: number;
    restricaoNome?: string;
    ingrediente?: string;
    justificativa?: string;
    alteracoes?: { campo: string; valorSolicitado: string }[];
    /** InclusaoAlergia/AlteracaoRestricao: reação anafilática informada pelo solicitante. */
    anafilaxia?: boolean;
}

export interface FiltrosSolicitacao {
    tipo?: TipoSolicitacao | "";
    status?: StatusSolicitacao | "";
    alunoId?: number;
    cardapioId?: number;
    pagina?: number;
    tamanho?: number;
}

interface PaginaSolicitacoes {
    pagina: number;
    tamanho: number;
    total: number;
    itens: SolicitacaoResumo[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

function comoParametros(filtros: FiltrosSolicitacao): string {
    const parametros = new URLSearchParams();

    if (filtros.tipo) parametros.set("tipo", filtros.tipo);
    if (filtros.status) parametros.set("status", filtros.status);
    if (filtros.alunoId) parametros.set("alunoId", String(filtros.alunoId));
    if (filtros.cardapioId) parametros.set("cardapioId", String(filtros.cardapioId));
    parametros.set("pagina", String(filtros.pagina ?? 1));
    parametros.set("tamanho", String(filtros.tamanho ?? 50));

    return `?${parametros.toString()}`;
}

export async function listarSolicitacoes(
    filtros: FiltrosSolicitacao = {},
): Promise<PaginaSolicitacoes> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes${comoParametros(filtros)}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as solicitações");

    return resposta.json();
}

export async function obterSolicitacao(id: number): Promise<SolicitacaoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar a solicitação");

    return resposta.json();
}

export async function criarSolicitacao(
    entrada: CriarSolicitacaoEntrada,
): Promise<SolicitacaoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao criar a solicitação");

    return resposta.json();
}

async function decidir(
    id: number,
    acao: string,
    corpo?: unknown,
): Promise<SolicitacaoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes/${id}/${acao}`, {
        method: "POST",
        headers: corpo === undefined ? undefined : { "Content-Type": "application/json" },
        credentials: "include",
        body: corpo === undefined ? undefined : JSON.stringify(corpo),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao decidir a solicitação");

    return resposta.json();
}

export const analisarSolicitacao = (id: number) => decidir(id, "analise");

export const aprovarSolicitacao = (id: number, resposta: string) =>
    decidir(id, "aprovar", { resposta });

export const recusarSolicitacao = (id: number, resposta: string) =>
    decidir(id, "recusar", { resposta });

export const concluirSolicitacao = (id: number) => decidir(id, "concluir");

export const cancelarSolicitacao = (id: number) => decidir(id, "cancelar");

/** Confirma a anafilaxia do aluno na análise de uma inclusão de alergia (Spec 017/023). */
export const marcarAnafilaxia = (id: number) => decidir(id, "anafilaxia");
