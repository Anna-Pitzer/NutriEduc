import { ErroRequisicao, extrairFalha } from "./erroApi";

const ApiURL = "";

export type PapelUsuario =
    | "Nutricionista"
    | "AdminSecretaria"
    | "AdminEscola"
    | "Merendeira"
    | "Responsavel";

export type CategoriaRestricao = "Alergia" | "Restricao";

/** Indicadores da unidade da sessão (Spec 025, RF1). */
export interface PainelSecretaria {
    unidade: string;
    alunos: number;
    alunosPorEscola: { escolaId: number | null; escola: string; total: number }[];
    usuariosAtivosPorPapel: { papel: PapelUsuario; total: number }[];
    cardapiosPublicadosUltimosSeteDias: number;
    ocorrenciasAbertas: number;
    solicitacoesPendentes: number;
}

export interface ItemDoDia {
    alimento: string;
    gramas: number;
    alunosComRestricao: number;
}

export interface RefeicaoDoDia {
    nome: string;
    itens: ItemDoDia[];
}

export interface CardapioDoDia {
    nome: string;
    publicadoEm: string | null;
    refeicoes: RefeicaoDoDia[];
}

/** Cardápio publicado do dia da unidade (Spec 025, RF3/RF4). */
export interface PainelMerendeira {
    data: string;
    unidade: string;
    cardapio: CardapioDoDia | null;
    motivo: string | null;
}

export interface RestricaoDoFilho {
    nome: string;
    categoria: CategoriaRestricao;
}

/** Filho vinculado com restrições e o cardápio do dia (Spec 025, RF5). */
export interface FilhoDoPainel {
    id: number;
    nome: string;
    escola: string | null;
    unidade: string | null;
    restricoes: RestricaoDoFilho[];
    requerAdaptacao: boolean;
    cardapio: CardapioDoDia | null;
    motivo: string | null;
}

export interface PainelFamilia {
    data: string;
    filhos: FilhoDoPainel[];
}

export type TipoEventoAluno =
    | "Criado"
    | "Alterado"
    | "RestricaoAdicionada"
    | "RestricaoRemovida";

export type TipoOcorrencia = "Aluno" | "Refeicao" | "Alimento" | "Outro";

export type StatusOcorrencia =
    | "Aberta"
    | "EmAnalise"
    | "Resolvida"
    | "Arquivada"
    | "Cancelada";

/** Relatório do próprio filho (Spec 025, RF6). */
export interface RelatorioDoFilho {
    alunoId: number;
    alunoNome: string;
    de: string;
    ate: string;
    restricoes: RestricaoDoFilho[];
    alteracoes: {
        tipo: TipoEventoAluno;
        restricao: string;
        autor: string;
        origem: string | null;
        criadoEm: string;
    }[];
    ocorrencias: {
        id: number;
        tipo: TipoOcorrencia;
        status: StatusOcorrencia;
        descricao: string;
        criadaEm: string;
    }[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    const falha = await extrairFalha(resposta, padrao);
    throw new ErroRequisicao(falha.mensagem, resposta.status, falha.detalhes);
}

function comData(caminho: string, data?: string): string {
    return data ? `${caminho}?data=${encodeURIComponent(data)}` : caminho;
}

export async function buscarPainelSecretaria(): Promise<PainelSecretaria> {
    const resposta = await fetch(`${ApiURL}/api/painel/secretaria`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o painel da unidade");

    return resposta.json();
}

export async function buscarPainelMerendeira(data?: string): Promise<PainelMerendeira> {
    const resposta = await fetch(
        comData(`${ApiURL}/api/painel/merendeira`, data),
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o cardápio do dia");

    return resposta.json();
}

export async function buscarPainelFamilia(data?: string): Promise<PainelFamilia> {
    const resposta = await fetch(
        comData(`${ApiURL}/api/painel/familia`, data),
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar a visão da família");

    return resposta.json();
}

export async function buscarRelatorioDoFilho(
    alunoId: number,
    filtro: { de?: string; ate?: string } = {},
): Promise<RelatorioDoFilho> {
    const parametros = new URLSearchParams();

    if (filtro.de) parametros.set("de", filtro.de);
    if (filtro.ate) parametros.set("ate", filtro.ate);

    const consulta = parametros.toString();

    const resposta = await fetch(
        `${ApiURL}/api/painel/familia/${alunoId}/relatorio${consulta ? `?${consulta}` : ""}`,
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o relatório do filho");

    return resposta.json();
}
