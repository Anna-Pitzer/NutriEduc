import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { ErroRequisicao, naoAutorizado } from "../../services/erroApi";
import {
    criarFeedback,
    encerrarFeedback,
    listarFeedbacks,
    type FeedbackResumo,
    type StatusFeedback,
} from "../../services/FeedbackApi";
import { FormularioFeedback } from "../../components/FormularioFeedback";
import {
    ListaFeedback,
    rotuloStatusFeedback,
} from "../../components/ListaFeedback";

const TAMANHO_PAGINA = 20;

/**
 * Feedback do responsável (Spec 035, RF21–RF25). O aluno é opcional; a lista mostra
 * somente os feedbacks do próprio autor, com resposta e encerramento quando cabível.
 */
export function FeedbackPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [feedbacks, setFeedbacks] = useState<FeedbackResumo[]>([]);
    const [total, setTotal] = useState(0);
    const [pagina, setPagina] = useState(1);

    const [filtroAluno, setFiltroAluno] = useState<number | "">("");
    const [filtroStatus, setFiltroStatus] = useState<StatusFeedback | "">("");

    const [alunoId, setAlunoId] = useState<number | null>(null);
    const [assunto, setAssunto] = useState("");
    const [mensagem, setMensagem] = useState("");

    const [carregandoFilhos, setCarregandoFilhos] = useState(true);
    const [carregando, setCarregando] = useState(true);
    const [enviando, setEnviando] = useState(false);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [erroFilhos, setErroFilhos] = useState("");
    const [erroAssunto, setErroAssunto] = useState("");
    const [erroMensagem, setErroMensagem] = useState("");
    const [sucesso, setSucesso] = useState("");
    const [tentativa, setTentativa] = useState(0);

    const tratarFalha = useCallback(
        async (falha: unknown, padrao: string) => {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            setErro(falha instanceof Error ? falha.message : padrao);
        },
        [logout],
    );

    useEffect(() => {
        let ativo = true;

        async function carregarFilhos() {
            try {
                const lista = await buscarAlunos();

                if (ativo) {
                    setFilhos(
                        [...lista].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
                    );
                }
            } catch (falha) {
                if (ativo) {
                    if (naoAutorizado(falha)) {
                        await logout();
                    } else {
                        setErroFilhos(
                            falha instanceof Error
                                ? falha.message
                                : "Não foi possível carregar seus filhos.",
                        );
                    }
                }
            } finally {
                if (ativo) {
                    setCarregandoFilhos(false);
                }
            }
        }

        void carregarFilhos();

        return () => {
            ativo = false;
        };
    }, [logout, tentativa]);

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const resposta = await listarFeedbacks({
                status: filtroStatus,
                alunoId: filtroAluno === "" ? undefined : filtroAluno,
                pagina,
                tamanho: TAMANHO_PAGINA,
            });

            setFeedbacks(resposta.itens);
            setTotal(resposta.total);
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível carregar os feedbacks.");
        } finally {
            setCarregando(false);
        }
    }, [filtroStatus, filtroAluno, pagina, tratarFalha]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    function limparFiltros() {
        setFiltroAluno("");
        setFiltroStatus("");
        setPagina(1);
    }

    async function enviar() {
        setErro("");
        setErroAssunto("");
        setErroMensagem("");
        setSucesso("");

        const textoAssunto = assunto.trim();
        const textoMensagem = mensagem.trim();
        let invalido = false;

        if (textoAssunto.length < 5 || textoAssunto.length > 120) {
            setErroAssunto("O assunto deve ter entre 5 e 120 caracteres.");
            invalido = true;
        }

        if (textoMensagem.length < 5 || textoMensagem.length > 1000) {
            setErroMensagem("A mensagem deve ter entre 5 e 1000 caracteres.");
            invalido = true;
        }

        if (invalido) {
            return;
        }

        setEnviando(true);

        try {
            await criarFeedback(
                textoAssunto,
                textoMensagem,
                alunoId === null ? undefined : alunoId,
            );

            setAssunto("");
            setMensagem("");
            setSucesso("Feedback enviado.");
            await carregar();
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível enviar o feedback.");
        } finally {
            setEnviando(false);
        }
    }

    async function encerrar(id: number) {
        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            await encerrarFeedback(id);
            setSucesso("Feedback encerrado.");
            await carregar();
        } catch (falha) {
            if (falha instanceof ErroRequisicao && falha.status === 409) {
                const mensagem = falha.message;
                await carregar();
                setErro(mensagem);
            } else {
                await tratarFalha(falha, "Não foi possível encerrar o feedback.");
            }
        } finally {
            setSalvando(false);
        }
    }

    const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));

    if (carregandoFilhos) {
        return <p className="text-sm text-slate-500">Carregando seus filhos...</p>;
    }

    if (erroFilhos) {
        return (
            <div className="rounded-2xl border border-red-200 bg-red-50 p-5">
                <p role="alert" className="text-sm text-red-700">{erroFilhos}</p>
                <button
                    type="button"
                    onClick={() => {
                        setErroFilhos("");
                        setCarregandoFilhos(true);
                        setTentativa((atual) => atual + 1);
                    }}
                    className="mt-3 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white"
                >
                    Tentar novamente
                </button>
            </div>
        );
    }

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Feedback</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Envie uma mensagem para a nutrição/escola. O filho é opcional e serve apenas de
                    contexto.
                </p>
            </header>

            <FormularioFeedback
                filhos={filhos}
                alunoId={alunoId}
                onAluno={setAlunoId}
                assunto={assunto}
                onAssunto={setAssunto}
                mensagem={mensagem}
                onMensagem={setMensagem}
                enviando={enviando}
                erro={erro}
                erroAssunto={erroAssunto}
                erroMensagem={erroMensagem}
                onSubmit={() => void enviar()}
            />

            {sucesso && (
                <p role="status" className="mt-4 rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">
                    {sucesso}
                </p>
            )}

            <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                <div className="grid gap-3 md:grid-cols-2">
                    <div className="flex flex-col gap-1">
                        <label htmlFor="feedback-filtro-filho" className="text-sm font-medium text-slate-700">
                            Filho
                        </label>
                        <select
                            id="feedback-filtro-filho"
                            value={filtroAluno}
                            onChange={(evento) => {
                                setFiltroAluno(
                                    evento.target.value === "" ? "" : Number(evento.target.value),
                                );
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        >
                            <option value="">Todos os filhos</option>
                            {filhos.map((filho) => (
                                <option key={filho.id} value={filho.id}>
                                    {filho.nome}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="feedback-filtro-status" className="text-sm font-medium text-slate-700">
                            Status
                        </label>
                        <select
                            id="feedback-filtro-status"
                            value={filtroStatus}
                            onChange={(evento) => {
                                setFiltroStatus(evento.target.value as StatusFeedback | "");
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        >
                            <option value="">Todos os status</option>
                            {Object.entries(rotuloStatusFeedback).map(([valor, rotulo]) => (
                                <option key={valor} value={valor}>
                                    {rotulo}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                {(filtroAluno !== "" || filtroStatus !== "") && (
                    <button
                        type="button"
                        onClick={limparFiltros}
                        className="mt-3 rounded-xl border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700"
                    >
                        Limpar filtros
                    </button>
                )}
            </div>

            <article className="mt-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                <h2 className="text-lg font-semibold text-roxo">Meus feedbacks</h2>

                <ListaFeedback
                    itens={feedbacks}
                    carregando={carregando}
                    onEncerrar={salvando ? undefined : (id) => void encerrar(id)}
                    vazioTexto="Nenhum feedback enviado. Use o formulário acima para falar com a escola."
                />

                <div className="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-100 pt-3 text-sm text-slate-600">
                    <span>
                        {total} feedback(s) · página {pagina} de {totalPaginas}
                    </span>
                    <div className="flex gap-2">
                        <button
                            type="button"
                            disabled={pagina <= 1}
                            onClick={() => setPagina((atual) => Math.max(1, atual - 1))}
                            className="rounded-xl border border-slate-300 px-3 py-1 font-semibold text-slate-700 disabled:opacity-40"
                        >
                            Anterior
                        </button>
                        <button
                            type="button"
                            disabled={pagina >= totalPaginas}
                            onClick={() => setPagina((atual) => atual + 1)}
                            className="rounded-xl border border-slate-300 px-3 py-1 font-semibold text-slate-700 disabled:opacity-40"
                        >
                            Próxima
                        </button>
                    </div>
                </div>
            </article>

            <Link
                to="/pais"
                className="mt-4 inline-block text-sm font-semibold text-roxo underline"
            >
                Voltar para o início
            </Link>
        </section>
    );
}
