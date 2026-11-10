import { ErroRequisicao, extrairErro } from "./erroApi";

const ApiURL = "";

export type StatusFeedback = "Aberto" | "Respondido" | "Encerrado";

export type PapelAutor =
  | "Nutricionista"
  | "Merendeira"
  | "Responsavel"
  | "AdminSecretaria"
  | "AdminEscola";

export interface FeedbackResumo {
  id: number;
  assunto: string;
  mensagem: string;
  autor: string;
  papelAutor: PapelAutor;
  unidadeId: number;
  escolaId: number | null;
  status: StatusFeedback;
  criadoEm: string;
  podeResponder: boolean;
  podeEncerrar: boolean;
}

export interface FeedbackDetalhe extends FeedbackResumo {
  resposta: string | null;
  respondidoPor: string | null;
  atualizadoEm: string;
}

interface PaginaFeedbacks {
  pagina: number;
  tamanho: number;
  total: number;
  itens: FeedbackResumo[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
  throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function listarFeedbacks(status?: StatusFeedback): Promise<PaginaFeedbacks> {
  const parametros = new URLSearchParams();
  if (status) parametros.set("status", status);
  parametros.set("tamanho", "100");

  const resposta = await fetch(`${ApiURL}/api/feedbacks?${parametros.toString()}`, {
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao carregar os feedbacks");

  return resposta.json();
}

export async function criarFeedback(
  assunto: string,
  mensagem: string,
  alunoId?: number,
): Promise<FeedbackDetalhe> {
  const resposta = await fetch(`${ApiURL}/api/feedbacks`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({ assunto, mensagem, alunoId }),
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao enviar o feedback");

  return resposta.json();
}

export async function responderFeedback(
  id: number,
  resposta: string,
): Promise<FeedbackDetalhe> {
  const http = await fetch(`${ApiURL}/api/feedbacks/${id}/responder`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({ resposta }),
  });

  if (!http.ok) return falhar(http, "Erro ao responder o feedback");

  return http.json();
}

export async function encerrarFeedback(id: number): Promise<FeedbackDetalhe> {
  const resposta = await fetch(`${ApiURL}/api/feedbacks/${id}/encerrar`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao encerrar o feedback");

  return resposta.json();
}
