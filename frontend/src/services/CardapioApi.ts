const ApiURL = "";

export type Refeicao = "cafe" | "almoco" | "lanche" | "jantar";
export type Serie = "Creche" | "Integral" | "geral";

export interface AlimentoCardapio {
    id: number;
    nome: string;
    calorias: number;
    carboidratos: number;
    proteinas: number;
    gorduras: number;
    fibras: number;
    sodio: number;
}

export interface Alimento extends AlimentoCardapio {
    categoria: string;
}

export interface CardapioCadastro {
    refeicao: Refeicao;
    alimentos: AlimentoCardapio[];
}

export interface CardapioHistorico {
    id: number;
    refeicoes: {
        cafe: AlimentoCardapio[];
        almoco: AlimentoCardapio[];
        lanche: AlimentoCardapio[];
        jantar: AlimentoCardapio[];
    };
    tipo: Serie;
    data: string;
    dataCriacao: string;
}

/**
 * As rotas de cardápio só existem a partir da Spec 012. Até lá, 404 significa
 * "módulo indisponível" e a tela mostra estado vazio em vez de erro no console.
 */
async function buscarOuVazio<T>(url: string): Promise<T[]> {
    const resposta = await fetch(url, { credentials: "include" });

    if (resposta.status === 404) return [];

    if (!resposta.ok) throw new Error("Erro ao carregar dados do cardápio");

    return resposta.json();
}

export async function buscarAlimentos(): Promise<Alimento[]> {
    return buscarOuVazio<Alimento>(`${ApiURL}/api/alimentos`);
}

export async function buscarCardapio(
    refeicao: Refeicao
): Promise<AlimentoCardapio[]> {
    return buscarOuVazio<AlimentoCardapio>(`${ApiURL}/api/cardapios/${refeicao}`);
}

export async function buscarHistoricoCardapios(): Promise<CardapioHistorico[]> {
    return buscarOuVazio<CardapioHistorico>(`${ApiURL}/api/cardapios/historico`);
}

export async function salvarCardapio(dados: CardapioCadastro) {
    const resposta = await fetch(`${ApiURL}/api/cardapios`, {
        method: "POST",
        credentials: "include",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dados)
    });

    if (!resposta.ok) throw new Error("Erro ao salvar cardápio");

    if (resposta.status === 204) return;

    return resposta.json();
}
