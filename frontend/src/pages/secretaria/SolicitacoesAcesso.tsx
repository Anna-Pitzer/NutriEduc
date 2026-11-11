import { useEffect, useState, type FormEvent } from "react";
import { LayoutSecretaria } from "./layoutSecretaria";
import {
    aprovarSolicitacaoAcesso,
    listarSolicitacoesAcesso,
    recusarSolicitacaoAcesso,
    type SolicitacaoAcessoResumo,
} from "../../services/SolicitacaoAcessoApi";
import { listarEscolas, type EscolaResposta } from "../../services/EscolaApi";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";

type FiltroStatus = "" | "Pendente" | "Aprovada" | "Recusada";
type ModoDecisao = "aprovar" | "recusar";

export function SolicitacoesAcesso() {
    const [solicitacoes, setSolicitacoes] = useState<SolicitacaoAcessoResumo[]>([]);
    const [total, setTotal] = useState(0);
    const [status, setStatus] = useState<FiltroStatus>("Pendente");
    const [tipo, setTipo] = useState("");
    const [busca, setBusca] = useState("");
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

    const [escolas, setEscolas] = useState<EscolaResposta[]>([]);
    const [alunos, setAlunos] = useState<AlunoResposta[]>([]);

    const [emDecisao, setEmDecisao] = useState<SolicitacaoAcessoResumo | null>(null);
    const [modoDecisao, setModoDecisao] = useState<ModoDecisao>("aprovar");
    const [escolaId, setEscolaId] = useState<number | "">("");
    const [alunosIds, setAlunosIds] = useState<number[]>([]);
    const [motivo, setMotivo] = useState("");
    const [salvando, setSalvando] = useState(false);

    async function carregar() {
        setCarregando(true);
        setErro("");

        try {
            const pagina = await listarSolicitacoesAcesso({ status, tipo, busca });
            setSolicitacoes(pagina.itens);
            setTotal(pagina.total);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Não foi possível carregar a fila.");
        } finally {
            setCarregando(false);
        }
    }

    useEffect(() => {
        void carregar();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [status, tipo]);

    async function prepararAprovacao(solicitacao: SolicitacaoAcessoResumo) {
        setErro("");
        setEmDecisao(solicitacao);
        setModoDecisao("aprovar");
        setEscolaId("");
        setAlunosIds([]);
        setMotivo("");

        if (solicitacao.tipoSolicitado === "Merendeira") {
            setEscolas(await listarEscolas().catch(() => []));
        }

        if (solicitacao.tipoSolicitado === "Responsavel") {
            setAlunos(await buscarAlunos().catch(() => []));
        }
    }

    function prepararRecusa(solicitacao: SolicitacaoAcessoResumo) {
        setErro("");
        setEmDecisao(solicitacao);
        setModoDecisao("recusar");
        setEscolaId("");
        setAlunosIds([]);
        setMotivo("");
    }

    function alternarAluno(id: number) {
        setAlunosIds((atuais) =>
            atuais.includes(id) ? atuais.filter((item) => item !== id) : [...atuais, id],
        );
    }

    async function confirmarAprovacao(e: FormEvent) {
        e.preventDefault();
        if (!emDecisao || salvando) return;

        if (emDecisao.tipoSolicitado === "Merendeira" && escolaId === "") {
            setErro("Selecione a escola da merendeira.");
            return;
        }

        if (emDecisao.tipoSolicitado === "Responsavel" && alunosIds.length === 0) {
            setErro("Selecione ao menos um aluno para vincular.");
            return;
        }

        setSalvando(true);

        try {
            await aprovarSolicitacaoAcesso(emDecisao.id, {
                escolaId: emDecisao.tipoSolicitado === "Merendeira" ? Number(escolaId) : null,
                alunosIds: emDecisao.tipoSolicitado === "Responsavel" ? alunosIds : undefined,
            });

            setEmDecisao(null);
            await carregar();
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Não foi possível aprovar.");
        } finally {
            setSalvando(false);
        }
    }

    async function confirmarRecusa(e: FormEvent) {
        e.preventDefault();
        if (!emDecisao || salvando) return;

        if (motivo.trim().length < 5) {
            setErro("O motivo deve ter ao menos 5 caracteres.");
            return;
        }

        setSalvando(true);

        try {
            await recusarSolicitacaoAcesso(emDecisao.id, motivo.trim());
            setEmDecisao(null);
            await carregar();
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Não foi possível recusar.");
        } finally {
            setSalvando(false);
        }
    }

    function aoSubmeter(e: FormEvent) {
        if (modoDecisao === "aprovar") {
            void confirmarAprovacao(e);
        } else {
            void confirmarRecusa(e);
        }
    }

    return (
        <LayoutSecretaria>
            <div className="space-y-4">
                <div className="flex flex-wrap items-center gap-3">
                    <h2 className="text-xl font-semibold text-gray-800">Solicitações de acesso</h2>
                    <span className="text-sm text-gray-500">{total} registro(s)</span>
                </div>

                <div className="flex flex-wrap gap-3">
                    <select
                        value={status}
                        onChange={(e) => setStatus(e.target.value as FiltroStatus)}
                        className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                    >
                        <option value="Pendente">Pendentes</option>
                        <option value="Aprovada">Aprovadas</option>
                        <option value="Recusada">Recusadas</option>
                        <option value="">Todas</option>
                    </select>

                    <select
                        value={tipo}
                        onChange={(e) => setTipo(e.target.value)}
                        className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                    >
                        <option value="">Todos os tipos</option>
                        <option value="Nutricionista">Nutricionista</option>
                        <option value="Merendeira">Merendeira</option>
                        <option value="Responsavel">Responsavel</option>
                    </select>

                    <input
                        type="text"
                        placeholder="Buscar por nome, e-mail ou CPF"
                        value={busca}
                        onChange={(e) => setBusca(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") void carregar();
                        }}
                        className="rounded-lg border border-gray-300 px-3 py-2 text-sm"
                    />
                </div>

                {erro && <p className="text-sm text-red-600">{erro}</p>}

                {carregando ? (
                    <p className="text-sm text-gray-500">Carregando…</p>
                ) : solicitacoes.length === 0 ? (
                    <p className="text-sm text-gray-500">Nenhuma solicitação encontrada.</p>
                ) : (
                    <div className="space-y-3">
                        {solicitacoes.map((solicitacao) => (
                            <div
                                key={solicitacao.id}
                                className="rounded-xl border border-gray-200 bg-white p-4"
                            >
                                <div className="flex flex-wrap items-center justify-between gap-2">
                                    <div>
                                        <p className="font-medium text-gray-800">
                                            {solicitacao.nome}{" "}
                                            <span className="text-xs text-gray-500">
                                                #{solicitacao.id}
                                            </span>
                                        </p>
                                        <p className="text-sm text-gray-500">
                                            {solicitacao.tipoSolicitado} · {solicitacao.email} ·{" "}
                                            {solicitacao.cpf}
                                        </p>
                                    </div>
                                    <span className="rounded-full bg-gray-100 px-3 py-1 text-xs font-medium text-gray-600">
                                        {solicitacao.status}
                                    </span>
                                </div>

                                {solicitacao.status === "Pendente" && (
                                    <div className="mt-3 flex gap-2">
                                        <button
                                            type="button"
                                            onClick={() => void prepararAprovacao(solicitacao)}
                                            className="rounded-lg bg-verdeEscuro px-4 py-2 text-sm font-medium text-white"
                                        >
                                            Aprovar
                                        </button>
                                        <button
                                            type="button"
                                            onClick={() => prepararRecusa(solicitacao)}
                                            className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white"
                                        >
                                            Recusar
                                        </button>
                                    </div>
                                )}

                                {emDecisao?.id === solicitacao.id && (
                                    <form
                                        onSubmit={aoSubmeter}
                                        className="mt-3 space-y-3 rounded-lg bg-gray-50 p-3"
                                    >
                                        {modoDecisao === "aprovar" &&
                                            solicitacao.tipoSolicitado === "Merendeira" && (
                                                <select
                                                    value={escolaId}
                                                    onChange={(e) =>
                                                        setEscolaId(
                                                            e.target.value === "" ? "" : Number(e.target.value),
                                                        )
                                                    }
                                                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
                                                >
                                                    <option value="">Selecione a escola</option>
                                                    {escolas.map((escola) => (
                                                        <option key={escola.id} value={escola.id}>
                                                            {escola.nome}
                                                        </option>
                                                    ))}
                                                </select>
                                            )}

                                        {modoDecisao === "aprovar" &&
                                            solicitacao.tipoSolicitado === "Responsavel" && (
                                                <div className="space-y-1">
                                                    <p className="text-sm text-gray-600">Vincular alunos:</p>
                                                    {alunos.map((aluno) => (
                                                        <label
                                                            key={aluno.id}
                                                            className="flex items-center gap-2 text-sm text-gray-700"
                                                        >
                                                            <input
                                                                type="checkbox"
                                                                checked={alunosIds.includes(aluno.id)}
                                                                onChange={() => alternarAluno(aluno.id)}
                                                            />
                                                            {aluno.nome}
                                                        </label>
                                                    ))}
                                                </div>
                                            )}

                                        {modoDecisao === "recusar" && (
                                            <textarea
                                                value={motivo}
                                                onChange={(e) => setMotivo(e.target.value)}
                                                placeholder="Motivo da recusa (5–500)"
                                                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm"
                                            />
                                        )}

                                        <div className="flex gap-2">
                                            <button
                                                type="submit"
                                                disabled={salvando}
                                                className="rounded-lg bg-verdeEscuro px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                                            >
                                                {salvando ? "Salvando…" : "Confirmar"}
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() => setEmDecisao(null)}
                                                className="rounded-lg bg-gray-200 px-4 py-2 text-sm font-medium text-gray-700"
                                            >
                                                Cancelar
                                            </button>
                                        </div>
                                    </form>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </LayoutSecretaria>
    );
}
