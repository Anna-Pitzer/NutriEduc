import { useEffect, useState } from "react";
import { useAuth } from "../../context/authContext";
import type { FeedbackResumo } from "../../services/FeedbackApi";
import {
  criarFeedback,
  encerrarFeedback,
  listarFeedbacks,
  responderFeedback,
} from "../../services/FeedbackApi";
import { ListaFeedback } from "../../components/ListaFeedback";
import VoltarPaginas from "../../components/VoltarPaginas";
import Header from "../../components/Header";

/**
 * Tela de feedback (Spec 018): merendeira/responsável/escola enviam, nutrição/escola
 * respondem e o autor encerra o próprio feedback respondido. As permissões vêm do backend.
 */
export function FeedbackPage() {
  const { userLogin } = useAuth();
  const papel = userLogin?.papel;

  const [itens, setItens] = useState<FeedbackResumo[]>([]);
  const [assunto, setAssunto] = useState("");
  const [mensagem, setMensagem] = useState("");
  const [respostas, setRespostas] = useState<Record<number, string>>({});
  const [erro, setErro] = useState<string | null>(null);

  const podeCriar =
    papel === "Merendeira" || papel === "Responsavel" || papel === "AdminEscola";

  async function carregar() {
    try {
      const pagina = await listarFeedbacks({ tamanho: 100 });
      setItens(pagina.itens);
    } catch (excecao) {
      setErro(excecao instanceof Error ? excecao.message : "Erro ao carregar os feedbacks");
    }
  }

  useEffect(() => {
    listarFeedbacks({ tamanho: 100 })
      .then((pagina) => setItens(pagina.itens))
      .catch((excecao) =>
        setErro(excecao instanceof Error ? excecao.message : "Erro ao carregar os feedbacks"),
      );
  }, []);

  async function enviar() {
    setErro(null);

    try {
      await criarFeedback(assunto, mensagem);
      setAssunto("");
      setMensagem("");
      await carregar();
    } catch (excecao) {
      setErro(excecao instanceof Error ? excecao.message : "Erro ao enviar o feedback");
    }
  }

  async function responder(id: number) {
    setErro(null);

    try {
      await responderFeedback(id, respostas[id] ?? "");
      setRespostas((atuais) => {
        const novas = { ...atuais };
        delete novas[id];
        return novas;
      });
      await carregar();
    } catch (excecao) {
      setErro(excecao instanceof Error ? excecao.message : "Erro ao responder o feedback");
    }
  }

  async function encerrar(id: number) {
    setErro(null);

    try {
      await encerrarFeedback(id);
      await carregar();
    } catch (excecao) {
      setErro(excecao instanceof Error ? excecao.message : "Erro ao encerrar o feedback");
    }
  }

  return (
    <section className="relative min-h-screen bg-fundo">
      <Header />

      <div className="
        max-w-7xl mx-auto
      bg-white border-4 border-lilaz rounded-3xl p-8 shadow-[0_8px_30px_rgba(11,102,25,0.08)] duration-300 hover:-translate-y-1.5">
        <header className="flex mb-6">
          <div>
            <VoltarPaginas />
          </div>

          <div>
            <h1 className="text-2xl font-bold text-verdeClaro">FeedBacks</h1>
            <p className="mt-1 text-sm text-slate-600">
              Area de recebimento dos feedbacks da plataforma.
            </p>

          </div>

        </header>

        {erro && (
          <p className="rounded-lg border border-vermelho bg-vermelho/10 px-4 py-2 text-sm text-vermelho">
            {erro}
          </p>
        )}

        {podeCriar && (
          <form
            className="space-y-3 rounded-xl border border-gray-200 bg-white p-4 shadow-sm"
            onSubmit={(evento) => {
              evento.preventDefault();
              void enviar();
            }}
          >
            <h2 className="text-lg font-semibold text-roxo">Enviar feedback</h2>
            <input
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
              placeholder="Assunto (5–120 caracteres)"
              value={assunto}
              onChange={(evento) => setAssunto(evento.target.value)}
            />
            <textarea
              className="w-full rounded-lg border border-gray-300 px-3 py-2"
              placeholder="Mensagem (5–1000 caracteres)"
              value={mensagem}
              onChange={(evento) => setMensagem(evento.target.value)}
            />
            <button
              type="submit"
              className="rounded-lg bg-verdeClaro px-4 py-2 text-sm font-semibold text-white hover:opacity-90"
            >
              Enviar
            </button>
          </form>
        )}

        <ListaFeedback
          itens={itens}
          onEncerrar={(id) => void encerrar(id)}
          renderAcoes={(feedback) =>
            feedback.podeResponder ? (
              <div className="mt-3 space-y-2">
                <label
                  htmlFor={`resposta-${feedback.id}`}
                  className="text-sm font-medium text-gray-700"
                >
                  Resposta
                </label>
                <textarea
                  id={`resposta-${feedback.id}`}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
                  placeholder="Resposta (5–1000 caracteres)"
                  value={respostas[feedback.id] ?? ""}
                  onChange={(evento) =>
                    setRespostas((atuais) => ({
                      ...atuais,
                      [feedback.id]: evento.target.value,
                    }))
                  }
                />
                <button
                  type="button"
                  className="rounded-lg bg-verdeClaro px-3 py-1 text-sm font-semibold text-white hover:opacity-90"
                  onClick={() => void responder(feedback.id)}
                >
                  Responder
                </button>
              </div>
            ) : null
          }
        />

      </div>

    </section>
  );
}
