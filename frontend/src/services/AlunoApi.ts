import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export interface AlunoCadastro {
    nome: string;
    nascimento: string;
    serie: string;
    escola: string;
    telefone: string;
    observacao: string;
    restricoes: string[];
    anafilaxia: boolean;
    nomeResponsavel: string;
    telefoneResponsavel: string;
}

export interface AlunoResposta extends AlunoCadastro {
    id: number;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function cadastrarAlunos(dados: AlunoCadastro): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/registrar`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao cadastrar aluno");
}

export async function buscarAlunoPorId(id: number): Promise<AlunoResposta> {
    const resposta = await fetch(`${ApiURL}/api/aluno/perfil/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao buscar aluno");

    return resposta.json();
}

export async function atualizarAluno(id: number, dados: AlunoCadastro): Promise<AlunoResposta> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao atualizar aluno");

    return resposta.json();
}

export async function buscarAlunos(): Promise<AlunoResposta[]> {
    const resposta = await fetch(`${ApiURL}/api/aluno`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao buscar alunos");

    return resposta.json();
}

export async function excluirAluno(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao excluir aluno");
}
