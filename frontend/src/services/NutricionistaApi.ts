import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

/**
 * Nutricionista selecionável como responsável por um cardápio. O contrato expõe apenas a
 * identificação profissional (nome, e-mail, CPF e CRN) — nunca dados de autenticação.
 */
export interface Nutricionista {
    id: number;
    nome: string | null;
    email: string | null;
    cpf: string | null;
    crn: string | null;
}

export async function listarNutricionistas(): Promise<Nutricionista[]> {
    const resposta = await fetch(`${ApiURL}/api/nutricionistas`, { credentials: "include" });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao carregar as nutricionistas"),
            resposta.status,
        );
    }

    return resposta.json();
}
