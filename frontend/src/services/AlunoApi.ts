import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export interface AlunoCadastro {
    nome: string;
    nascimento: string;
    serie: string;
    escola: string;
    /** Escola do aluno (Spec 027): fonte de verdade do vínculo institucional. */
    escolaId?: number | null;
    telefone: string;
    observacao: string;
    restricoes: string[];
    anafilaxia: boolean;
    nomeResponsavel: string;
    telefoneResponsavel: string;
}

export interface AlunoResposta extends AlunoCadastro {
    id: number;
    /** Derivada da escola pelo backend; nunca enviada no payload. */
    unidadeId?: number | null;
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

export interface FiltroAlunos {
    /** Busca textual pelo nome (Spec 034). */
    busca?: string;
    escolaId?: number | "";
    serie?: string;
}

export async function buscarAlunos(filtros: FiltroAlunos = {}): Promise<AlunoResposta[]> {
    const parametros = new URLSearchParams();

    if (filtros.busca?.trim()) {
        parametros.set("busca", filtros.busca.trim());
    }

    if (filtros.escolaId !== undefined && filtros.escolaId !== "") {
        parametros.set("escolaId", String(filtros.escolaId));
    }

    if (filtros.serie?.trim()) {
        parametros.set("serie", filtros.serie.trim());
    }

    const consulta = parametros.toString();
    const resposta = await fetch(`${ApiURL}/api/aluno${consulta ? `?${consulta}` : ""}`, {
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
