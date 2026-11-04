import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChevronLeft, ChevronRight, Eye, Pencil, Plus, Sparkles, Trash } from "lucide-react";
import { ToastContainer, Zoom, toast } from "react-toastify";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import { useAuth } from "../../../context/authContext";
import { homePorPapel } from "../../../routes/homePorPapel";
import {
    excluirCardapio,
    listarCardapios,
    type CardapioResumo,
    type PaginaCardapios,
    type SituacaoCardapio,
} from "../../../services/CardapioApi";
import { listarUnidades, type Unidade } from "../../../services/UnidadeApi";

const situacoes: { valor: SituacaoCardapio | ""; rotulo: string }[] = [
    { valor: "", rotulo: "Todas as situações" },
    { valor: "Rascunho", rotulo: "Rascunho" },
    { valor: "Publicado", rotulo: "Publicado" },
    { valor: "Cancelado", rotulo: "Cancelado" },
];

const corSituacao: Record<SituacaoCardapio, string> = {
    Rascunho: "bg-bege text-roxo",
    Publicado: "bg-green-100 text-green-800",
    Cancelado: "bg-red-100 text-red-700",
    Substituido: "bg-purple-100 text-purple-700",
};

function dataCurta(iso: string): string {
    const [ano, mes, dia] = iso.split("-");
    return `${dia}/${mes}/${ano}`;
}

/**
 * Listagem de cardápios (Spec 012, RF13), no padrão visual das telas de alunos.
 * A nutrição vê toda a rede; a merenda vê apenas a própria unidade (RF8).
 */
export function ListarCardapios() {
    const { userLogin } = useAuth();
    const navigate = useNavigate();
    const ehNutricionista = userLogin?.papel === "Nutricionista";
    const inicio = userLogin?.papel ? homePorPapel[userLogin.papel] : "/";

    const [pagina, setPagina] = useState<PaginaCardapios<CardapioResumo> | null>(null);
    const [unidades, setUnidades] = useState<Unidade[]>([]);
    const [unidadeId, setUnidadeId] = useState<number | "">(userLogin?.unidadeId ?? "");
    const [data, setData] = useState("");
    const [situacao, setSituacao] = useState<SituacaoCardapio | "">("");
    const [paginaAtual, setPaginaAtual] = useState(1);
    const [versao, setVersao] = useState(0);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        async function carregarUnidades() {
            try {
                setUnidades(await listarUnidades());
            } catch {
                // A lista de unidades é conveniência de filtro: sem ela a tela continua.
                setUnidades([]);
            }
        }

        carregarUnidades();
    }, []);

    useEffect(() => {
        async function carregar() {
            setCarregando(true);

            try {
                const dados = await listarCardapios({
                    unidadeId: unidadeId === "" ? undefined : unidadeId,
                    data: data === "" ? undefined : data,
                    situacao: situacao === "" ? undefined : situacao,
                    pagina: paginaAtual,
                });

                setPagina(dados);
                setErro(null);
            } catch (falha) {
                setPagina(null);
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar os cardápios");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [unidadeId, data, situacao, paginaAtual, versao]);

    const excluir = useCallback(async (cardapio: CardapioResumo) => {
        if (!window.confirm(`Excluir o cardápio "${cardapio.nome}"?`)) {
            return;
        }

        try {
            await excluirCardapio(cardapio.id);
            toast.success(`${cardapio.nome} foi excluído.`);
            setVersao((atual) => atual + 1);
        } catch (falha) {
            const mensagem = falha instanceof Error ? falha.message : "Erro ao excluir o cardápio";
            setErro(mensagem);
            toast.error(mensagem);
        }
    }, []);

    const filtroAtivo = unidadeId !== "" || data !== "" || situacao !== "";

    function limparCampos() {
        setUnidadeId(userLogin?.unidadeId ?? "");
        setData("");
        setSituacao("");
        setPaginaAtual(1);
    }

    const totalPaginas = pagina ? Math.max(1, Math.ceil(pagina.total / pagina.tamanho)) : 1;

    return (
        <div className="flex flex-col items-center min-h-screen bg-fundo">
            <Header />

            <div className="mx-auto w-full max-w-7xl">
                <button
                    type="button"
                    onClick={() => navigate(inicio)}
                    aria-label="Voltar ao início"
                    title="Voltar ao início"
                    className="group flex cursor-pointer items-center justify-center"
                >
                    <ChevronLeft
                        size={50}
                        className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                    />
                </button>
            </div>

            <div className="flex w-full max-w-7xl flex-col items-center justify-center bg-[#FAF9F5] mb-30 mx-auto rounded-3xl p-8 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                <div className="flex w-full flex-col justify-start gap-2 md:flex-row md:items-center md:justify-between">
                    <div>
                        <h1 className="font-bold text-2xl text-gray-800">Cardápios</h1>
                        <h2 className="text-lg">
                            Consulte por unidade, data e situação, e monte o cardápio por refeição.
                        </h2>
                    </div>

                    {ehNutricionista && (
                        <div className="flex items-center gap-2">
                            <button
                                type="button"
                                onClick={() => navigate("/cardapios/parametros-geracao")}
                                className="flex shrink-0 items-center justify-center gap-2 rounded-2xl bg-roxo px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-purple-700 hover:shadow-md active:scale-[0.98]"
                            >
                                <Sparkles size={18} />
                                Gerar automático
                            </button>
                            <button
                                type="button"
                                onClick={() => navigate("/cardapios/novo")}
                                className="flex shrink-0 items-center justify-center gap-2 rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98]"
                            >
                                <Plus size={18} />
                                Novo cardápio
                            </button>
                        </div>
                    )}
                </div>

                <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                <div className="flex flex-row flex-wrap gap-4 items-center justify-center">
                    <div className="flex flex-col">
                        <label htmlFor="inUnidade">Unidade</label>
                        <select
                            id="inUnidade"
                            value={unidadeId}
                            disabled={!ehNutricionista && userLogin?.unidadeId != null}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setUnidadeId(
                                    evento.target.value === "" ? "" : Number(evento.target.value),
                                );
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4 shadow-[0_5px_20px_rgba(1,1,1,0.1)] disabled:cursor-not-allowed disabled:bg-gray-100"
                        >
                            <option value="">Todas as unidades</option>
                            {unidades.map((unidade) => (
                                <option key={unidade.id} value={unidade.id}>
                                    {unidade.nome}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inData">Data do período</label>
                        <input
                            id="inData"
                            type="date"
                            value={data}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setData(evento.target.value);
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                        />
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inSituacao">Situação</label>
                        <select
                            id="inSituacao"
                            value={situacao}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setSituacao(evento.target.value as SituacaoCardapio | "");
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                        >
                            {situacoes.map((opcao) => (
                                <option key={opcao.rotulo} value={opcao.valor}>
                                    {opcao.rotulo}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <button
                    type="button"
                    onClick={limparCampos}
                    disabled={!filtroAtivo}
                    className="mt-4 text-sm text-vermelho hover:cursor-pointer hover:underline disabled:cursor-not-allowed disabled:text-gray-400 disabled:hover:no-underline"
                >
                    Limpar filtro
                </button>

                <h2 className="w-full py-4 text-lg">
                    <span className="text-roxo">{pagina?.total ?? 0}</span> cardápio(s) encontrado(s)
                </h2>

                {carregando && <p className="w-full py-4 text-gray-500">Carregando cardápios...</p>}
                {erro && (
                    <p role="alert" className="w-full py-4 text-red-700">
                        {erro}
                    </p>
                )}
                {!carregando && !erro && pagina?.itens.length === 0 && (
                    <p className="w-full py-4 text-gray-500">Nenhum cardápio encontrado.</p>
                )}

                <div className="w-full overflow-hidden rounded-3xl border border-[#fce9d3] shadow-[0_5px_5px_rgba(1,1,1,0.1)]">
                    <table className="w-full bg-white">
                        <thead>
                            <tr className="h-12 bg-bege">
                                <th className="px-4 text-left font-light">Cardápio</th>
                                <th className="px-4 text-left font-light">Período</th>
                                <th className="px-4 text-left font-light">Refeições</th>
                                <th className="px-4 text-left font-light">Situação</th>
                                <th className="px-4 text-center font-light">Visualizar</th>
                                {ehNutricionista && (
                                    <th className="px-4 text-center font-light">Editar</th>
                                )}
                                {ehNutricionista && (
                                    <th className="px-4 text-center font-light">Excluir</th>
                                )}
                            </tr>
                        </thead>

                        <tbody>
                            {pagina?.itens.map((cardapio) => (
                                <tr key={cardapio.id} className="border-t border-[#fce9d3] h-12">
                                    <td className="p-4 max-w-60 truncate" title={cardapio.nome}>
                                        {cardapio.nome}
                                    </td>
                                    <td className="p-4">
                                        {dataCurta(cardapio.periodo.inicio)} a{" "}
                                        {dataCurta(cardapio.periodo.fim)}
                                    </td>
                                    <td className="p-4">{cardapio.totalRefeicoes}</td>
                                    <td className="p-4">
                                        <div className="flex flex-col items-start gap-1">
                                            <span
                                                className={`rounded-full px-3 py-1 text-sm ${corSituacao[cardapio.situacao]}`}
                                            >
                                                {cardapio.situacao}
                                            </span>
                                            {cardapio.totalBloqueantes > 0 && (
                                                <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs text-red-700">
                                                    incompatível
                                                </span>
                                            )}
                                            {cardapio.totalAlertas > 0 && (
                                                <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700">
                                                    {cardapio.totalAlertas} alerta(s)
                                                </span>
                                            )}
                                        </div>
                                    </td>
                                    <td className="p-4 text-center">
                                        <button
                                            type="button"
                                            onClick={() => navigate(`/cardapios/${cardapio.id}`)}
                                            aria-label={`Visualizar ${cardapio.nome}`}
                                            className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full"
                                        >
                                            <Eye />
                                        </button>
                                    </td>

                                    {ehNutricionista && (
                                        <td className="p-4 text-center">
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    navigate(`/cardapios/${cardapio.id}/editar`)
                                                }
                                                disabled={cardapio.situacao !== "Rascunho"}
                                                aria-label={`Editar ${cardapio.nome}`}
                                                title={
                                                    cardapio.situacao === "Rascunho"
                                                        ? "Editar"
                                                        : "Só rascunho é editável"
                                                }
                                                className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full disabled:cursor-not-allowed disabled:text-gray-300 disabled:hover:bg-transparent"
                                            >
                                                <Pencil />
                                            </button>
                                        </td>
                                    )}

                                    {ehNutricionista && (
                                        <td className="p-4 text-center">
                                            <button
                                                type="button"
                                                onClick={() => void excluir(cardapio)}
                                                disabled={cardapio.situacao !== "Rascunho"}
                                                aria-label={`Excluir ${cardapio.nome}`}
                                                title={
                                                    cardapio.situacao === "Rascunho"
                                                        ? "Excluir"
                                                        : "Só rascunho é excluído"
                                                }
                                                className="text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full disabled:cursor-not-allowed disabled:text-gray-300 disabled:hover:bg-transparent"
                                            >
                                                <Trash />
                                            </button>
                                        </td>
                                    )}
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>

                <div className="mt-6 flex w-full items-center justify-between">
                    <p className="text-sm text-gray-500">
                        Página {paginaAtual} de {totalPaginas}
                    </p>

                    <div className="flex items-center gap-3">
                        <button
                            type="button"
                            onClick={() => setPaginaAtual((atual) => Math.max(1, atual - 1))}
                            disabled={paginaAtual <= 1}
                            className="flex items-center gap-1 rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40"
                        >
                            <ChevronLeft size={16} /> Anterior
                        </button>

                        <button
                            type="button"
                            onClick={() =>
                                setPaginaAtual((atual) => Math.min(totalPaginas, atual + 1))
                            }
                            disabled={paginaAtual >= totalPaginas}
                            className="flex items-center gap-1 rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40"
                        >
                            Próxima <ChevronRight size={16} />
                        </button>
                    </div>
                </div>

                <ToastContainer
                    position="top-center"
                    autoClose={2000}
                    hideProgressBar={false}
                    newestOnTop
                    closeOnClick={false}
                    rtl={false}
                    pauseOnFocusLoss
                    draggable
                    pauseOnHover
                    theme="light"
                    transition={Zoom}
                    toastClassName="!shadow-none !bg-bege !font-[DynaPuff] !w-100"
                />
            </div>

            <footer className="w-full">
                <Footer />
            </footer>
        </div>
    );
}
