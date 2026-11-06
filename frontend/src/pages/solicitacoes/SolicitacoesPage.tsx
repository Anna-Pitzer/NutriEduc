import { useCallback, useEffect, useMemo, useState } from "react";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { listarCardapios, type CardapioResumo } from "../../services/CardapioApi";
import { listarRestricoes, type Restricao } from "../../services/RestricaoApi";
import { naoAutorizado } from "../../services/erroApi";
import {
    analisarSolicitacao,
    aprovarSolicitacao,
    cancelarSolicitacao,
    concluirSolicitacao,
    criarSolicitacao,
    listarSolicitacoes,
    obterSolicitacao,
    recusarSolicitacao,
    type SolicitacaoDetalhe,
    type SolicitacaoResumo,
    type StatusSolicitacao,
    type TipoSolicitacao,
} from "../../services/SolicitacaoApi";

const rotuloTipo: Record<TipoSolicitacao, string> = {
    MudancaCardapio: "Mudança de cardápio",
    Ingrediente: "Ingrediente",
    FaltaIngrediente: "Falta de ingrediente",
    InclusaoAlergia: "Inclusão de alergia",
    AlteracaoRestricao: "Alteração de restrição",
    DadosCadastrais: "Dados cadastrais",
};

const rotuloStatus: Record<StatusSolicitacao, string> = {
    Aberta: "Aberta",
    EmAnalise: "Em análise",
    Aprovada: "Aprovada",
    Recusada: "Recusada",
    Concluida: "Concluída",
    Cancelada: "Cancelada",
};

const corStatus: Record<StatusSolicitacao, string> = {
    Aberta: "bg-sky-50 text-sky-700",
    EmAnalise: "bg-amber-50 text-amber-700",
    Aprovada: "bg-emerald-50 text-emerald-700",
    Recusada: "bg-red-50 text-red-700",
    Concluida: "bg-emerald-100 text-emerald-800",
    Cancelada: "bg-slate-100 text-slate-600",
};

const statusFinal = new Set<StatusSolicitacao>(["Recusada", "Concluida", "Cancelada"]);

const camposDados = [
    { chave: "nome", rotulo: "Nome" },
    { chave: "dataNascimento", rotulo: "Data de nascimento" },
    { chave: "nomeResponsavel", rotulo: "Nome do responsável" },
    { chave: "telefoneResponsavel", rotulo: "Telefone do responsável" },
    { chave: "observacao", rotulo: "Observação" }
];

const rotuloCampo: Record<string, string> = {
    Nome: "Nome",
    DataNascimento: "Data de nascimento",
    NomeResponsavel: "Nome do responsável",
    TelefoneResponsavel: "Telefone do responsável",
    Observacao: "Observação"
};

function tiposDeCriacao(papel: string | null): TipoSolicitacao[] {
    if (papel === "Responsavel") {
        return ["InclusaoAlergia", "AlteracaoRestricao", "DadosCadastrais"];
    }

    if (papel === "Merendeira" || papel === "AdminEscola" || papel === "Nutricionista") {
        return ["MudancaCardapio", "Ingrediente", "FaltaIngrediente"];
    }

    return [];
}

function ehDecisor(papel: string | null, tipo: TipoSolicitacao): boolean {
    if (tipo === "InclusaoAlergia" || tipo === "AlteracaoRestricao" || tipo === "DadosCadastrais") {
        return papel === "AdminSecretaria";
    }

    return papel === "Nutricionista";
}

function formatarData(valor: string): string {
    return new Date(valor).toLocaleString("pt-BR");
}

export function SolicitacoesPage() {
    const { userLogin, logout } = useAuth();
    const papel = userLogin?.papel ?? null;

    const [solicitacoes, setSolicitacoes] = useState<SolicitacaoResumo[]>([]);
    const [selecionada, setSelecionada] = useState<SolicitacaoDetalhe | null>(null);
    const [alunos, setAlunos] = useState<AlunoResposta[]>([]);
    const [cardapios, setCardapios] = useState<CardapioResumo[]>([]);
    const [restricoes, setRestricoes] = useState<Restricao[]>([]);

    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [sucesso, setSucesso] = useState("");

    const [filtroStatus, setFiltroStatus] = useState<StatusSolicitacao | "">("");
    const [filtroTipo, setFiltroTipo] = useState<TipoSolicitacao | "">("");

    const [tipo, setTipo] = useState<TipoSolicitacao | "">("");
    const [descricao, setDescricao] = useState("");
    const [alunoId, setAlunoId] = useState("");
    const [cardapioId, setCardapioId] = useState("");
    const [restricaoNome, setRestricaoNome] = useState("");
    const [ingrediente, setIngrediente] = useState("");
    const [alteracoesDados, setAlteracoesDados] = useState<
        { campo: string; valor: string }[]
    >([{ campo: "nome", valor: "" }]);
    const [resposta, setResposta] = useState("");

    const tiposCriaveis = useMemo(() => tiposDeCriacao(papel), [papel]);

    const precisaDados = tipo === "DadosCadastrais";
    const precisaAluno =
        tipo === "InclusaoAlergia" ||
        tipo === "AlteracaoRestricao" ||
        precisaDados;
    const precisaCardapio = tipo === "MudancaCardapio";
    const precisaIngrediente = tipo === "Ingrediente" || tipo === "FaltaIngrediente";
    const precisaRestricao = precisaAluno;

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
            const pagina = await listarSolicitacoes({
                status: filtroStatus,
                tipo: filtroTipo,
                tamanho: 100,
            });

            setSolicitacoes(pagina.itens);
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
            papel === "Responsavel" ? Promise.resolve([]) : listarCardapios({ tamanho: 100 }).catch(() => ({ itens: [] })),
            listarRestricoes().catch(() => []),
        ]).then(([listaAlunos, paginaCardapios, listaRestricoes]) => {
            if (!ativo) return;

            setAlunos(listaAlunos);
            setCardapios("itens" in paginaCardapios ? paginaCardapios.itens : []);
            setRestricoes(listaRestricoes);
        });

        return () => {
            ativo = false;
        };
    }, [papel]);

    async function selecionar(id: number) {
        setErro("");
        setSucesso("");

        try {
            const detalhe = await obterSolicitacao(id);
            setSelecionada(detalhe);
            setResposta(detalhe.resposta ?? "");
        } catch (falha) {
            tratarFalha(falha);
        }
    }

    async function executar(acao: () => Promise<SolicitacaoDetalhe>, mensagem: string) {
        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            const detalhe = await acao();
            setSelecionada(detalhe);
            setResposta(detalhe.resposta ?? "");
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

        if (precisaDados && descricao.trim().length < 10) {
            setErro("A justificativa precisa ter pelo menos 10 caracteres.");
            return;
        }

        if (precisaDados) {
            const preenchidas = alteracoesDados.filter(
                (alteracao) => alteracao.campo && alteracao.valor.trim().length > 0,
            );

            if (preenchidas.length === 0) {
                setErro("Informe ao menos um campo e o novo valor.");
                return;
            }

            if (new Set(preenchidas.map((alteracao) => alteracao.campo)).size !== preenchidas.length) {
                setErro("O mesmo campo não pode aparecer duas vezes.");
                return;
            }
        }

        if (precisaAluno && !alunoId) {
            setErro("Escolha o aluno.");
            return;
        }

        if (precisaCardapio && !cardapioId) {
            setErro("Escolha o cardápio.");
            return;
        }

        if (precisaIngrediente && ingrediente.trim().length < 2) {
            setErro("Informe o ingrediente.");
            return;
        }

        if (precisaRestricao && restricaoNome.trim().length < 2) {
            setErro("Escolha a restrição.");
            return;
        }

        setSalvando(true);
        setErro("");
        setSucesso("");

        try {
            const criada = await criarSolicitacao({
                tipo,
                descricao,
                alunoId: precisaAluno ? Number(alunoId) : undefined,
                cardapioId: precisaCardapio ? Number(cardapioId) : undefined,
                restricaoNome: precisaRestricao ? restricaoNome : undefined,
                ingrediente: precisaIngrediente ? ingrediente : undefined,
                justificativa: precisaDados ? descricao : undefined,
                alteracoes: precisaDados
                    ? alteracoesDados
                        .filter((alteracao) => alteracao.campo && alteracao.valor.trim().length > 0)
                        .map((alteracao) => ({
                            campo: alteracao.campo,
                            valorSolicitado: alteracao.valor.trim()
                        }))
                    : undefined
            });

            setTipo("");
            setDescricao("");
            setAlunoId("");
            setCardapioId("");
            setRestricaoNome("");
            setIngrediente("");
            setAlteracoesDados([{ campo: "nome", valor: "" }]);
            setSelecionada(criada);
            setSucesso("Solicitação criada.");
            await carregar();
        } catch (falha) {
            tratarFalha(falha);
        } finally {
            setSalvando(false);
        }
    }

    const podeCancelar =
        selecionada !== null &&
        selecionada.status === "Aberta" &&
        selecionada.solicitanteId === userLogin?.id;

    const podeDecidir = selecionada !== null && ehDecisor(papel, selecionada.tipo);

    return (
        <section className="p-6">
            <header className="mb-6">
                <h1 className="text-2xl font-bold text-roxo">Solicitações</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Pedidos de cardápio, ingrediente, restrição e dados cadastrais, com decisão registrada.
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
                            <h2 className="text-lg font-semibold text-roxo">Nova solicitação</h2>

                            <div className="mt-4 grid gap-3 md:grid-cols-2">
                                <label className="text-sm font-medium text-slate-700">
                                    Tipo
                                    <select
                                        value={tipo}
                                        onChange={(evento) => setTipo(evento.target.value as TipoSolicitacao | "")}
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

                                {precisaCardapio && (
                                    <label className="text-sm font-medium text-slate-700">
                                        Cardápio
                                        <select
                                            value={cardapioId}
                                            onChange={(evento) => setCardapioId(evento.target.value)}
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
                                )}

                                {precisaRestricao && (
                                    <label className="text-sm font-medium text-slate-700">
                                        Restrição
                                        <select
                                            value={restricaoNome}
                                            onChange={(evento) => setRestricaoNome(evento.target.value)}
                                            className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                        >
                                            <option value="">Selecione</option>
                                            {restricoes
                                                .filter((restricao) => restricao.ativa)
                                                .map((restricao) => (
                                                    <option key={restricao.id} value={restricao.nome}>
                                                        {restricao.nome} ({restricao.categoria})
                                                    </option>
                                                ))}
                                        </select>
                                    </label>
                                )}

                                {precisaIngrediente && (
                                    <label className="text-sm font-medium text-slate-700">
                                        Ingrediente
                                        <input
                                            value={ingrediente}
                                            onChange={(evento) => setIngrediente(evento.target.value)}
                                            className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                            placeholder="Ex.: leite de coco"
                                        />
                                    </label>
                                )}
                            </div>

                            {precisaDados && (
                                <div className="mt-4 space-y-3">
                                    <p className="text-sm font-medium text-slate-700">Campos a corrigir</p>

                                    {alteracoesDados.map((alteracao, indice) => (
                                        <div
                                            key={`${indice}-${alteracao.campo}`}
                                            className="grid gap-2 md:grid-cols-[220px_1fr_auto]"
                                        >
                                            <select
                                                value={alteracao.campo}
                                                onChange={(evento) => setAlteracoesDados((atuais) =>
                                                    atuais.map((item, posicao) =>
                                                        posicao === indice
                                                            ? { ...item, campo: evento.target.value }
                                                            : item,
                                                    ),
                                                )}
                                                className="rounded-xl border border-slate-300 px-3 py-2 text-sm"
                                            >
                                                {camposDados.map((campo) => (
                                                    <option key={campo.chave} value={campo.chave}>
                                                        {campo.rotulo}
                                                    </option>
                                                ))}
                                            </select>

                                            <input
                                                value={alteracao.valor}
                                                onChange={(evento) => setAlteracoesDados((atuais) =>
                                                    atuais.map((item, posicao) =>
                                                        posicao === indice
                                                            ? { ...item, valor: evento.target.value }
                                                            : item,
                                                    ),
                                                )}
                                                className="rounded-xl border border-slate-300 px-3 py-2 text-sm"
                                                placeholder="Novo valor"
                                            />

                                            <button
                                                type="button"
                                                disabled={alteracoesDados.length === 1}
                                                onClick={() => setAlteracoesDados((atuais) =>
                                                    atuais.filter((_, posicao) => posicao !== indice),
                                                )}
                                                className="rounded-xl border border-slate-300 px-3 py-2 text-sm text-slate-600 disabled:opacity-40"
                                            >
                                                Remover
                                            </button>
                                        </div>
                                    ))}

                                    <button
                                        type="button"
                                        onClick={() => setAlteracoesDados((atuais) => [
                                            ...atuais,
                                            { campo: "observacao", valor: "" }
                                        ])}
                                        className="rounded-xl border border-roxo/40 px-3 py-2 text-sm font-semibold text-roxo"
                                    >
                                        Adicionar campo
                                    </button>
                                </div>
                            )}

                            <label className="mt-3 block text-sm font-medium text-slate-700">
                                {precisaDados ? "Justificativa" : "Descrição"}
                                <textarea
                                    value={descricao}
                                    onChange={(evento) => setDescricao(evento.target.value)}
                                    rows={3}
                                    className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                    placeholder={
                                        precisaDados
                                            ? "Explique por que a correção é necessária."
                                            : "Explique o pedido com o contexto necessário."
                                    }
                                />
                            </label>

                            <button
                                type="button"
                                onClick={() => void criar()}
                                disabled={salvando}
                                className="mt-4 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                            >
                                {salvando ? "Enviando..." : "Enviar solicitação"}
                            </button>
                        </article>
                    )}

                    <article className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <h2 className="text-lg font-semibold text-roxo">Acompanhamento</h2>

                            <div className="flex gap-2">
                                <select
                                    value={filtroStatus}
                                    onChange={(evento) => setFiltroStatus(evento.target.value as StatusSolicitacao | "")}
                                    className="rounded-xl border border-slate-300 px-3 py-2 text-sm"
                                >
                                    <option value="">Todos os status</option>
                                    {Object.entries(rotuloStatus).map(([valor, rotulo]) => (
                                        <option key={valor} value={valor}>{rotulo}</option>
                                    ))}
                                </select>

                                <select
                                    value={filtroTipo}
                                    onChange={(evento) => setFiltroTipo(evento.target.value as TipoSolicitacao | "")}
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
                        ) : solicitacoes.length === 0 ? (
                            <p className="mt-4 text-sm text-slate-500">Nenhuma solicitação encontrada.</p>
                        ) : (
                            <ul className="mt-4 divide-y divide-slate-100">
                                {solicitacoes.map((item) => (
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
                                                    {item.solicitante} · {formatarData(item.criadoEm)}
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
                                        Solicitação #{selecionada.id}
                                    </h2>
                                    <p className="text-sm text-slate-500">{rotuloTipo[selecionada.tipo]}</p>
                                </div>

                                <span className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatus[selecionada.status]}`}>
                                    {rotuloStatus[selecionada.status]}
                                </span>
                            </div>

                            <dl className="mt-4 space-y-2 text-sm">
                                <div>
                                    <dt className="font-medium text-slate-500">
                                        {selecionada.tipo === "DadosCadastrais" ? "Justificativa" : "Descrição"}
                                    </dt>
                                    <dd className="text-slate-800">{selecionada.descricao}</dd>
                                </div>

                                {selecionada.alteracoes.length > 0 && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Alterações pedidas</dt>
                                        <dd className="text-slate-800">
                                            <ul className="space-y-1">
                                                {selecionada.alteracoes.map((alteracao, indice) => (
                                                    <li key={`${alteracao.campo}-${indice}`}>
                                                        <span className="font-medium">
                                                            {rotuloCampo[alteracao.campo] ?? alteracao.campo}:
                                                        </span>{" "}
                                                        {alteracao.valorNoPedido || "(vazio)"} →{" "}
                                                        {alteracao.valorSolicitado || "(vazio)"}
                                                    </li>
                                                ))}
                                            </ul>
                                        </dd>
                                    </div>
                                )}

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

                                {selecionada.restricaoNome && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Restrição</dt>
                                        <dd className="text-slate-800">{selecionada.restricaoNome}</dd>
                                    </div>
                                )}

                                {selecionada.ingrediente && (
                                    <div>
                                        <dt className="font-medium text-slate-500">Ingrediente</dt>
                                        <dd className="text-slate-800">{selecionada.ingrediente}</dd>
                                    </div>
                                )}

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

                            {selecionada.aviso && (
                                <p className="mt-3 rounded-xl bg-sky-50 px-3 py-2 text-xs text-sky-800">
                                    {selecionada.aviso}
                                </p>
                            )}

                            {podeDecidir && !statusFinal.has(selecionada.status) && (
                                <div className="mt-4 space-y-3 border-t border-slate-100 pt-4">
                                    {selecionada.status === "EmAnalise" && (
                                        <label className="block text-sm font-medium text-slate-700">
                                            Resposta da decisão
                                            <textarea
                                                value={resposta}
                                                onChange={(evento) => setResposta(evento.target.value)}
                                                rows={2}
                                                className="mt-1 w-full rounded-xl border border-slate-300 px-3 py-2"
                                            />
                                        </label>
                                    )}

                                    <div className="flex flex-wrap gap-2">
                                        {selecionada.status === "Aberta" && (
                                            <button
                                                type="button"
                                                disabled={salvando}
                                                onClick={() => void executar(
                                                    () => analisarSolicitacao(selecionada.id),
                                                    "Solicitação em análise.",
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
                                                    disabled={salvando}
                                                    onClick={() => void executar(
                                                        () => aprovarSolicitacao(selecionada.id, resposta),
                                                        "Solicitação aprovada.",
                                                    )}
                                                    className="rounded-xl bg-emerald-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                                                >
                                                    Aprovar
                                                </button>

                                                <button
                                                    type="button"
                                                    disabled={salvando}
                                                    onClick={() => void executar(
                                                        () => recusarSolicitacao(selecionada.id, resposta),
                                                        "Solicitação recusada.",
                                                    )}
                                                    className="rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                                                >
                                                    Recusar
                                                </button>
                                            </>
                                        )}

                                        {selecionada.status === "Aprovada" && (
                                            <button
                                                type="button"
                                                disabled={salvando}
                                                onClick={() => void executar(
                                                    () => concluirSolicitacao(selecionada.id),
                                                    "Solicitação concluída.",
                                                )}
                                                className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                                            >
                                                Concluir
                                            </button>
                                        )}
                                    </div>
                                </div>
                            )}

                            {podeCancelar && (
                                <button
                                    type="button"
                                    disabled={salvando}
                                    onClick={() => void executar(
                                        () => cancelarSolicitacao(selecionada.id),
                                        "Solicitação cancelada.",
                                    )}
                                    className="mt-4 rounded-xl border border-red-300 px-4 py-2 text-sm font-semibold text-red-700 disabled:opacity-50"
                                >
                                    Cancelar solicitação
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
                                            {evento.resposta ? ` — ${evento.resposta}` : ""}
                                        </li>
                                    ))}
                                </ol>
                            </div>
                        </article>
                    ) : (
                        <article className="rounded-2xl border border-dashed border-slate-300 bg-white/60 p-5 text-sm text-slate-500">
                            Selecione uma solicitação para ver o detalhe, o histórico e as ações.
                        </article>
                    )}
                </aside>
            </div>
        </section>
    );
}
