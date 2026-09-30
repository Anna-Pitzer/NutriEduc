const ApiURL = "";

export interface AlunoCadastro {
    nome: string;
    nascimento: string;
    serie: string;
    escola: string;
    telefone: string;
    observacao: string;
    anafilaxia: boolean;
    nomeResponsavel: string;
    telefoneResponsavel: string;
}

export interface AlunoResposta {
    id: number;
    nome: string;
    nascimento: string;
    serie: string;
    escola: string;
    telefone: string;
    observacao: string;
    anafilaxia: boolean;
    nomeResponsavel: string;
    telefoneResponsavel: string;
}

export async function cadastrarAlunos(dados: AlunoCadastro) {
    const resposta = await fetch(`${ApiURL}/api/aluno`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) throw new Error("Erro ao cadastrar aluno");
}

export async function buscarAlunos(): Promise<AlunoResposta[]> {
    const resposta = await fetch(`${ApiURL}/api/aluno`, {
        credentials: "include",
    });

    if (!resposta.ok) throw new Error("Erro ao buscar alunos");

    return resposta.json();
}

export async function excluirAluno(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) throw new Error("Erro ao excluir aluno");
}
