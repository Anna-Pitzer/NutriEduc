import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

/** Composição por 100 g (Spec 011, RF3). */
export interface ValoresNutricionais {
    caloriasKcal: number;
    proteinasG: number;
    carboidratosG: number;
    gordurasG: number;
    fibrasG: number;
    sodioMg: number;
}

export type CategoriaRestricao = "Alergia" | "Restricao";

/** Vínculo resolvido: a severidade vem da categoria da restrição (Spec 011, RF4). */
export interface VinculoAlimento {
    restricaoId: number;
    restricao: string;
    categoria: CategoriaRestricao | null;
}

export interface CategoriaAlimento {
    id: number;
    nome: string;
    ativa: boolean;
}

export interface Alimento {
    id: number;
    nome: string;
    categoria: CategoriaAlimento;
    ativo: boolean;
    valores: ValoresNutricionais;
    vinculos: VinculoAlimento[];
}

export interface PaginaAlimentos {
    pagina: number;
    tamanho: number;
    total: number;
    itens: Alimento[];
}

/**
 * Ordenações aceitas pela listagem. Espelha `OrdenacaoAlimento` do backend: qualquer
 * outro valor devolve 400, então o tipo impede o erro em tempo de compilação.
 */
export type OrdenacaoAlimento = "nome" | "nome-desc" | "categoria" | "recentes";

export interface FiltroAlimentos {
    busca?: string;
    categoriaId?: number;
    pagina?: number;
    tamanho?: number;
    incluirInativos?: boolean;
    ordenar?: OrdenacaoAlimento;
}

export interface AlimentoEntrada {
    nome: string;
    categoriaId: number;
    valores: ValoresNutricionais;
    restricaoIds: number[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

function comoParametros(filtros: FiltroAlimentos): string {
    const parametros = new URLSearchParams();

    if (filtros.busca?.trim()) parametros.set("busca", filtros.busca.trim());
    if (filtros.categoriaId) parametros.set("categoriaId", String(filtros.categoriaId));
    if (filtros.pagina) parametros.set("pagina", String(filtros.pagina));
    if (filtros.tamanho) parametros.set("tamanho", String(filtros.tamanho));
    if (filtros.incluirInativos) parametros.set("incluirInativos", "true");
    if (filtros.ordenar && filtros.ordenar !== "nome") {
        parametros.set("ordenar", filtros.ordenar);
    }

    const consulta = parametros.toString();

    return consulta ? `?${consulta}` : "";
}

/** Listagem paginada do catálogo (Spec 011, RF7). */
export async function listarAlimentos(
    filtros: FiltroAlimentos = {},
): Promise<PaginaAlimentos> {
    const resposta = await fetch(`${ApiURL}/api/alimentos${comoParametros(filtros)}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o catálogo de alimentos");

    return resposta.json();
}

export async function obterAlimento(id: number): Promise<Alimento> {
    const resposta = await fetch(`${ApiURL}/api/alimentos/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o alimento");

    return resposta.json();
}

export async function criarAlimento(entrada: AlimentoEntrada): Promise<Alimento> {
    const resposta = await fetch(`${ApiURL}/api/alimentos`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar o alimento");

    return resposta.json();
}

export async function atualizarAlimento(
    id: number,
    entrada: AlimentoEntrada,
): Promise<Alimento> {
    const resposta = await fetch(`${ApiURL}/api/alimentos/${id}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar o alimento");

    return resposta.json();
}

export async function desativarAlimento(id: number): Promise<Alimento> {
    const resposta = await fetch(`${ApiURL}/api/alimentos/${id}/desativar`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao desativar o alimento");

    return resposta.json();
}

export async function reativarAlimento(id: number): Promise<Alimento> {
    const resposta = await fetch(`${ApiURL}/api/alimentos/${id}/reativar`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao reativar o alimento");

    return resposta.json();
}

/** Categorias do catálogo; `incluirInativas` é exclusivo da nutrição (RF1). */
export async function listarCategoriasAlimento(
    incluirInativas = false,
): Promise<CategoriaAlimento[]> {
    const consulta = incluirInativas ? "?incluirInativas=true" : "";

    const resposta = await fetch(`${ApiURL}/api/categorias-alimento${consulta}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as categorias");

    return resposta.json();
}

export async function criarCategoriaAlimento(nome: string): Promise<CategoriaAlimento> {
    const resposta = await fetch(`${ApiURL}/api/categorias-alimento`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nome }),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar a categoria");

    return resposta.json();
}

export async function atualizarCategoriaAlimento(
    id: number,
    nome: string,
): Promise<CategoriaAlimento> {
    const resposta = await fetch(`${ApiURL}/api/categorias-alimento/${id}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nome }),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar a categoria");

    return resposta.json();
}

/** Converte os vínculos resolvidos no formato aceito pelo formulário (só ids). */
export function restricaoIdsDe(alimento: Alimento): number[] {
    return alimento.vinculos.map((vinculo) => vinculo.restricaoId);
}

/** Texto curto de severidade para a listagem: alergia bloqueia, restrição alerta. */
export function descreverVinculo(vinculo: VinculoAlimento): string {
    const severidade = vinculo.categoria === "Alergia" ? "bloqueia" : "alerta";

    return `${vinculo.restricao} (${severidade})`;
}
