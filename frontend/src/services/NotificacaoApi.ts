import { ErroRequisicao, extrairErro } from "./erroApi";

const ApiURL = "";

export type TipoNotificacao =
  | "CardapioPublicado"
  | "CardapioAlterado"
  | "SolicitacaoStatus"
  | "OcorrenciaStatus"
  | "AlunoAtualizado"
  | "FeedbackRespondido"
  | "SolicitacaoAcesso"
  | "NovaSolicitacao"
  | "OcorrenciaRegistrada";

export interface Notificacao {
  id: number;
  tipo: TipoNotificacao;
  titulo: string;
  mensagem: string;
  referenciaTipo: string | null;
  referenciaId: number | null;
  lida: boolean;
  criadoEm: string;
}

interface PaginaNotificacoes {
  pagina: number;
  tamanho: number;
  total: number;
  itens: Notificacao[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
  throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

export async function listarNotificacoes(naoLidas?: boolean): Promise<PaginaNotificacoes> {
  const parametros = new URLSearchParams();
  if (naoLidas) parametros.set("naoLidas", "true");
  parametros.set("tamanho", "100");

  const resposta = await fetch(`${ApiURL}/api/notificacoes?${parametros.toString()}`, {
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao carregar as notificações");

  return resposta.json();
}

/**
 * Total de não lidas do usuário da sessão. Usa o `total` da paginação com `tamanho=1`
 * para não depender de quantas notificações a lista carregada contém.
 */
export async function contarNaoLidas(): Promise<number> {
  const resposta = await fetch(`${ApiURL}/api/notificacoes?naoLidas=true&tamanho=1`, {
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao contar as notificações não lidas");

  const pagina: PaginaNotificacoes = await resposta.json();
  return pagina.total;
}

export async function marcarLida(id: number): Promise<Notificacao> {
  const resposta = await fetch(`${ApiURL}/api/notificacoes/${id}/ler`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao marcar a notificação como lida");

  return resposta.json();
}

export async function marcarTodas(): Promise<void> {
  const resposta = await fetch(`${ApiURL}/api/notificacoes/ler-todas`, {
    method: "POST",
    credentials: "include",
  });

  if (!resposta.ok) return falhar(resposta, "Erro ao marcar as notificações como lidas");
}
