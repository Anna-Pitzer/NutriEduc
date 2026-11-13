import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

/** Responsável vinculado a um aluno (Spec 027/034). */
export interface ResponsavelVinculado {
    id: number;
    nome: string;
    email: string;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function listarResponsaveis(alunoId: number): Promise<ResponsavelVinculado[]> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${alunoId}/responsaveis`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os responsáveis");

    return resposta.json();
}

export async function vincularResponsavel(alunoId: number, usuarioId: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${alunoId}/responsaveis/${usuarioId}`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao vincular o responsável");
}

export async function desvincularResponsavel(alunoId: number, usuarioId: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${alunoId}/responsaveis/${usuarioId}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao desvincular o responsável");
}
