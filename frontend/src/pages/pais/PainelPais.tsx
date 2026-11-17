import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import { buscarPainelFamilia, type FilhoDoPainel } from "../../services/PainelApi";

/**
 * Painel inicial dos pais (Spec 025, RF5; Spec 035). Lista exatamente os filhos vinculados
 * ao responsável, com restrições e o cardápio do dia. Filho com restrição não recebe o
 * cardápio geral: o painel aponta a versão servida da área Cardápios (Spec 029).
 */
export function PainelPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<FilhoDoPainel[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            const painel = await buscarPainelFamilia();
            setFilhos(
                [...painel.filhos].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
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
        <section className="w-full">
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Meus filhos</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Acompanhe a alimentação escolar de cada filho.
                </p>
            </header>

            <div className="grid gap-4 md:grid-cols-2 w-full">
                {filhos.map((filho) => (
                    <article
                        key={filho.id}
                        className="flex flex-col rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
                    >
                        <h2 className="text-lg font-semibold text-roxo">{filho.nome}</h2>
                        <p className="mt-1 text-sm text-slate-600">
                            {[filho.escola, filho.unidade].filter(Boolean).join(" · ") ||
                                "Escola e unidade não informadas"}
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
                                            key={restricao.nome}
                                            className="rounded-full bg-amber-50 px-2 py-1 text-xs font-medium text-amber-800"
                                        >
                                            {restricao.nome}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>

                        <div className="mt-4 rounded-xl bg-[#FAF9F5] p-3">
                            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                                Cardápio de hoje
                            </p>

                            {filho.cardapio ? (
                                <div className="mt-1 text-sm text-slate-700">
                                    <p className="font-medium">{filho.cardapio.nome}</p>
                                    <ul className="mt-1 space-y-1">
                                        {filho.cardapio.refeicoes.map((refeicao) => (
                                            <li key={refeicao.nome}>
                                                <span className="font-medium">
                                                    {refeicao.nome}:
                                                </span>{" "}
                                                {refeicao.itens
                                                    .map((item) => item.alimento)
                                                    .join(", ") || "sem itens"}
                                            </li>
                                        ))}
                                    </ul>
                                </div>
                            ) : (
                                <p className="mt-1 text-sm text-slate-600">
                                    {filho.motivo ??
                                        "Não há cardápio publicado para hoje."}
                                </p>
                            )}

                            {filho.requerAdaptacao && (
                                <Link
                                    to="/pais/cardapios"
                                    className="mt-2 inline-block text-sm font-semibold text-roxo underline-offset-2 hover:underline"
                                >
                                    Ver a versão servida do meu filho
                                </Link>
                            )}
                        </div>
                    </article>
                ))}
            </div>


        </section>
    );
}
