import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import { SeletorDeFilho } from "../../components/SeletorDeFilho";
import {
    buscarPainelFamilia,
    buscarRelatorioDoFilho,
    type FilhoDoPainel,
    type RelatorioDoFilho,
    type TipoEventoAluno,
} from "../../services/PainelApi";

function isoDeHoje(diasAtras = 0): string {
    const data = new Date();
    data.setDate(data.getDate() - diasAtras);
    return data.toISOString().slice(0, 10);
}

const rotuloDoEvento: Record<TipoEventoAluno, string> = {
    Criado: "Cadastro",
    Alterado: "Dado alterado",
    RestricaoAdicionada: "Restrição adicionada",
    RestricaoRemovida: "Restrição removida",
};

/**
 * Relatório do próprio filho (Spec 025, RF6/RF7): restrições vigentes, alterações de
 * restrição e ocorrências no período. O backend responde `403` para qualquer aluno fora
 * do vínculo, inclusive id inexistente.
 */
export function RelatorioPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<FilhoDoPainel[]>([]);
    const [alunoId, setAlunoId] = useState<number | null>(null);
    const [de, setDe] = useState(() => isoDeHoje(29));
    const [ate, setAte] = useState(() => isoDeHoje());
    const [relatorio, setRelatorio] = useState<RelatorioDoFilho | null>(null);
    const [carregandoFilhos, setCarregandoFilhos] = useState(true);
    const [carregando, setCarregando] = useState(false);
    const [erro, setErro] = useState("");

    useEffect(() => {
        let ativo = true;

        async function carregarFilhos() {
            try {
                const painel = await buscarPainelFamilia();

                if (!ativo) return;

                setFilhos(
                    [...painel.filhos].sort((a, b) =>
                        a.nome.localeCompare(b.nome, "pt-BR"),
                    ),
                );
            } catch (falha) {
                if (naoAutorizado(falha)) {
                    await logout();
                    return;
                }

                if (ativo) {
                    setErro(
                        falha instanceof Error
                            ? falha.message
                            : "Não foi possível carregar seus filhos.",
                    );
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
    }, [logout]);

    const consultar = useCallback(async () => {
        if (alunoId === null) {
            setRelatorio(null);
            return;
        }

        setCarregando(true);
        setErro("");

        try {
            setRelatorio(await buscarRelatorioDoFilho(alunoId, { de, ate }));
        } catch (falha) {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            setRelatorio(null);
            setErro(
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível carregar o relatório.",
            );
        } finally {
            setCarregando(false);
        }
    }, [alunoId, de, ate, logout]);

    useEffect(() => {
        void consultar();
    }, [consultar]);

    if (carregandoFilhos) {
        return <p className="text-sm text-slate-500">Carregando seus filhos...</p>;
    }

    if (filhos.length === 0) {
        return (
            <div className="rounded-2xl border border-dashed border-slate-300 bg-white/70 p-6 text-center">
                <h1 className="text-lg font-semibold text-roxo">Nenhum aluno vinculado</h1>
                <p className="mt-2 text-sm text-slate-600">
                    O relatório fica disponível quando houver um filho vinculado à sua conta.
                </p>
            </div>
        );
    }

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Relatório do filho</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Restrições vigentes, alterações de restrição e ocorrências do período.
                </p>
            </header>

            <div className="flex flex-wrap items-end gap-4 rounded-2xl border border-slate-200 bg-white p-4">
                <div className="min-w-50">
                    <SeletorDeFilho
                        filhos={filhos}
                        value={alunoId}
                        onChange={setAlunoId}
                        id="relatorio-filho"
                    />
                </div>

                <div className="flex flex-col gap-1">
                    <label
                        htmlFor="relatorio-de"
                        className="text-sm font-medium text-slate-700"
                    >
                        De
                    </label>
                    <input
                        id="relatorio-de"
                        type="date"
                        value={de}
                        onChange={(evento) => setDe(evento.target.value)}
                        className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    />
                </div>

                <div className="flex flex-col gap-1">
                    <label
                        htmlFor="relatorio-ate"
                        className="text-sm font-medium text-slate-700"
                    >
                        Até
                    </label>
                    <input
                        id="relatorio-ate"
                        type="date"
                        value={ate}
                        onChange={(evento) => setAte(evento.target.value)}
                        className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    />
                </div>
            </div>

            {erro && (
                <p
                    role="alert"
                    className="mt-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
                >
                    {erro}
                </p>
            )}

            {carregando && (
                <p className="mt-4 text-sm text-slate-500">Carregando relatório...</p>
            )}

            {!carregando && relatorio && (
                <div className="mt-4 flex flex-col gap-4">
                    <article className="rounded-2xl border border-slate-200 bg-white p-5">
                        <h2 className="text-lg font-semibold text-roxo">
                            {relatorio.alunoNome}
                        </h2>
                        <p className="mt-1 text-xs text-slate-500">
                            Período de {relatorio.de} a {relatorio.ate}
                        </p>

                        <p className="mt-4 text-xs font-semibold uppercase tracking-wide text-slate-500">
                            Restrições vigentes
                        </p>
                        {relatorio.restricoes.length === 0 ? (
                            <p className="mt-1 text-sm text-slate-600">Sem restrições.</p>
                        ) : (
                            <ul className="mt-1 flex flex-wrap gap-1">
                                {relatorio.restricoes.map((restricao) => (
                                    <li
                                        key={restricao.nome}
                                        className="rounded-full bg-amber-50 px-2 py-1 text-xs font-medium text-amber-800"
                                    >
                                        {restricao.nome}
                                    </li>
                                ))}
                            </ul>
                        )}
                    </article>

                    <article className="rounded-2xl border border-slate-200 bg-white p-5">
                        <h2 className="text-base font-semibold text-slate-800">
                            Alterações de restrição
                        </h2>

                        {relatorio.alteracoes.length === 0 ? (
                            <p className="mt-2 text-sm text-slate-600">
                                Nenhuma alteração no período.
                            </p>
                        ) : (
                            <ul className="mt-3 divide-y divide-slate-100">
                                {relatorio.alteracoes.map((alteracao, indice) => (
                                    <li
                                        key={`${alteracao.tipo}-${alteracao.restricao}-${indice}`}
                                        className="py-2"
                                    >
                                        <p className="text-sm text-slate-700">
                                            <span className="font-medium">
                                                {rotuloDoEvento[alteracao.tipo]}
                                            </span>
                                            {": "}
                                            {alteracao.restricao}
                                        </p>
                                        <p className="text-xs text-slate-500">
                                            {alteracao.autor} ·{" "}
                                            {new Date(alteracao.criadoEm).toLocaleString(
                                                "pt-BR",
                                            )}
                                            {alteracao.origem
                                                ? ` · origem ${alteracao.origem}`
                                                : ""}
                                        </p>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </article>

                    <article className="rounded-2xl border border-slate-200 bg-white p-5">
                        <h2 className="text-base font-semibold text-slate-800">
                            Ocorrências
                        </h2>

                        {relatorio.ocorrencias.length === 0 ? (
                            <p className="mt-2 text-sm text-slate-600">
                                Nenhuma ocorrência no período.
                            </p>
                        ) : (
                            <ul className="mt-3 divide-y divide-slate-100">
                                {relatorio.ocorrencias.map((ocorrencia) => (
                                    <li key={ocorrencia.id} className="py-2">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <span className="text-sm font-medium text-slate-700">
                                                {ocorrencia.tipo}
                                            </span>
                                            <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                                                {ocorrencia.status}
                                            </span>
                                            <span className="text-xs text-slate-500">
                                                {new Date(
                                                    ocorrencia.criadaEm,
                                                ).toLocaleString("pt-BR")}
                                            </span>
                                        </div>
                                        <p className="mt-1 text-sm text-slate-600">
                                            {ocorrencia.descricao}
                                        </p>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </article>
                </div>
            )}
        </section>
    );
}
