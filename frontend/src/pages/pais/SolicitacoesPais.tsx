import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { listarRestricoes, type Restricao } from "../../services/RestricaoApi";
import { ErroRequisicao, naoAutorizado } from "../../services/erroApi";
import {
    cancelarSolicitacao,
    criarSolicitacao,
    listarSolicitacoes,
    marcarAnafilaxia,
    obterSolicitacao,
    type SolicitacaoDetalhe,
    type SolicitacaoResumo,
    type StatusSolicitacao,
} from "../../services/SolicitacaoApi";
import { FormularioSolicitacao } from "../../components/FormularioSolicitacao";
import {
    corStatusSolicitacao,
    formatarDataSolicitacao,
    ListaSolicitacoes,
    rotuloStatusSolicitacao,
} from "../../components/ListaSolicitacoes";

const TAMANHO_PAGINA = 20;

/**
 * Solicitação de inclusão de alergia/restrição e acompanhamento (Spec 035, RF15–RF20).
 * O pedido não altera o cadastro do aluno: a decisão é exclusiva do `AdminSecretaria`.
 */
export function SolicitacoesPais() {
    const { userLogin, logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [restricoes, setRestricoes] = useState<Restricao[]>([]);
    const [solicitacoes, setSolicitacoes] = useState<SolicitacaoResumo[]>([]);
    const [total, setTotal] = useState(0);
    const [pagina, setPagina] = useState(1);

    const [filtroAluno, setFiltroAluno] = useState<number | "">("");
    const [filtroStatus, setFiltroStatus] = useState<StatusSolicitacao | "">("");

    const [alunoId, setAlunoId] = useState<number | null>(null);
    const [restricaoNome, setRestricaoNome] = useState("");
    const [anafilaxia, setAnafilaxia] = useState(false);
    const [descricao, setDescricao] = useState("");

    const [selecionada, setSelecionada] = useState<SolicitacaoDetalhe | null>(null);
    const [pedidoExistente, setPedidoExistente] = useState<number | null>(null);

    const [carregandoCatalogos, setCarregandoCatalogos] = useState(true);
    const [carregando, setCarregando] = useState(true);
    const [enviando, setEnviando] = useState(false);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [erroFilhos, setErroFilhos] = useState("");
    const [erroFilho, setErroFilho] = useState("");
    const [erroRestricao, setErroRestricao] = useState("");
    const [erroDescricao, setErroDescricao] = useState("");
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

        async function carregarCatalogos() {
            try {
                const [listaFilhos, listaRestricoes] = await Promise.all([
                    buscarAlunos(),
                    listarRestricoes(),
                ]);

                if (ativo) {
                    setFilhos(
                        [...listaFilhos].sort((a, b) =>
                            a.nome.localeCompare(b.nome, "pt-BR"),
                        ),
                    );
                    setRestricoes(listaRestricoes);
                }
            } catch (falha) {
                if (ativo) {
                    if (naoAutorizado(falha)) {
                        await logout();
                    } else {
                        setErroFilhos(
                            falha instanceof Error
                                ? falha.message
                                : "Não foi possível carregar os dados do pedido.",
                        );
                    }
                }
            } finally {
                if (ativo) {
                    setCarregandoCatalogos(false);
                }
            }
        }

        void carregarCatalogos();

        return () => {
            ativo = false;
        };
    }, [logout, tentativa]);

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const resposta = await listarSolicitacoes({
                tipo: "InclusaoAlergia",
                alunoId: filtroAluno === "" ? undefined : filtroAluno,
                status: filtroStatus,
                pagina,
                tamanho: TAMANHO_PAGINA,
            });

            setSolicitacoes(resposta.itens);
            setTotal(resposta.total);
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível carregar as solicitações.");
        } finally {
            setCarregando(false);
        }
    }, [filtroAluno, filtroStatus, pagina, tratarFalha]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    function limparFiltros() {
        setFiltroAluno("");
        setFiltroStatus("");
        setPagina(1);
        setSelecionada(null);
    }

    async function selecionar(id: number) {
        setErro("");
        setSucesso("");

        try {
            setSelecionada(await obterSolicitacao(id));
        } catch (falha) {
            setSelecionada(null);

            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            if (falha instanceof ErroRequisicao && falha.status === 403) {
                setErro("Acesso negado.");
                return;
            }

            if (falha instanceof ErroRequisicao && falha.status === 404) {
                setErro("Solicitação não encontrada.");
                return;
            }

            setErro(
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível carregar a solicitação.",
            );
        }
    }

    async function enviar() {
        setErro("");
        setErroFilho("");
        setErroRestricao("");
        setErroDescricao("");
        setSucesso("");
        setPedidoExistente(null);

        const texto = descricao.trim();
        let invalido = false;

        if (!alunoId) {
            setErroFilho("Escolha o filho.");
            invalido = true;
        }

        if (!restricaoNome) {
            setErroRestricao("Escolha a restrição ou alergia.");
            invalido = true;
        }

        if (texto.length < 5 || texto.length > 1000) {
            setErroDescricao("A descrição deve ter entre 5 e 1000 caracteres.");
            invalido = true;
        }

        if (invalido) {
            return;
        }

        setEnviando(true);

        try {
            await criarSolicitacao({
                tipo: "InclusaoAlergia",
                alunoId: alunoId ?? undefined,
                restricaoNome,
                descricao: texto,
                anafilaxia,
            });

            setRestricaoNome("");
            setAnafilaxia(false);
            setDescricao("");
            setSucesso("Solicitação enviada. Acompanhe a decisão da secretaria.");
            await carregar();
        } catch (falha) {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            const mensagem =
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível enviar a solicitação.";

            setErro(mensagem);

            if (falha instanceof ErroRequisicao && falha.status === 409) {
                const id = /#(\d+)/.exec(mensagem)?.[1];
                setPedidoExistente(id ? Number(id) : null);
                await carregar();
                setErro(mensagem);
            }
        } finally {
            setEnviando(false);
        }
    }

    async function cancelar() {
        if (!selecionada) {
            return;
        }

        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            setSelecionada(await cancelarSolicitacao(selecionada.id));
            setSucesso("Solicitação cancelada.");
            await carregar();
        } catch (falha) {
            if (falha instanceof ErroRequisicao && falha.status === 409) {
                const mensagem = falha.message;
                await carregar();
                setErro(mensagem);
            } else {
                await tratarFalha(falha, "Não foi possível cancelar a solicitação.");
            }
        } finally {
            setSalvando(false);
        }
    }

    async function marcarAnafilaxiaDoAluno() {
        if (!selecionada) {
            return;
        }

        const confirmado = window.confirm(
            "Confirmar que o aluno tem anafilaxia? A marcação fica registrada no histórico do aluno.",
        );

        if (!confirmado) return;

        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            setSelecionada(await marcarAnafilaxia(selecionada.id));
            setSucesso("Anafilaxia registrada para o aluno.");
            await carregar();
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível registrar a anafilaxia.");
        } finally {
            setSalvando(false);
        }
    }

    const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));
    const podeCancelar =
        selecionada !== null &&
        selecionada.status === "Aberta" &&
        selecionada.solicitanteId === userLogin?.id;

    // Spec 023/035: o responsável confirma a anafilaxia no próprio pedido de alergia.
    const podeMarcarAnafilaxia =
        selecionada !== null &&
        selecionada.tipo === "InclusaoAlergia" &&
        (selecionada.status === "Aberta" || selecionada.status === "EmAnalise");

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Solicitações de alergia</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Peça a inclusão de uma alergia/restrição e acompanhe a decisão da secretaria.
                </p>
            </header>

            {carregandoCatalogos ? (
                <p className="text-sm text-slate-500">Carregando dados do pedido...</p>
            ) : erroFilhos ? (
                <div className="rounded-2xl border border-red-200 bg-red-50 p-5">
                    <p role="alert" className="text-sm text-red-700">{erroFilhos}</p>
                    <button
                        type="button"
                        onClick={() => {
                            setErroFilhos("");
                            setCarregandoCatalogos(true);
                            setTentativa((atual) => atual + 1);
                        }}
                        className="mt-3 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white"
                    >
                        Tentar novamente
                    </button>
                </div>
            ) : filhos.length === 0 ? (
                <div className="rounded-2xl border border-dashed border-slate-300 bg-white/70 p-6 text-center">
                    <p className="text-sm text-slate-600">
                        Nenhum aluno vinculado à sua conta. As ações de aluno estão indisponíveis.
                    </p>
                    <Link
                        to="/pais"
                        className="mt-4 inline-block rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white"
                    >
                        Voltar para o início
                    </Link>
                </div>
            ) : (
                <>
                    <FormularioSolicitacao
                        filhos={filhos}
                        restricoes={restricoes}
                        alunoId={alunoId}
                        onAluno={setAlunoId}
                        restricaoNome={restricaoNome}
                        onRestricao={setRestricaoNome}
                        descricao={descricao}
                        onDescricao={setDescricao}
                        anafilaxia={anafilaxia}
                        onAnafilaxia={setAnafilaxia}
                        permitirAnafilaxia
                        enviando={enviando}
                        erro={erro}
                        erroFilho={erroFilho}
                        erroRestricao={erroRestricao}
                        erroDescricao={erroDescricao}
                        onSubmit={() => void enviar()}
                    />

                    {pedidoExistente && (
                        <button
                            type="button"
                            onClick={() => void selecionar(pedidoExistente)}
                            className="mt-3 rounded-xl bg-amber-50 px-4 py-2 text-sm font-semibold text-amber-800 underline"
                        >
                            Abrir pedido existente #{pedidoExistente}
                        </button>
                    )}

                    {sucesso && (
                        <p role="status" className="mt-4 rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">
                            {sucesso}
                        </p>
                    )}

                    <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                        <div className="grid gap-3 md:grid-cols-2">
                            <div className="flex flex-col gap-1">
                                <label htmlFor="solicitacao-filtro-filho" className="text-sm font-medium text-slate-700">
                                    Filho
                                </label>
                                <select
                                    id="solicitacao-filtro-filho"
                                    value={filtroAluno}
                                    onChange={(evento) => {
                                        setFiltroAluno(
                                            evento.target.value === ""
                                                ? ""
                                                : Number(evento.target.value),
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
                                <label htmlFor="solicitacao-filtro-status" className="text-sm font-medium text-slate-700">
                                    Status
                                </label>
                                <select
                                    id="solicitacao-filtro-status"
                                    value={filtroStatus}
                                    onChange={(evento) => {
                                        setFiltroStatus(
                                            evento.target.value as StatusSolicitacao | "",
                                        );
                                        setPagina(1);
                                    }}
                                    className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                                >
                                    <option value="">Todos os status</option>
                                    {Object.entries(rotuloStatusSolicitacao).map(
                                        ([valor, rotulo]) => (
                                            <option key={valor} value={valor}>
                                                {rotulo}
                                            </option>
                                        ),
                                    )}
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

                    <div className="mt-5 grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
                        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                            <h2 className="text-lg font-semibold text-roxo">Acompanhamento</h2>

                            <ListaSolicitacoes
                                itens={solicitacoes}
                                onSelecionar={(id) => void selecionar(id)}
                                carregando={carregando}
                                selecionadaId={selecionada?.id ?? null}
                            />

                            <div className="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-100 pt-3 text-sm text-slate-600">
                                <span>
                                    {total} solicitação(ões) · página {pagina} de {totalPaginas}
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

                        <aside>
                            {selecionada ? (
                                <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                                    <div className="flex items-start justify-between gap-3">
                                        <div>
                                            <h2 className="text-lg font-semibold text-roxo">
                                                Solicitação #{selecionada.id}
                                            </h2>
                                            <p className="text-sm text-slate-500">
                                                Inclusão de alergia
                                            </p>
                                        </div>
                                        <span
                                            className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatusSolicitacao[selecionada.status]}`}
                                        >
                                            {rotuloStatusSolicitacao[selecionada.status]}
                                        </span>
                                    </div>

                                    <dl className="mt-4 space-y-2 text-sm">
                                        <div>
                                            <dt className="font-medium text-slate-500">Aluno</dt>
                                            <dd className="text-slate-800">
                                                {selecionada.aluno ?? "Sem aluno"}
                                            </dd>
                                        </div>

                                        <div>
                                            <dt className="font-medium text-slate-500">Restrição</dt>
                                            <dd className="text-slate-800">
                                                {selecionada.restricaoNome ?? "—"}
                                            </dd>
                                        </div>

                                        {selecionada.anafilaxia && (
                                            <div>
                                                <dt className="font-medium text-slate-500">Anafilaxia</dt>
                                                <dd className="text-slate-800">
                                                    Informada pelo responsável
                                                </dd>
                                            </div>
                                        )}

                                        <div>
                                            <dt className="font-medium text-slate-500">Descrição</dt>
                                            <dd className="text-slate-800">{selecionada.descricao}</dd>
                                        </div>

                                        <div>
                                            <dt className="font-medium text-slate-500">Data</dt>
                                            <dd className="text-slate-800">
                                                {formatarDataSolicitacao(selecionada.criadoEm)}
                                            </dd>
                                        </div>

                                        {selecionada.resposta && (
                                            <div>
                                                <dt className="font-medium text-slate-500">Resposta</dt>
                                                <dd className="text-slate-800">
                                                    {selecionada.resposta}
                                                    {selecionada.respondidoPor
                                                        ? ` — ${selecionada.respondidoPor}`
                                                        : ""}
                                                </dd>
                                            </div>
                                        )}
                                    </dl>

                                    <div className="mt-4 flex flex-wrap gap-2">
                                        {podeMarcarAnafilaxia && (
                                            <button
                                                type="button"
                                                disabled={salvando}
                                                onClick={() => void marcarAnafilaxiaDoAluno()}
                                                className="rounded-xl border border-amber-300 bg-amber-50 px-4 py-2 text-sm font-semibold text-amber-900 disabled:opacity-50"
                                            >
                                                Marcar anafilaxia
                                            </button>
                                        )}

                                        {podeCancelar && (
                                            <button
                                                type="button"
                                                disabled={salvando}
                                                onClick={() => void cancelar()}
                                                className="rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50"
                                            >
                                                {salvando ? "Cancelando..." : "Cancelar solicitação"}
                                            </button>
                                        )}
                                    </div>
                                </article>
                            ) : (
                                <p className="rounded-2xl border border-dashed border-slate-300 bg-white/60 p-5 text-sm text-slate-500">
                                    Selecione uma solicitação para ver o status e a resposta.
                                </p>
                            )}
                        </aside>
                    </div>
                </>
            )}
        </section>
    );
}
