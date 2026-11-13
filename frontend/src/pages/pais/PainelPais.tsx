import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { naoAutorizado } from "../../services/erroApi";

/**
 * Painel inicial dos pais (Spec 035, RF1/RF26). Lista exatamente os filhos devolvidos por
 * `GET /api/aluno`, ordenados por nome em pt-BR; sem filhos, as ações de aluno ficam
 * indisponíveis.
 */
export function PainelPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const lista = await buscarAlunos();
            setFilhos(
                [...lista].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
            );
        } catch (falha) {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            setErro(
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível carregar seus filhos.",
            );
        } finally {
            setCarregando(false);
        }
    }, [logout]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    if (carregando) {
        return <p className="text-sm text-slate-500">Carregando seus filhos...</p>;
    }

    if (erro) {
        return (
            <div className="rounded-2xl border border-red-200 bg-red-50 p-5">
                <p role="alert" className="text-sm text-red-700">
                    {erro}
                </p>
                <button
                    type="button"
                    onClick={() => void carregar()}
                    className="mt-3 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Tentar novamente
                </button>
            </div>
        );
    }

    if (filhos.length === 0) {
        return (
            <div className="rounded-2xl border border-dashed border-slate-300 bg-white/70 p-6 text-center">
                <h1 className="text-lg font-semibold text-roxo">Nenhum aluno vinculado</h1>
                <p className="mt-2 text-sm text-slate-600">
                    Sua conta ainda não possui filhos vinculados. Procure a secretaria da escola
                    para regularizar o vínculo.
                </p>
            </div>
        );
    }

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Meus filhos</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Selecione uma área para acompanhar a alimentação escolar.
                </p>
            </header>

            <div className="grid gap-4 md:grid-cols-2">
                {filhos.map((filho) => (
                    <article
                        key={filho.id}
                        className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                    >
                        <h2 className="text-lg font-semibold text-roxo">{filho.nome}</h2>
                        <p className="mt-1 text-sm text-slate-600">
                            {[filho.escola, filho.serie].filter(Boolean).join(" · ") ||
                                "Escola e série não informadas"}
                        </p>

                        <div className="mt-3">
                            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                Restrições
                            </p>
                            {filho.restricoes.length === 0 ? (
                                <p className="mt-1 text-sm text-slate-600">Sem restrições.</p>
                            ) : (
                                <ul className="mt-1 flex flex-wrap gap-1">
                                    {filho.restricoes.map((restricao) => (
                                        <li
                                            key={restricao}
                                            className="rounded-full bg-amber-50 px-2 py-1 text-xs font-medium text-amber-800"
                                        >
                                            {restricao}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    </article>
                ))}
            </div>

            <nav aria-label="Atalhos da área dos pais" className="mt-5 flex flex-wrap gap-2">
                <Link
                    to="/pais/cardapios"
                    className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Ver cardápios
                </Link>
                <Link
                    to="/pais/ocorrencias"
                    className="rounded-xl border border-roxo/40 px-4 py-2 text-sm font-semibold text-roxo focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Ocorrências
                </Link>
                <Link
                    to="/pais/solicitacoes"
                    className="rounded-xl border border-roxo/40 px-4 py-2 text-sm font-semibold text-roxo focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Solicitar alergia
                </Link>
                <Link
                    to="/pais/feedback"
                    className="rounded-xl border border-roxo/40 px-4 py-2 text-sm font-semibold text-roxo focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Feedback
                </Link>
            </nav>
        </section>
    );
}
