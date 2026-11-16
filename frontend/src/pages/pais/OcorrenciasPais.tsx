import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { ErroRequisicao, naoAutorizado } from "../../services/erroApi";
import {
    cancelarOcorrencia,
    listarOcorrencias,
    obterOcorrencia,
    type OcorrenciaDetalhe,
    type OcorrenciaResumo,
    type StatusOcorrencia,
    type TipoOcorrencia,
} from "../../services/OcorrenciaApi";
import {
    corStatusOcorrencia,
    formatarData,
    ListaOcorrencias,
    rotuloStatusOcorrencia,
    rotuloTipoOcorrencia,
} from "../../components/ListaOcorrencias";

const TAMANHO_PAGINA = 20;

const filtroAtivo = (
    alunoId: number | "",
    tipo: TipoOcorrencia | "",
    status: StatusOcorrencia | "",
    de: string,
    ate: string,
) => alunoId !== "" || tipo !== "" || status !== "" || de !== "" || ate !== "";

/**
 * Leitura e acompanhamento de ocorrências dos filhos (Spec 035, RF7–RF10/RF14). A API já
 * limita a consulta ao vínculo; a tela não amplia nem decide autorização.
 */
export function OcorrenciasPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [ocorrencias, setOcorrencias] = useState<OcorrenciaResumo[]>([]);
    const [total, setTotal] = useState(0);
    const [pagina, setPagina] = useState(1);

    const [filtroAluno, setFiltroAluno] = useState<number | "">("");
    const [filtroTipo, setFiltroTipo] = useState<TipoOcorrencia | "">("");
    const [filtroStatus, setFiltroStatus] = useState<StatusOcorrencia | "">("");
    const [de, setDe] = useState("");
    const [ate, setAte] = useState("");

    const [selecionada, setSelecionada] = useState<OcorrenciaDetalhe | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [sucesso, setSucesso] = useState("");

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
                    await tratarFalha(falha, "Não foi possível carregar seus filhos.");
                }
            }
        }

        void carregarFilhos();

        return () => {
            ativo = false;
        };
    }, [tratarFalha]);

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const resposta = await listarOcorrencias({
                alunoId: filtroAluno === "" ? undefined : filtroAluno,
                tipo: filtroTipo,
                status: filtroStatus,
                de: de || undefined,
                ate: ate || undefined,
                pagina,
                tamanho: TAMANHO_PAGINA,
            });

            setOcorrencias(resposta.itens);
            setTotal(resposta.total);
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível carregar as ocorrências.");
        } finally {
            setCarregando(false);
        }
    }, [filtroAluno, filtroTipo, filtroStatus, de, ate, pagina, tratarFalha]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    function limparFiltros() {
        setFiltroAluno("");
        setFiltroTipo("");
        setFiltroStatus("");
        setDe("");
        setAte("");
        setPagina(1);
        setSelecionada(null);
    }

    async function selecionar(id: number) {
        setErro("");
        setSucesso("");

        try {
            setSelecionada(await obterOcorrencia(id));
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
                setErro("Ocorrência não encontrada.");
                return;
            }

            setErro(
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível carregar a ocorrência.",
            );
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
            setSelecionada(await cancelarOcorrencia(selecionada.id));
            setSucesso("Ocorrência cancelada.");
            await carregar();
        } catch (falha) {
            if (falha instanceof ErroRequisicao && falha.status === 409) {
                const mensagem = falha.message;
                await carregar();
                setErro(mensagem);
            } else {
                await tratarFalha(falha, "Não foi possível cancelar a ocorrência.");
            }
        } finally {
            setSalvando(false);
        }
    }

    const totalPaginas = Math.max(1, Math.ceil(total / TAMANHO_PAGINA));
    const temFiltro = filtroAtivo(filtroAluno, filtroTipo, filtroStatus, de, ate);

    return (
        <section>
            <header className="mb-5 flex flex-wrap items-start justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold text-roxo">Ocorrências</h1>
                    <p className="mt-1 text-sm text-slate-600">
                        Acompanhe e registre ocorrências dos seus filhos.
                    </p>
                </div>

                {filhos.length > 0 && (
                    <Link
                        to="/pais/ocorrencias/nova"
                        className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                    >
                        Nova ocorrência
                    </Link>
                )}
            </header>

            {filhos.length === 0 && (
                <div className="mb-4 rounded-2xl border border-dashed border-slate-300 bg-white/70 p-5 text-sm text-slate-600">
                    Nenhum aluno vinculado à sua conta; as ações de aluno estão indisponíveis.
                </div>
            )}

            <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
                <div className="grid gap-3 md:grid-cols-5">
                    <div className="flex flex-col gap-1">
                        <label htmlFor="ocorrencia-filtro-filho" className="text-sm font-medium text-slate-700">
                            Filho
                        </label>
                        <select
                            id="ocorrencia-filtro-filho"
                            value={filtroAluno}
                            onChange={(evento) => {
                                setFiltroAluno(
                                    evento.target.value === "" ? "" : Number(evento.target.value),
                                );
                                setPagina(1);
                            }}
                            disabled={filhos.length === 0}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20 disabled:opacity-50"
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
                        <label htmlFor="ocorrencia-filtro-tipo" className="text-sm font-medium text-slate-700">
                            Tipo
                        </label>
                        <select
                            id="ocorrencia-filtro-tipo"
                            value={filtroTipo}
                            onChange={(evento) => {
                                setFiltroTipo(evento.target.value as TipoOcorrencia | "");
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        >
                            <option value="">Todos os tipos</option>
                            {Object.entries(rotuloTipoOcorrencia).map(([valor, rotulo]) => (
                                <option key={valor} value={valor}>
                                    {rotulo}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="ocorrencia-filtro-status" className="text-sm font-medium text-slate-700">
                            Status
                        </label>
                        <select
                            id="ocorrencia-filtro-status"
                            value={filtroStatus}
                            onChange={(evento) => {
                                setFiltroStatus(evento.target.value as StatusOcorrencia | "");
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        >
                            <option value="">Todos os status</option>
                            {Object.entries(rotuloStatusOcorrencia).map(([valor, rotulo]) => (
                                <option key={valor} value={valor}>
                                    {rotulo}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="ocorrencia-filtro-de" className="text-sm font-medium text-slate-700">
                            De
                        </label>
                        <input
                            id="ocorrencia-filtro-de"
                            type="date"
                            value={de}
                            onChange={(evento) => {
                                setDe(evento.target.value);
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        />
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="ocorrencia-filtro-ate" className="text-sm font-medium text-slate-700">
                            Até
                        </label>
                        <input
                            id="ocorrencia-filtro-ate"
                            type="date"
                            value={ate}
                            onChange={(evento) => {
                                setAte(evento.target.value);
                                setPagina(1);
                            }}
                            className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                        />
                    </div>
                </div>

                {temFiltro && (
                    <button
                        type="button"
                        onClick={limparFiltros}
                        className="mt-3 rounded-xl border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                    >
                        Limpar filtros
                    </button>
                )}
            </div>

            {erro && (
                <p role="alert" className="mt-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            {sucesso && (
                <p role="status" className="mt-4 rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">
                    {sucesso}
                </p>
            )}

            <div className="mt-5 grid gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
                <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                    <h2 className="text-lg font-semibold text-roxo">Acompanhamento</h2>

                    <ListaOcorrencias
                        itens={ocorrencias}
                        onSelecionar={(id) => void selecionar(id)}
                        carregando={carregando}
                        selecionadaId={selecionada?.id ?? null}
                    />

                    {!carregando && ocorrencias.length === 0 && !erro && temFiltro && (
                        <div className="mt-3">
                            <button
                                type="button"
                                onClick={limparFiltros}
                                className="rounded-xl border border-slate-300 px-3 py-1 text-sm font-semibold text-slate-700"
                            >
                                Limpar filtros
                            </button>
                        </div>
                    )}

                    <div className="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-100 pt-3 text-sm text-slate-600">
                        <span>
                            {total} ocorrência(s) · página {pagina} de {totalPaginas}
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
                                        Ocorrência #{selecionada.id}
                                    </h2>
                                    <p className="text-sm text-slate-500">
                                        {rotuloTipoOcorrencia[selecionada.tipo]}
                                    </p>
                                </div>
                                <span
                                    className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatusOcorrencia[selecionada.status]}`}
                                >
                                    {rotuloStatusOcorrencia[selecionada.status]}
                                </span>
                            </div>

                            <dl className="mt-4 space-y-2 text-sm">
                                <div>
                                    <dt className="font-medium text-slate-500">Descrição</dt>
                                    <dd className="text-slate-800">{selecionada.descricao}</dd>
                                </div>

                                <div>
                                    <dt className="font-medium text-slate-500">Aluno</dt>
                                    <dd className="text-slate-800">{selecionada.aluno ?? "Sem aluno"}</dd>
                                </div>

                                <div>
                                    <dt className="font-medium text-slate-500">Data</dt>
                                    <dd className="text-slate-800">{formatarData(selecionada.criadoEm)}</dd>
                                </div>

                                <div>
                                    <dt className="font-medium text-slate-500">Autor</dt>
                                    <dd className="text-slate-800">{selecionada.autor}</dd>
                                </div>
                            </dl>

                            {selecionada.podeCancelar && (
                                <button
                                    type="button"
                                    disabled={salvando}
                                    onClick={() => void cancelar()}
                                    className="mt-4 rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50"
                                >
                                    {salvando ? "Cancelando..." : "Cancelar ocorrência"}
                                </button>
                            )}

                            <div className="mt-5 border-t border-slate-100 pt-4">
                                <h3 className="text-sm font-semibold text-slate-700">Histórico</h3>
                                <ol className="mt-2 space-y-2">
                                    {selecionada.historico.map((evento, indice) => (
                                        <li
                                            key={`${evento.status}-${indice}`}
                                            className="text-xs text-slate-600"
                                        >
                                            <span className="font-semibold text-slate-800">
                                                {rotuloStatusOcorrencia[evento.status]}
                                            </span>{" "}
                                            por {evento.autor} em {formatarData(evento.criadoEm)}
                                            {evento.observacao ? ` — ${evento.observacao}` : ""}
                                        </li>
                                    ))}
                                </ol>
                            </div>
                        </article>
                    ) : (
                        <p className="rounded-2xl border border-dashed border-slate-300 bg-white/60 p-5 text-sm text-slate-500">
                            Selecione uma ocorrência para ver o detalhe e o histórico.
                        </p>
                    )}
                </aside>
            </div>
        </section>
    );
}
