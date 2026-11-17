import { useCallback, useEffect, useState } from "react";
import Header from "../../components/Header";
import Navbar from "../../components/Navbar";
import Footer from "../../components/Footer";
import { useAuth } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import { buscarPainelMerendeira, type PainelMerendeira } from "../../services/PainelApi";
import { AlertaAlunosIndisponiveis } from "./AlertaAlunosIndisponiveis";

/**
 * Área das merendeiras (Spec 025, RF3/RF4; Spec 029, RF15): cardápio publicado do dia da
 * própria unidade, com a contagem de alunos com restrição por item, e alerta das versões
 * adaptadas que não podem ser servidas. A contagem vem do backend; esta tela nunca
 * identifica aluno e não oferece nenhuma ação de escrita.
 *
 * A casca segue o padrão das demais áreas (mesma estrutura de `layoutSecretaria` e
 * `LayoutPais`): fundo `bg-fundo`, `Navbar` como menu lateral, conteúdo em cartão branco
 * com borda `lilaz` e `Footer` ao final.
 */
export function HomeMerendeiras() {
    const { userLogin, logout } = useAuth();
    const unidadeId = userLogin?.unidadeId ?? null;

    const [data, setData] = useState(() => new Date().toISOString().slice(0, 10));
    const [painel, setPainel] = useState<PainelMerendeira | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            setPainel(await buscarPainelMerendeira(data));
        } catch (falha) {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            setPainel(null);
            setErro(
                falha instanceof Error
                    ? falha.message
                    : "Não foi possível carregar o cardápio do dia.",
            );
        } finally {
            setCarregando(false);
        }
    }, [data, logout]);

    useEffect(() => {
        void carregar();
    }, [carregar]);

    return (
        <div className="relative min-h-screen bg-fundo">
            <div className="relative z-20 flex min-h-screen flex-col">
                <Header />

                <div className="mx-auto flex w-full max-w-350 flex-1 flex-col items-stretch gap-6 px-4 pb-8 md:px-8 lg:flex-row lg:items-start">
                    <div className="shrink-0 lg:sticky lg:top-6">
                        <Navbar />
                    </div>

                    <main className="min-w-0 flex-1">
                        <AlertaAlunosIndisponiveis unidadeId={unidadeId} />

                        <div className="rounded-2xl border-4 border-lilaz bg-white p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            <div className="flex flex-wrap items-start justify-between gap-4">
                                <div>
                                    <h1 className="text-2xl font-bold text-roxo">
                                        Cardápio do dia
                                    </h1>
                                    <p className="mt-2 text-sm text-gray-500">
                                        {painel?.unidade ??
                                            "Cardápio publicado da unidade da sua sessão."}
                                    </p>
                                </div>

                                <div>
                                    <label
                                        htmlFor="data-do-cardapio"
                                        className="block text-xs font-semibold uppercase tracking-wide text-gray-500"
                                    >
                                        Data
                                    </label>
                                    <input
                                        id="data-do-cardapio"
                                        type="date"
                                        value={data}
                                        onChange={(evento) => setData(evento.target.value)}
                                        className="mt-1 h-11 rounded-xl border border-gray-200 bg-white px-3 text-sm text-gray-700 outline-none transition-colors focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                                    />
                                </div>
                            </div>

                            <p className="mt-4 rounded-xl bg-[#FAF9F5] px-4 py-2 text-xs text-gray-600">
                                Consulta somente leitura. A contagem de restrições é da unidade e
                                nunca identifica um aluno.
                            </p>

                            {erro && (
                                <p
                                    role="alert"
                                    className="mt-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700"
                                >
                                    {erro}
                                </p>
                            )}

                            {carregando && (
                                <p className="mt-6 text-sm text-gray-500">
                                    Carregando cardápio...
                                </p>
                            )}

                            {!carregando && painel && !painel.cardapio && (
                                <p className="mt-6 text-sm text-gray-600">
                                    {painel.motivo ??
                                        "Não há cardápio publicado para a data escolhida."}
                                </p>
                            )}
                        </div>

                        {painel?.cardapio && (
                            <section className="mt-6 flex flex-col gap-4">
                                <div className="rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                                    <h2 className="text-lg font-semibold text-gray-800">
                                        {painel.cardapio.nome}
                                    </h2>
                                    {painel.cardapio.publicadoEm && (
                                        <p className="mt-1 text-xs text-gray-500">
                                            Publicado em{" "}
                                            {new Date(
                                                painel.cardapio.publicadoEm,
                                            ).toLocaleString("pt-BR")}
                                        </p>
                                    )}
                                </div>

                                {painel.cardapio.refeicoes.map((refeicao) => (
                                    <article
                                        key={refeicao.nome}
                                        className="rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                                    >
                                        <h3 className="text-base font-semibold text-roxo">
                                            {refeicao.nome}
                                        </h3>

                                        {refeicao.itens.length === 0 ? (
                                            <p className="mt-2 text-sm text-gray-500">
                                                Nenhum item nesta refeição.
                                            </p>
                                        ) : (
                                            <ul className="mt-3 divide-y divide-gray-100">
                                                {refeicao.itens.map((item) => (
                                                    <li
                                                        key={`${refeicao.nome}-${item.alimento}`}
                                                        className="flex flex-wrap items-center justify-between gap-2 py-2"
                                                    >
                                                        <div className="text-sm text-gray-700">
                                                            <span className="font-medium">
                                                                {item.alimento}
                                                            </span>
                                                            <span className="ml-2 text-gray-500">
                                                                {item.gramas} g
                                                            </span>
                                                        </div>

                                                        {item.alunosComRestricao > 0 ? (
                                                            <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800">
                                                                {item.alunosComRestricao} aluno(s)
                                                                {" com restrição"}
                                                            </span>
                                                        ) : (
                                                            <span className="rounded-full bg-green-50 px-3 py-1 text-xs font-medium text-green-700">
                                                                Sem restrição
                                                            </span>
                                                        )}
                                                    </li>
                                                ))}
                                            </ul>
                                        )}
                                    </article>
                                ))}
                            </section>
                        )}
                    </main>
                </div>

                <Footer />
            </div>
        </div>
    );
}
