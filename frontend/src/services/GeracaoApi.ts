import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

/** Parâmetros de geração por unidade (Spec 013, RF2). */
export interface ParametroGeracao {
    id: number;
    unidadeId: number;
    caloriasMinimasRefeicao: number;
    caloriasMaximasRefeicao: number;
    opcoesPorRefeicao: number;
    janelaRepeticaoDias: number;
    excluirAlimentosIncompativeis: boolean;
    categoriasObrigatorias: number[];
    refeicoesPadrao: string[];
}

/** Entrada de criação/edição dos parâmetros de geração. */
export interface SalvarParametroGeracao {
    caloriasMinimasRefeicao: number;
    caloriasMaximasRefeicao: number;
    opcoesPorRefeicao: number;
    janelaRepeticaoDias: number;
    excluirAlimentosIncompativeis: boolean;
    categoriasObrigatorias: number[];
    refeicoesPadrao: string[];
}

export interface TotalRefeicaoGerada {
    refeicao: string;
    caloriasKcal: number;
}

export interface TentativaRefeicaoGerada {
    refeicao: string;
    tentativas: number;
}

export interface VersaoAdaptadaGerada {
    id: number;
    escolaId: number;
    conjunto: string;
    status: string;
    motivo?: string | null;
}

export interface DiagnosticoRefeicao {
    refeicao: string;
    tentativas: number;
    faixaMinimaAlcancavel: number;
    faixaMaximaAlcancavel: number;
    categoriaAusente: number | null;
    candidatosDisponiveis: number;
}

/** Resposta de geração (Spec 032, RF12): geral + adaptados, seed, tentativas, totais e diagnóstico. */
export interface GeracaoCardapio {
    cardapioGeralId: number;
    seed: number;
    tentativasPorRefeicao: TentativaRefeicaoGerada[];
    totaisPorRefeicao: TotalRefeicaoGerada[];
    versaoAdaptadaIds: VersaoAdaptadaGerada[];
    avisos: string[];
    diagnostico: DiagnosticoRefeicao[];
}

export interface GerarCardapioEntrada {
    unidadeId: number;
    /** Nutricionista que elaborou o cardápio; obrigatória na geração. */
    nutricionistaId: number;
    nome: string;
    periodo: { inicio: string; fim: string };
    seed?: number;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function obterParametrosGeracao(unidadeId: number): Promise<ParametroGeracao> {
    const resposta = await fetch(`${ApiURL}/api/unidades/${unidadeId}/parametros-geracao`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os parâmetros de geração");

    return resposta.json();
}

export async function salvarParametrosGeracao(
    unidadeId: number,
    entrada: SalvarParametroGeracao,
): Promise<ParametroGeracao> {
    const resposta = await fetch(`${ApiURL}/api/unidades/${unidadeId}/parametros-geracao`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar os parâmetros de geração");

    return resposta.json();
}

export async function gerarCardapio(entrada: GerarCardapioEntrada): Promise<GeracaoCardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/gerar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao gerar o cardápio");

    return resposta.json();
}
