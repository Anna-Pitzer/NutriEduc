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

export async function cadastrarAlunos(dados: AlunoCadastro): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/registrar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados),
    });

    if (!resposta.ok) throw new Error("Erro ao cadastrar aluno");
}

export async function buscarAlunos(): Promise<AlunoResposta[]> {
    const resposta = await fetch(`${ApiURL}/api/aluno`);

    if (!resposta.ok) throw new Error("Erro ao buscar alunos");

    return resposta.json();
}

export async function excluirAluno(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/aluno/${id}`, {
        method: "DELETE",
    });

    if (!resposta.ok) throw new Error("Erro ao excluir aluno");
}