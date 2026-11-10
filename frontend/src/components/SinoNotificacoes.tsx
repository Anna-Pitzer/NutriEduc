import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Bell } from "lucide-react";
import type { Notificacao } from "../services/NotificacaoApi";
import { listarNotificacoes, marcarLida, marcarTodas } from "../services/NotificacaoApi";

/**
 * Sino de notificações do cabeçalho (Spec 018, RF12): contador de não lidas, lista com
 * mensagem e marcação de leitura ao abrir.
 */
function rotaDaNotificacao(item: Notificacao): string | null {
  switch (item.referenciaTipo?.toLowerCase()) {
    case "cardapio":
      return item.referenciaId ? `/cardapios/${item.referenciaId}` : "/cardapios";
    case "solicitacao":
      return "/solicitacoes";
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

export function SinoNotificacoes() {
  const [aberto, setAberto] = useState(false);
  const [itens, setItens] = useState<Notificacao[]>([]);

  const navigate = useNavigate();
  const naoLidas = itens.filter((item) => !item.lida).length;

  async function carregar() {
    try {
      const pagina = await listarNotificacoes();
      setItens(pagina.itens);
    } catch {
      // Falha ao carregar não deve derrubar o cabeçalho.
    }
  }

  useEffect(() => {
    listarNotificacoes()
      .then((pagina) => setItens(pagina.itens))
      .catch(() => {
        // Falha ao carregar não deve derrubar o cabeçalho.
      });
  }, []);

  async function alternar() {
    if (!aberto) {
      await carregar();
    }

    setAberto(!aberto);
  }

  async function abrir(item: Notificacao) {
    await marcarLida(item.id);
    const rota = rotaDaNotificacao(item);

    if (rota) {
      navigate(rota);
    } else {
      await carregar();
    }
  }

  async function lerTodas() {
    await marcarTodas();
    await carregar();
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => void alternar()}
        aria-label="Notificações"
        className="relative flex items-center gap-2 rounded-lg bg-white px-3 py-2 text-sm font-semibold text-roxo shadow-sm hover:bg-gray-100"
      >
        <Bell size={18} />
        {naoLidas > 0 && (
          <span className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-vermelho text-[11px] font-bold text-white">
            {naoLidas}
          </span>
        )}
      </button>

      {aberto && (
        <div className="absolute right-0 top-12 z-20 max-h-96 w-80 overflow-y-auto rounded-xl border border-gray-200 bg-white p-2 shadow-lg">
          <div className="flex items-center justify-between border-b border-gray-100 px-2 py-2">
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

          {itens.length === 0 ? (
            <p className="px-2 py-6 text-center text-sm text-gray-500">
              Nenhuma notificação.
            </p>
          ) : (
            <ul className="divide-y divide-gray-100">
              {itens.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={() => void abrir(item)}
                    className="w-full px-2 py-2 text-left hover:bg-gray-50"
                  >
                    <span
                      className={`block text-sm font-medium ${item.lida ? "text-gray-600" : "text-roxo"}`}
                    >
                      {item.titulo}
                    </span>
                    <span className="block text-xs text-gray-500">{item.mensagem}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
