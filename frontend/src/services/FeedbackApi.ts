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
  /** Aluno associado (Spec 035); nulo quando o feedback não referencia aluno. */
  alunoId: number | null;
  aluno: string | null;
  status: StatusFeedback;
  criadoEm: string;
  /** Resposta da nutrição/escola (Spec 018) exposta também na listagem (Spec 035, RF23). */
  resposta: string | null;
  respondidoPor: string | null;
  podeResponder: boolean;
  podeEncerrar: boolean;
}

export interface FeedbackDetalhe extends FeedbackResumo {
  atualizadoEm: string;
}

export interface PaginaFeedbacks {
  pagina: number;
  tamanho: number;
  total: number;
  itens: FeedbackResumo[];
}

export interface FiltroFeedback {
  status?: StatusFeedback | "";
  alunoId?: number;
  pagina?: number;
  tamanho?: number;
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
  throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function listarFeedbacks(
  filtro: FiltroFeedback = {},
): Promise<PaginaFeedbacks> {
  const parametros = new URLSearchParams();

  if (filtro.status) parametros.set("status", filtro.status);
  if (filtro.alunoId) parametros.set("alunoId", String(filtro.alunoId));
  parametros.set("pagina", String(filtro.pagina ?? 1));
  parametros.set("tamanho", String(filtro.tamanho ?? 20));

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
