import { useEffect, useState } from "react";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import {
    listarCardapiosPublicados,
    type Cardapio,
    type FiltroPublicados,
} from "../../services/CardapioApi";
import { listarUnidadesAtivas, type UnidadeAtiva } from "../../services/UnidadeApi";

/**
 * Leitura pública de cardápios (Spec 012, RF9 e RF13).
 *
 * Consome apenas `/api/cardapios/publicados`, que devolve exclusivamente cardápios
 * `Publicado` cujo período inclui a data. Nada é calculado aqui: os valores e os totais
 * vêm do backend (RNF3).
 */
export function CardapiosPublicados({
    unidadeFixa,
    titulo,
}: {
    unidadeFixa?: number | null;
    titulo: string;
}) {
    const [unidades, setUnidades] = useState<UnidadeAtiva[]>([]);
    const [unidadeId, setUnidadeId] = useState<number | "">(unidadeFixa ?? "");
    const [data, setData] = useState("");
    const [refeicao, setRefeicao] = useState("");
    const [cardapios, setCardapios] = useState<Cardapio[]>([]);
    const [carregando, setCarregando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        async function carregarUnidades() {
            try {
                setUnidades(await listarUnidadesAtivas());
            } catch {
                setUnidades([]);
            }
        }

        if (unidadeFixa == null) {
            carregarUnidades();
        }
    }, [unidadeFixa]);

    useEffect(() => {
        async function carregar() {
            if (unidadeId === "") {
                setCardapios([]);
                return;
            }

            setCarregando(true);

            try {
                const filtro: FiltroPublicados = {
                    unidadeId: Number(unidadeId),
                    data: data === "" ? undefined : data,
                    refeicao: refeicao.trim() === "" ? undefined : refeicao.trim(),
                };

                const pagina = await listarCardapiosPublicados(filtro);
                setCardapios(pagina.itens);
                setErro(null);
            } catch (falha) {
                setCardapios([]);
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar os cardápios");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [unidadeId, data, refeicao]);

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-6 mt-6">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">{titulo}</h2>
                    <p className="text-sm text-gray-500 mt-1">
                        Somente cardápios publicados cujo período inclui a data escolhida.
                    </p>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex flex-wrap gap-4">
                        {unidadeFixa == null && (
                            <select
                                value={unidadeId}
                                onChange={(evento) =>
                                    setUnidadeId(
                                        evento.target.value === "" ? "" : Number(evento.target.value),
                                    )
                                }
                                aria-label="Unidade"
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer my-2"
                            >
                                <option value="">Escolha a unidade</option>
                                {unidades.map((unidade) => (
                                    <option key={unidade.id} value={unidade.id}>
                                        {unidade.nome}
                                    </option>
                                ))}
                            </select>
                        )}

                        <input
                            type="date"
                            value={data}
                            onChange={(evento) => setData(evento.target.value)}
                            aria-label="Data"
                            className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                        />

                        <input
                            type="text"
                            value={refeicao}
                            onChange={(evento) => setRefeicao(evento.target.value)}
                            placeholder="Refeição (ex.: Almoço)"
                            aria-label="Refeição"
                            className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                        />
                    </div>

                    {erro && (
                        <p role="alert" className="py-4 text-red-700">
                            {erro}
                        </p>
                    )}

                    {carregando && <p className="py-4 text-gray-500">Carregando cardápios...</p>}

                    {!carregando && unidadeId === "" && (
                        <p className="py-4 text-gray-500">Escolha a unidade para ver o cardápio.</p>
                    )}

                    {!carregando && unidadeId !== "" && cardapios.length === 0 && !erro && (
                        <p className="py-4 text-gray-500">
                            Nenhum cardápio publicado para essa data.
                        </p>
                    )}

                    <div className="mt-4 flex flex-col gap-6">
                        {cardapios.map((cardapio) => (
                            <div
                                key={cardapio.id}
                                className="rounded-2xl border border-[#fce9d3] bg-white p-4"
                            >
                                <h3 className="font-bold text-lg text-gray-800">{cardapio.nome}</h3>

                                <div className="mt-3 flex flex-col gap-4">
                                    {cardapio.refeicoes.map((refeicaoDoCardapio) => (
                                        <div key={`${refeicaoDoCardapio.ordem}-${refeicaoDoCardapio.nome}`}>
                                            <h4 className="font-semibold text-roxo">
                                                {refeicaoDoCardapio.nome}
                                            </h4>

                                            <ul className="mt-1 flex flex-col gap-1 text-sm text-gray-600">
                                                {refeicaoDoCardapio.itens.map((item) => (
                                                    <li key={item.alimentoId}>
                                                        {item.alimento} — {item.gramas} g ·{" "}
                                                        {item.valores.caloriasKcal} kcal
                                                    </li>
                                                ))}
                                            </ul>

                                            <p className="mt-1 text-sm text-gray-500">
                                                Total da refeição:{" "}
                                                {refeicaoDoCardapio.total.caloriasKcal} kcal
                                            </p>
                                        </div>
                                    ))}
                                </div>

                                <p className="mt-3 text-right text-sm text-gray-600">
                                    Total do cardápio:{" "}
                                    <span className="font-semibold text-roxo">
                                        {cardapio.total.caloriasKcal} kcal
                                    </span>
                                </p>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            <Footer />
        </div>
    );
}
