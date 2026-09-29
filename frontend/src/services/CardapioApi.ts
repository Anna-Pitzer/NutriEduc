const ApiURL = "";

export type Refeicao = "cafe" | "almoco" | "lanche" | "jantar";
export type Serie = "Creche" | "Integral" | "geral"

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

export async function buscarAlimentos(): Promise<Alimento[]> {
    const resposta = await fetch(`${ApiURL}/api/alimentos`);

    if (!resposta.ok) throw new Error("Erro ao buscar alimentos");

    return resposta.json();
}

export async function buscarCardapio(
    refeicao: Refeicao
): Promise<AlimentoCardapio[]> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${refeicao}`);

    if (!resposta.ok) throw new Error("Erro ao buscar cardápio");

    return resposta.json();
}

export async function salvarCardapio(dados: CardapioCadastro) {
    const resposta = await fetch(`${ApiURL}/api/cardapios`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dados)
    });

    if (!resposta.ok) throw new Error("Erro ao salvar cardápio");

    if (resposta.status === 204) return;

    return resposta.json();
}

export async function excluirCardapio(refeicao: Refeicao) {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${refeicao}`,
        {
            method: "DELETE"
        }
    );

    if (!resposta.ok) throw new Error("Erro ao excluir cardápio");

    if (resposta.status === 204) return;

    return resposta.json();
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

export async function salvarCardapioCompleto(dados: {
    refeicoes: CardapioHistorico["refeicoes"];
    tipo: Serie;
    data: string;
}) {
    const resposta = await fetch(`${ApiURL}/api/cardapios/historico`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dados)
    });

    if(!resposta.ok) throw new Error("Erro ao salvar cardápio");

    return resposta.json();
}

export async function buscarHistoricoCardapios(): Promise<CardapioHistorico[]> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/historico`);
    
    if (!resposta.ok) throw new Error("Erro ao buscar histórico de cardápios");

    return resposta.json();
}

export async function buscarCardapioHistorico(id: number): Promise<CardapioHistorico[]> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/historico/${id}`);
    
    if (!resposta.ok) throw new Error("Erro ao buscar cardápio");

    return resposta.json();
}

export async function excluirCardapioHistorico(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/historico/${id}`, {
        method: "DELETE"
    });

    if (!resposta.ok) throw new Error("Erro ao excluir cardápio");
}