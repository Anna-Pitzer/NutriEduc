import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { listarAlimentos, type Alimento } from "../../services/AlimentoApi";
import { listarCardapios, obterCardapio, type CardapioResumo, type RefeicaoCardapio } from "../../services/CardapioApi";
import { naoAutorizado } from "../../services/erroApi";
import {
    cancelarOcorrencia,
    criarOcorrencia,
    listarOcorrencias,
    mudarStatusOcorrencia,
    obterOcorrencia,
    type OcorrenciaDetalhe,
    type OcorrenciaResumo,
    type StatusOcorrencia,
    type TipoOcorrencia,
} from "../../services/OcorrenciaApi";

const rotuloTipo: Record<TipoOcorrencia, string> = {
    Aluno: "Aluno",
    Refeicao: "Refeição",
    Alimento: "Alimento",
    Outro: "Outro",
};

const rotuloStatus: Record<StatusOcorrencia, string> = {
    Aberta: "Aberta",
    EmAnalise: "Em análise",
    Resolvida: "Resolvida",
    Arquivada: "Arquivada",
    Cancelada: "Cancelada",
};

const corStatus: Record<StatusOcorrencia, string> = {
    Aberta: "bg-sky-50 text-sky-700",
    EmAnalise: "bg-amber-50 text-amber-700",
    Resolvida: "bg-emerald-50 text-emerald-700",
    Arquivada: "bg-slate-100 text-slate-600",
    Cancelada: "bg-red-50 text-red-700",
};

function tiposDeCriacao(papel: string | null): TipoOcorrencia[] {
    if (papel === "Responsavel") {
        return ["Aluno"];
    }

    if (papel === "Merendeira" || papel === "AdminEscola" || papel === "Nutricionista") {
        return ["Aluno", "Refeicao", "Alimento", "Outro"];
    }

    return [];
}

function podeMudarStatus(papel: string | null): boolean {
    return papel === "Nutricionista" || papel === "AdminEscola";
}

function formatarData(valor: string): string {
    return new Date(valor).toLocaleString("pt-BR");
}

export function OcorrenciasPage() {
    const { userLogin, logout } = useAuth();
    const papel = userLogin?.papel ?? null;

    const [searchParams] = useSearchParams();
    const alunoPreSelecionado = searchParams.get("alunoId");

    const [ocorrencias, setOcorrencias] = useState<OcorrenciaResumo[]>([]);
    const [selecionada, setSelecionada] = useState<OcorrenciaDetalhe | null>(null);
    const [alunos, setAlunos] = useState<AlunoResposta[]>([]);
    const [cardapios, setCardapios] = useState<CardapioResumo[]>([]);
    const [alimentos, setAlimentos] = useState<Alimento[]>([]);
    const [refeicoes, setRefeicoes] = useState<RefeicaoCardapio[]>([]);

    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [sucesso, setSucesso] = useState("");

    const [filtroStatus, setFiltroStatus] = useState<StatusOcorrencia | "">("");
    const [filtroTipo, setFiltroTipo] = useState<TipoOcorrencia | "">("");

    const [tipo, setTipo] = useState<TipoOcorrencia | "">(alunoPreSelecionado ? "Aluno" : "");
    const [descricao, setDescricao] = useState("");
    const [alunoId, setAlunoId] = useState(alunoPreSelecionado ?? "");
    const [cardapioId, setCardapioId] = useState("");
    const [refeicaoId, setRefeicaoId] = useState("");
    const [alimentoId, setAlimentoId] = useState("");
    const [observacao, setObservacao] = useState("");

    const tiposCriaveis = useMemo(() => tiposDeCriacao(papel), [papel]);

    const precisaAluno = tipo === "Aluno";
    const precisaRefeicao = tipo === "Refeicao";
    const precisaAlimento = tipo === "Alimento";

    const tratarFalha = useCallback(
        (falha: unknown) => {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErro(falha instanceof Error ? falha.message : "Não foi possível concluir a operação.");
        },
        [logout],
    );

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const pagina = await listarOcorrencias({
                status: filtroStatus,
                tipo: filtroTipo,
                tamanho: 100,
            });

            setOcorrencias(pagina.itens);
        } catch (falha) {
            tratarFalha(falha);
        } finally {
            setCarregando(false);
        }
    }, [filtroStatus, filtroTipo, tratarFalha]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    useEffect(() => {
        let ativo = true;

        Promise.all([
            buscarAlunos().catch(() => []),
            papel === "Responsavel" ? Promise.resolve({ itens: [] }) : listarCardapios({ tamanho: 100 }).catch(() => ({ itens: [] })),
            listarAlimentos({ tamanho: 100 }).catch(() => ({ itens: [] })),
        ]).then(([listaAlunos, paginaCardapios, paginaAlimentos]) => {
            if (!ativo) return;

            setAlunos(listaAlunos);
            setCardapios(paginaCardapios.itens);
            setAlimentos(paginaAlimentos.itens);
        });

        return () => {
            ativo = false;
        };
    }, [papel]);

    async function trocarCardapio(valor: string) {
        setCardapioId(valor);
        setRefeicaoId("");

        if (!valor) {
            setRefeicoes([]);
            return;
        }

        try {
            const detalhe = await obterCardapio(Number(valor));
            setRefeicoes(detalhe.refeicoes);
        } catch {
            setRefeicoes([]);
        }
    }

    async function selecionar(id: number) {
        setErro("");
        setSucesso("");

        try {
            const detalhe = await obterOcorrencia(id);
            setSelecionada(detalhe);
            setObservacao("");
        } catch (falha) {
            tratarFalha(falha);
        }
    }

    async function executar(acao: () => Promise<OcorrenciaDetalhe>, mensagem: string) {
        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            const detalhe = await acao();
            setSelecionada(detalhe);
            setObservacao("");
            setSucesso(mensagem);
            await carregar();
        } catch (falha) {
            tratarFalha(falha);
        } finally {
            setSalvando(false);
        }
    }

    async function criar() {
        if (!tipo || descricao.trim().length < 5) {
            setErro("Escolha o tipo e escreva uma descrição com pelo menos 5 caracteres.");
            return;
        }

        if (precisaAluno && !alunoId) {
            setErro("Escolha o aluno.");
            return;
        }

        if (precisaRefeicao && (!cardapioId || !refeicaoId)) {
            setErro("Escolha o cardápio e a refeição.");
            return;
        }

        if (precisaAlimento && !alimentoId) {
            setErro("Escolha o alimento.");
            return;
        }

        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            const criada = await criarOcorrencia({
                tipo,
                descricao: descricao.trim(),
                alunoId: precisaAluno ? Number(alunoId) : undefined,
                cardapioId: precisaRefeicao ? Number(cardapioId) : undefined,
                refeicaoId: precisaRefeicao ? Number(refeicaoId) : undefined,
                alimentoId: precisaAlimento ? Number(alimentoId) : undefined,
            });

            setTipo("");
            setDescricao("");
            setAlunoId("");
            setCardapioId("");
            setRefeicaoId("");
            setAlimentoId("");
            setRefeicoes([]);
            setSelecionada(criada);
            setSucesso("Ocorrência registrada.");
            await carregar();
        } catch (falha) {
            tratarFalha(falha);
        } finally {
            setSalvando(false);
        }
    }

    const podeDecidir = selecionada !== null && podeMudarStatus(papel);
    const observacaoValida = observacao.trim().length >= 2;

    return (
        <section className="p-6">
            <header className="mb-6">
                <h1 className="text-2xl font-bold text-roxo">Ocorrências</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Registro e acompanhamento de problemas de aluno, refeição e alimento.
                </p>
            </header>

            {erro && (
                <p role="alert" className="mb-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            {sucesso && (
                <p role="status" className="mb-4 rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">
                    {sucesso}
                </p>
            )}

            <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
                <div className="space-y-5">
                    {tiposCriaveis.length > 0 && (
                        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                            <h2 className="text-lg font-semibold text-roxo">Nova ocorrência</h2>

                            <div className="mt-4 grid gap-3 md:grid-cols-2">
                                <label className="text-sm font-medium text-slate-700">
                                    Tipo
                                    <select
                                        value={tipo}
                                        onChange={(evento) => setTipo(evento.target.value as TipoOcorrencia | "")}
                                        className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                    >
                                        <option value="">Selecione</option>
                                        {tiposCriaveis.map((valor) => (
                                            <option key={valor} value={valor}>
                                                {rotuloTipo[valor]}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                {precisaAluno && (
                                    <label className="text-sm font-medium text-slate-700">
                                        Aluno
                                        <select
                                            value={alunoId}
                                            onChange={(evento) => setAlunoId(evento.target.value)}
                                            className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                        >
                                            <option value="">Selecione</option>
                                            {alunos.map((aluno) => (
                                                <option key={aluno.id} value={aluno.id}>
                                                    {aluno.nome}
                                                </option>
                                            ))}
                                        </select>
                                    </label>
                                )}

                                {precisaRefeicao && (
                                    <>
                                        <label className="text-sm font-medium text-slate-700">
                                            Cardápio
                                            <select
                                                value={cardapioId}
                                                onChange={(evento) => void trocarCardapio(evento.target.value)}
                                                className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                            >
                                                <option value="">Selecione</option>
                                                {cardapios.map((cardapio) => (
                                                    <option key={cardapio.id} value={cardapio.id}>
                                                        {cardapio.nome}
                                                    </option>
                                                ))}
                                            </select>
                                        </label>

                                        <label className="text-sm font-medium text-slate-700">
                                            Refeição
                                            <select
                                                value={refeicaoId}
                                                onChange={(evento) => setRefeicaoId(evento.target.value)}
                                                disabled={!cardapioId}
                                                className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2 disabled:opacity-50"
                                            >
                                                <option value="">Selecione</option>
                                                {refeicoes.map((refeicao) => (
                                                    <option key={refeicao.id} value={refeicao.id}>
                                                        {refeicao.nome}
                                                    </option>
                                                ))}
                                            </select>
                                        </label>
                                    </>
                                )}

                                {precisaAlimento && (
                                    <label className="text-sm font-medium text-slate-700">
                                        Alimento
                                        <select
                                            value={alimentoId}
                                            onChange={(evento) => setAlimentoId(evento.target.value)}
                                            className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                        >
                                            <option value="">Selecione</option>
                                            {alimentos.map((alimento) => (
                                                <option key={alimento.id} value={alimento.id}>
                                                    {alimento.nome}
                                                </option>
                                            ))}
                                        </select>
                                    </label>
                                )}
                            </div>

                            <label className="mt-3 block text-sm font-medium text-slate-700">
                                Descrição
                                <textarea
                                    value={descricao}
                                    onChange={(evento) => setDescricao(evento.target.value)}
                                    rows={3}
                                    className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                    placeholder="Descreva o ocorrido com o contexto necessário."
                                />
                            </label>

                            <button
                                type="button"
                                onClick={() => void criar()}
                                disabled={salvando}
                                className="mt-4 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                            >
                                {salvando ? "Enviando..." : "Registrar ocorrência"}
                            </button>
                        </article>
                    )}

                    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <h2 className="text-lg font-semibold text-roxo">Acompanhamento</h2>

                            <div className="flex gap-2">
                                <select
                                    value={filtroStatus}
                                    onChange={(evento) => setFiltroStatus(evento.target.value as StatusOcorrencia | "")}
                                    className="rounded-xl border border-slate-300 px-3 py-2 text-sm"
                                >
                                    <option value="">Todos os status</option>
                                    {Object.entries(rotuloStatus).map(([valor, rotulo]) => (
                                        <option key={valor} value={valor}>{rotulo}</option>
                                    ))}
                                </select>

                                <select
                                    value={filtroTipo}
                                    onChange={(evento) => setFiltroTipo(evento.target.value as TipoOcorrencia | "")}
                                    className="rounded-xl border border-slate-300 px-3 py-2 text-sm"
                                >
                                    <option value="">Todos os tipos</option>
                                    {Object.entries(rotuloTipo).map(([valor, rotulo]) => (
                                        <option key={valor} value={valor}>{rotulo}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        {carregando ? (
                            <p className="mt-4 text-sm text-slate-500">Carregando...</p>
                        ) : ocorrencias.length === 0 ? (
                            <p className="mt-4 text-sm text-slate-500">Nenhuma ocorrência encontrada.</p>
                        ) : (
                            <ul className="mt-4 divide-y divide-slate-100">
                                {ocorrencias.map((item) => (
                                    <li key={item.id}>
                                        <button
                                            type="button"
                                            onClick={() => void selecionar(item.id)}
                                            className={`flex w-full flex-wrap items-center justify-between gap-2 px-2 py-3 text-left hover:bg-slate-50 ${
                                                selecionada?.id === item.id ? "rounded-xl bg-slate-100" : ""
                                            }`}
                                        >
                                            <span className="min-w-0">
                                                <span className="block text-sm font-semibold text-slate-800">
                                                    #{item.id} · {rotuloTipo[item.tipo]}
                                                </span>
                                                <span className="block truncate text-sm text-slate-600">
                                                    {item.descricao}
                                                </span>
                                                <span className="block text-xs text-slate-400">
                                                    {item.autor} · {formatarData(item.criadoEm)}
                                                </span>
                                            </span>

                                            <span className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatus[item.status]}`}>
                                                {rotuloStatus[item.status]}
                                            </span>
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </article>
                </div>

                <aside className="space-y-4">
                    {selecionada ? (
                        <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                            <div className="flex items-start justify-between gap-3">
                                <div>
                                    <h2 className="text-lg font-semibold text-roxo">
                                        Ocorrência #{selecionada.id}
                                    </h2>
                                    <p className="text-sm text-slate-500">{rotuloTipo[selecionada.tipo]}</p>
                                </div>

                                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatus[selecionada.status]}`}>
                                    {rotuloStatus[selecionada.status]}
                                </span>
                            </div>

                            <dl className="mt-4 space-y-2 text-sm">
                                <div>
                                    <dt className="font-medium text-slate-500">Descrição</dt>
                                    <dd className="text-slate-800">{selecionada.descricao}</dd>
                                </div>

                                {selecionada.aluno && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Aluno</dt>
                                        <dd className="text-slate-800">{selecionada.aluno}</dd>
                                    </div>
                                )}

                                {selecionada.cardapio && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Cardápio</dt>
                                        <dd className="text-slate-800">{selecionada.cardapio}</dd>
                                    </div>
                                )}

                                {selecionada.refeicao && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Refeição</dt>
                                        <dd className="text-slate-800">{selecionada.refeicao}</dd>
                                    </div>
                                )}

                                {selecionada.alimento && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Alimento</dt>
                                        <dd className="text-slate-800">{selecionada.alimento}</dd>
                                    </div>
                                )}

                                <div>
                                    <dt className="font-medium text-slate-500">Autor</dt>
                                    <dd className="text-slate-800">{selecionada.autor}</dd>
                                </div>
                            </dl>

                            {podeDecidir && !["Resolvida", "Arquivada", "Cancelada"].includes(selecionada.status) && (
                                <div className="mt-4 space-y-3 border-t border-slate-100 pt-4">
                                    <label className="block text-sm font-medium text-slate-700">
                                        Observação
                                        <textarea
                                            value={observacao}
                                            onChange={(evento) => setObservacao(evento.target.value)}
                                            rows={2}
                                            className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                        />
                                    </label>

                                    <div className="flex flex-wrap gap-2">
                                        {selecionada.status === "Aberta" && (
                                            <button
                                                type="button"
                                                disabled={salvando || !observacaoValida}
                                                onClick={() => void executar(
                                                    () => mudarStatusOcorrencia(selecionada.id, "EmAnalise", observacao),
                                                    "Ocorrência em análise.",
                                                )}
                                                className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                                            >
                                                Iniciar análise
                                            </button>
                                        )}

                                        {selecionada.status === "EmAnalise" && (
                                            <>
                                                <button
                                                    type="button"
                                                    disabled={salvando || !observacaoValida}
                                                    onClick={() => void executar(
                                                        () => mudarStatusOcorrencia(selecionada.id, "Resolvida", observacao),
                                                        "Ocorrência resolvida.",
                                                    )}
                                                    className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                                                >
                                                    Resolver
                                                </button>

                                                <button
                                                    type="button"
                                                    disabled={salvando || !observacaoValida}
                                                    onClick={() => void executar(
                                                        () => mudarStatusOcorrencia(selecionada.id, "Aberta", observacao),
                                                        "Ocorrência reaberta.",
                                                    )}
                                                    className="rounded-xl border border-roxo/40 px-4 py-2 text-sm font-semibold text-roxo disabled:opacity-50"
                                                >
                                                    Reabrir
                                                </button>
                                            </>
                                        )}
                                    </div>
                                </div>
                            )}

                            {selecionada.podeCancelar && (
                                <button
                                    type="button"
                                    disabled={salvando}
                                    onClick={() => void executar(
                                        () => cancelarOcorrencia(selecionada.id),
                                        "Ocorrência cancelada.",
                                    )}
                                    className="mt-4 rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50"
                                >
                                    Cancelar ocorrência
                                </button>
                            )}

                            <div className="mt-5 border-t border-slate-100 pt-4">
                                <h3 className="text-sm font-semibold text-slate-700">Histórico</h3>
                                <ol className="mt-2 space-y-2">
                                    {selecionada.historico.map((evento, indice) => (
                                        <li key={`${evento.status}-${indice}`} className="text-xs text-slate-600">
                                            <span className="font-semibold text-slate-800">
                                                {rotuloStatus[evento.status]}
                                            </span>{" "}
                                            por {evento.autor} em {formatarData(evento.criadoEm)}
                                            {evento.observacao ? ` — ${evento.observacao}` : ""}
                                        </li>
                                    ))}
                                </ol>
                            </div>
                        </article>
                    ) : (
                        <article className="rounded-2xl border border-dashed border-slate-300 bg-white/60 p-5 text-sm text-slate-500">
                            Selecione uma ocorrência para ver o detalhe, o histórico e as ações.
                        </article>
                    )}
                </aside>
            </div>
        </section>
    );
}
