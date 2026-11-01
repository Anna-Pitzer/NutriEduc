import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export type SituacaoCardapio = "Rascunho" | "Publicado" | "Cancelado";

/** Período de vigência (Spec 012, RF1). Datas no formato ISO `yyyy-mm-dd`. */
export interface PeriodoCardapio {
    inicio: string;
    fim: string;
}

/** Composição nutricional devolvida pelo backend; o cliente nunca recalcula (RNF3). */
export interface ValoresItem {
    caloriasKcal: number;
    proteinasG: number;
    carboidratosG: number;
    gordurasG: number;
    fibrasG: number;
    sodioMg: number;
}

export interface ItemCardapio {
    alimentoId: number;
    alimento: string;
    gramas: number;
    valores: ValoresItem;
}

export interface RefeicaoCardapio {
    nome: string;
    ordem: number;
    itens: ItemCardapio[];
    total: ValoresItem;
}

/** Cardápio completo, com refeições, itens e totais (RF11). */
export interface Cardapio {
    id: number;
    nome: string;
    unidadeId: number;
    situacao: SituacaoCardapio;
    periodo: PeriodoCardapio;
    cardapioDuplicadoDeId: number | null;
    refeicoes: RefeicaoCardapio[];
    total: ValoresItem;
}

/** Linha da listagem: sem itens, só metadados (RF13). */
export interface CardapioResumo {
    id: number;
    nome: string;
    unidadeId: number;
    situacao: SituacaoCardapio;
    periodo: PeriodoCardapio;
    totalRefeicoes: number;
}

export interface PaginaCardapios<T> {
    pagina: number;
    tamanho: number;
    total: number;
    itens: T[];
}

export interface FiltroCardapios {
    unidadeId?: number;
    data?: string;
    situacao?: SituacaoCardapio;
    pagina?: number;
    tamanho?: number;
}

export interface FiltroPublicados {
    unidadeId: number;
    data?: string;
    refeicao?: string;
    pagina?: number;
    tamanho?: number;
}

export interface ItemEntrada {
    alimentoId: number;
    gramas: number;
}

export interface RefeicaoEntrada {
    nome: string;
    ordem: number;
    itens: ItemEntrada[];
}

export interface CardapioEntrada {
    nome: string;
    unidadeId: number;
    periodo: PeriodoCardapio;
    refeicoes: RefeicaoEntrada[];
}

export interface DuplicarEntrada {
    nome?: string;
    periodo: PeriodoCardapio;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

function comoParametros(filtros: object): string {
    const parametros = new URLSearchParams();

    for (const [chave, valor] of Object.entries(filtros) as [
        string,
        string | number | undefined,
    ][]) {
        if (valor !== undefined && valor !== "") {
            parametros.set(chave, String(valor));
        }
    }

    const consulta = parametros.toString();

    return consulta ? `?${consulta}` : "";
}

/** Listagem interna, escopada por papel (RF8). */
export async function listarCardapios(
    filtros: FiltroCardapios = {},
): Promise<PaginaCardapios<CardapioResumo>> {
    const resposta = await fetch(`${ApiURL}/api/cardapios${comoParametros(filtros)}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os cardápios");

    return resposta.json();
}

/** Rota anônima de pais e merendeiras (RF9). */
export async function listarCardapiosPublicados(
    filtros: FiltroPublicados,
): Promise<PaginaCardapios<Cardapio>> {
    const resposta = await fetch(
        `${ApiURL}/api/cardapios/publicados${comoParametros(filtros)}`,
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os cardápios publicados");

    return resposta.json();
}

export async function obterCardapio(id: number): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o cardápio");

    return resposta.json();
}

export async function criarCardapio(entrada: CardapioEntrada): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao criar o cardápio");

    return resposta.json();
}

export async function atualizarCardapio(
    id: number,
    entrada: CardapioEntrada,
): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar o cardápio");

    return resposta.json();
}

export async function excluirCardapio(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao excluir o cardápio");
}

export async function duplicarCardapio(
    id: number,
    entrada: DuplicarEntrada,
): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/duplicar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao duplicar o cardápio");

    return resposta.json();
}
