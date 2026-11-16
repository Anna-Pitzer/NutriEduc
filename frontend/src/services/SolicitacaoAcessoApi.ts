import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export interface SolicitacaoAcessoResumo {
    id: number;
    tipoSolicitado: string;
    status: string;
    unidadeId: number;
    usuarioId: number;
    nome: string;
    email: string;
    cpf: string;
    solicitadoEm: string;
}

export interface SolicitacaoAcessoDetalhe extends SolicitacaoAcessoResumo {
    crnSolicitado?: string | null;
    papelConcedido?: string | null;
    escolaConcedidaId?: number | null;
    motivo?: string | null;
    justificativaCorrecaoPapel?: string | null;
    decididoEm?: string | null;
    decididoPorNome?: string | null;
}

export interface Pagina<T> {
    pagina: number;
    tamanho: number;
    total: number;
    itens: T[];
}

function montarQuery(params: Record<string, string | number | undefined>): string {
    const q = new URLSearchParams();

    for (const [chave, valor] of Object.entries(params)) {
        if (valor !== undefined && valor !== "") {
            q.set(chave, String(valor));
        }
    }

    const texto = q.toString();
    return texto ? `?${texto}` : "";
}

export async function listarSolicitacoesAcesso(params: {
    status?: string;
    tipo?: string;
    busca?: string;
    pagina?: number;
    tamanho?: number;
} = {}): Promise<Pagina<SolicitacaoAcessoResumo>> {
    const resposta = await fetch(
        `${ApiURL}/api/solicitacoes-acesso${montarQuery({ ...params })}`,
        { credentials: "include" },
    );

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao listar solicitações de acesso"),
            resposta.status,
        );
    }

    return resposta.json();
}

export async function obterSolicitacaoAcesso(id: number): Promise<SolicitacaoAcessoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes-acesso/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao carregar a solicitação"),
            resposta.status,
        );
    }

    return resposta.json();
}

export interface AprovarAcessoPayload {
    escolaId?: number | null;
    alunosIds?: number[];
    papelConcedido?: string | null;
    justificativaCorrecaoPapel?: string | null;
}

export async function aprovarSolicitacaoAcesso(
    id: number,
    body: AprovarAcessoPayload,
): Promise<SolicitacaoAcessoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes-acesso/${id}/aprovar`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
    });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao aprovar a solicitação"),
            resposta.status,
        );
    }

    return resposta.json();
}

export async function recusarSolicitacaoAcesso(
    id: number,
    motivo: string,
): Promise<SolicitacaoAcessoDetalhe> {
    const resposta = await fetch(`${ApiURL}/api/solicitacoes-acesso/${id}/recusar`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ motivo }),
    });

    if (!resposta.ok) {
        throw new ErroRequisicao(
            await extrairErro(resposta, "Erro ao recusar a solicitação"),
            resposta.status,
        );
    }

    return resposta.json();
}
