import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Bell } from "lucide-react";
import type { Notificacao } from "../services/NotificacaoApi";
import {
  contarNaoLidas,
  listarNotificacoes,
  marcarLida,
  marcarTodas,
} from "../services/NotificacaoApi";

/**
 * Sino de notificações do cabeçalho (Spec 018, RF12): contador de não lidas vindo do
 * backend, lista com título, mensagem e data/hora, diferenciação de lidas e link para o
 * recurso de origem.
 */
function rotaDaNotificacao(item: Notificacao): string | null {
  switch (item.referenciaTipo?.toLowerCase()) {
    case "cardapio":
      return item.referenciaId ? `/cardapios/${item.referenciaId}` : "/cardapios";
    case "solicitacao":
      return "/solicitacoes";
    case "solicitacaoacesso":
      return item.referenciaId
        ? `/secretaria/solicitacoes-acesso?abrir=${item.referenciaId}`
        : "/secretaria/solicitacoes-acesso";
    case "ocorrencia":
      return "/ocorrencias";
    case "feedback":
      return "/feedbacks";
    case "aluno":
      return "/pais";
    default:
      return null;
  }
}

function formatarDataHora(valor: string): string {
  const data = new Date(valor);

  if (Number.isNaN(data.getTime())) {
    return "";
  }

  return data.toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function SinoNotificacoes() {
  const [aberto, setAberto] = useState(false);
  const [itens, setItens] = useState<Notificacao[]>([]);
  const [naoLidas, setNaoLidas] = useState(0);
  const [erro, setErro] = useState("");

  const navigate = useNavigate();

  async function carregar() {
    try {
      const [pagina, total] = await Promise.all([
        listarNotificacoes(),
        contarNaoLidas(),
      ]);

      setItens(pagina.itens);
      setNaoLidas(total);
      setErro("");
    } catch {
      // Falha ao carregar não derruba o cabeçalho, mas fica visível no painel.
      setErro("Não foi possível carregar as notificações.");
    }
  }

  useEffect(() => {
    let ativo = true;

    const atualizar = () => {
      Promise.all([listarNotificacoes(), contarNaoLidas()])
        .then(([pagina, total]) => {
          if (!ativo) return;

          setItens(pagina.itens);
          setNaoLidas(total);
          setErro("");
        })
        .catch(() => {
          if (ativo) setErro("Não foi possível carregar as notificações.");
        });
    };

    atualizar();

    // Sem WebSocket/fila: o sino se atualiza sozinho enquanto a sessão está aberta.
    const intervalo = window.setInterval(atualizar, 30000);
    window.addEventListener("focus", atualizar);

    return () => {
      ativo = false;
      window.clearInterval(intervalo);
      window.removeEventListener("focus", atualizar);
    };
  }, []);

  async function alternar() {
    if (!aberto) {
      await carregar();
    }

    setAberto(!aberto);
  }

  async function abrir(item: Notificacao) {
    const rota = rotaDaNotificacao(item);

    try {
      await marcarLida(item.id);
      setItens((atuais) =>
        atuais.map((notificacao) =>
          notificacao.id === item.id ? { ...notificacao, lida: true } : notificacao,
        ),
      );

      if (!item.lida) {
        setNaoLidas((atual) => Math.max(0, atual - 1));
      }
    } catch {
      // A navegação não depende do sucesso da marcação de leitura.
    }

    if (rota) {
      navigate(rota);
    } else {
      await carregar();
    }
  }

  async function lerTodas() {
    try {
      await marcarTodas();
      setItens((atuais) => atuais.map((item) => ({ ...item, lida: true })));
      setNaoLidas(0);
    } catch {
      // Falha ao marcar não deve derrubar o cabeçalho; a lista recarrega abaixo.
    }

    await carregar();
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => void alternar()}
        aria-label={`Notificações${naoLidas > 0 ? ` (${naoLidas} não lidas)` : ""}`}
        className="relative flex items-center gap-2 rounded-lg bg-white px-3 py-2 text-sm font-semibold text-roxo shadow-sm hover:bg-gray-100"
      >
        <Bell size={18} />
        {naoLidas > 0 && (
          <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-vermelho text-[11px] font-bold text-white">
            {naoLidas > 99 ? "99+" : naoLidas}
          </span>
        )}
      </button>

      {aberto && (
        <div className="absolute right-0 top-12 z-20 flex w-96 flex-col rounded-xl border border-gray-200 bg-white shadow-lg">
          <div className="flex items-center justify-between border-b border-gray-100 px-3 py-2">
            <span className="text-sm font-semibold text-roxo">Notificações</span>
            {naoLidas > 0 && (
              <button
                type="button"
                onClick={() => void lerTodas()}
                className="text-xs font-medium text-verdeClaro hover:underline"
              >
                Marcar todas como lidas
              </button>
            )}
          </div>

          <div className="max-h-80 overflow-y-auto overscroll-contain px-2 py-1">
          {erro && <p className="px-2 py-2 text-xs text-red-600">{erro}</p>}

          {itens.length === 0 ? (
            <p className="px-2 py-6 text-center text-sm text-gray-500">
              Nenhuma notificação.
            </p>
          ) : (
            <ul className="divide-y divide-gray-100">
              {itens.map((item) => (
                <li key={item.id} className={item.lida ? "" : "bg-gray-50"}>
                  <button
                    type="button"
                    onClick={() => void abrir(item)}
                    className="flex w-full items-start gap-2 px-2 py-2 text-left hover:bg-gray-100"
                  >
                    <span
                      aria-hidden="true"
                      className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${
                        item.lida ? "bg-transparent" : "bg-vermelho"
                      }`}
                    />
                    <span className="min-w-0">
                      <span
                        className={`block text-sm ${
                          item.lida ? "text-gray-600" : "font-semibold text-roxo"
                        }`}
                      >
                        {item.titulo}
                      </span>
                      <span className="block text-xs text-gray-500">{item.mensagem}</span>
                      <span className="block text-[11px] text-gray-400">
                        {formatarDataHora(item.criadoEm)}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          </div>
        </div>
      )}
    </div>
  );
}
