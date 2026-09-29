const ApiURL = "";

export interface AlunoCadastro {
    nome: string;
    matricula: string;
    nascimento: string;
    serie: string;
    escola: string;
    telefone: string;
    observacoes: string;
    restricoes: string[];
    anafilaxia: boolean;
    contatoEmerNome: string;
    contatoEmerTelefone: string;
}

export async function cadastrarAlunos(dados: AlunoCadastro) {
    const resposta = await fetch(`${ApiURL}/api/alunos`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dados)
    });

    if (!resposta.ok) throw new Error("Erro ao cadastrar aluno");

    if (resposta.status === 204) return;

    return resposta.json();
}