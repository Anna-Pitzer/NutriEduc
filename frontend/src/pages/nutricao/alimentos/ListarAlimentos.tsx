import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, Pencil, Plus, Power } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { ToastContainer, Zoom, toast } from "react-toastify";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import { useAuth } from "../../../context/authContext";
import { homePorPapel } from "../../../routes/homePorPapel";
import {
    desativarAlimento,
    listarAlimentos,
    listarCategoriasAlimento,
    reativarAlimento,
    type CategoriaAlimento,
    type OrdenacaoAlimento,
    type PaginaAlimentos,
    type VinculoAlimento,
} from "../../../services/AlimentoApi";

/** Rótulos da ordenação: a mesma lista fechada que o backend aceita (RF7). */
const ordenacoes: { valor: OrdenacaoAlimento; rotulo: string }[] = [
    { valor: "nome", rotulo: "Nome (A–Z)" },
    { valor: "nome-desc", rotulo: "Nome (Z–A)" },
    { valor: "categoria", rotulo: "Categoria" },
    { valor: "recentes", rotulo: "Mais recentes" },
];

/** Cor do vínculo: alergia bloqueia, restrição apenas alerta (Spec 011, RF4). */
function corVinculo(vinculo: VinculoAlimento): string {
    return vinculo.categoria === "Alergia"
        ? "bg-red-100 text-red-700"
        : "bg-yellow-100 text-yellow-800";
}

/**
 * Catálogo de alimentos (Spec 011, RF10), no mesmo padrão visual da listagem de
 * alunos: cabeçalho, cartão centrado, filtros em linha, contagem e tabela.
 *
 * A leitura do catálogo é da nutrição e da merenda; a escrita, só da nutrição — o
 * backend recusa o resto (RF9).
 */
export function ListarAlimentos() {
    const { userLogin } = useAuth();
    const navigate = useNavigate();
    const ehNutricionista = userLogin?.papel === "Nutricionista";
    const inicio = userLogin?.papel ? homePorPapel[userLogin.papel] : "/";

    const [pagina, setPagina] = useState<PaginaAlimentos | null>(null);
    const [categorias, setCategorias] = useState<CategoriaAlimento[]>([]);
    const [busca, setBusca] = useState("");
    const [categoriaId, setCategoriaId] = useState<number | "">("");
    const [incluirInativos, setIncluirInativos] = useState(false);
    const [ordenacao, setOrdenacao] = useState<OrdenacaoAlimento>("nome");
    const [paginaAtual, setPaginaAtual] = useState(1);
    const [versao, setVersao] = useState(0);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        async function carregarCategorias() {
            try {
                const lista = await listarCategoriasAlimento(ehNutricionista);
                setCategorias(lista);
            } catch (falha) {
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar categorias");
            }
        }

        carregarCategorias();
    }, [ehNutricionista]);

    useEffect(() => {
        async function carregar() {
            setCarregando(true);

            try {
                const dados = await listarAlimentos({
                    busca,
                    categoriaId: categoriaId === "" ? undefined : categoriaId,
                    pagina: paginaAtual,
                    incluirInativos,
                    ordenar: ordenacao,
                });

                setPagina(dados);
                setErro(null);
            } catch (falha) {
                setPagina(null);
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar alimentos");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [busca, categoriaId, incluirInativos, ordenacao, paginaAtual, versao]);

    const alterarSituacao = useCallback(async (id: number, nome: string, ativo: boolean) => {
        try {
            if (ativo) {
                await desativarAlimento(id);
                toast.success(`${nome} foi desativado.`);
            } else {
                await reativarAlimento(id);
                toast.success(`${nome} foi reativado.`);
            }

            setVersao((atual) => atual + 1);
        } catch (falha) {
            const mensagem = falha instanceof Error ? falha.message : "Erro ao alterar o alimento";
            setErro(mensagem);
            toast.error(mensagem);
        }
    }, []);

    const filtroAtivo =
        busca.trim() !== "" || categoriaId !== "" || incluirInativos || ordenacao !== "nome";

    function limparCampos() {
        setBusca("");
        setCategoriaId("");
        setIncluirInativos(false);
        setOrdenacao("nome");
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
                        <h1 className="font-bold text-2xl text-gray-800">Catálogo de alimentos</h1>
                        <h2 className="text-lg">
                            Consulte e filtre por nome, categoria, situação e ordenação.
                        </h2>
                    </div>

                    {ehNutricionista && (
                        <button
                            type="button"
                            onClick={() => navigate("/alimentos/novo")}
                            className="flex shrink-0 items-center justify-center gap-2 rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98]"
                        >
                            <Plus size={18} />
                            Novo alimento
                        </button>
                    )}
                </div>

                <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                <div className="flex flex-row flex-wrap gap-4 items-center justify-center">
                    <div className="flex flex-col">
                        <label htmlFor="inBusca">Buscar por nome</label>
                        <input
                            id="inBusca"
                            type="text"
                            value={busca}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setBusca(evento.target.value);
                            }}
                            placeholder="Digite o nome do alimento..."
                            className="rounded-2xl h-12 w-70 border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                        />
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inCategoria">Categoria</label>
                        <select
                            id="inCategoria"
                            value={categoriaId}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setCategoriaId(
                                    evento.target.value === "" ? "" : Number(evento.target.value),
                                );
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                        >
                            <option value="">Todas as categorias</option>
                            {categorias.map((categoria) => (
                                <option key={categoria.id} value={categoria.id}>
                                    {categoria.nome}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inSituacao">Situação</label>
                        <select
                            id="inSituacao"
                            value={incluirInativos ? "todos" : "ativos"}
                            disabled={!ehNutricionista}
                            title={
                                ehNutricionista
                                    ? undefined
                                    : "Somente a nutrição pode ver alimentos desativados."
                            }
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setIncluirInativos(evento.target.value === "todos");
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4 shadow-[0_5px_20px_rgba(1,1,1,0.1)] disabled:cursor-not-allowed disabled:bg-gray-100"
                        >
                            <option value="ativos">Somente ativos</option>
                            <option value="todos">Todos (inclui desativados)</option>
                        </select>
                    </div>

                    <div className="flex flex-col">
                        <label htmlFor="inOrdenar">Ordenar por</label>
                        <select
                            id="inOrdenar"
                            value={ordenacao}
                            onChange={(evento) => {
                                setPaginaAtual(1);
                                setOrdenacao(evento.target.value as OrdenacaoAlimento);
                            }}
                            className="h-12 w-70 rounded-2xl border border-gray-200 bg-white text-sm text-gray-400 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-4 shadow-[0_5px_20px_rgba(1,1,1,0.1)]"
                        >
                            {ordenacoes.map((opcao) => (
                                <option key={opcao.valor} value={opcao.valor}>
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
                    <span className="text-roxo">{pagina?.total ?? 0}</span> alimento(s) encontrado(s)
                    {filtroAtivo ? " com o filtro aplicado" : ""}
                </h2>

                {carregando && <p className="w-full py-4 text-gray-500">Carregando alimentos...</p>}
                {erro && (
                    <p role="alert" className="w-full py-4 text-red-700">
                        {erro}
                    </p>
                )}
                {!carregando && !erro && pagina?.itens.length === 0 && (
                    <p className="w-full py-4 text-gray-500">Nenhum alimento encontrado.</p>
                )}

                <div className="w-full overflow-hidden rounded-3xl border border-[#fce9d3] shadow-[0_5px_5px_rgba(1,1,1,0.1)]">
                    <table className="w-full bg-white">
                        <thead>
                            <tr className="h-12 bg-bege">
                                <th className="px-4 text-left font-light">Alimento</th>
                                <th className="px-4 text-left font-light">Categoria</th>
                                <th className="px-4 text-left font-light">kcal</th>
                                <th className="px-4 text-left font-light">Proteínas</th>
                                <th className="px-4 text-left font-light">Carboidratos</th>
                                <th className="px-4 text-left font-light">Vínculos</th>
                                <th className="px-4 text-left font-light">Situação</th>
                                {ehNutricionista && (
                                    <th className="px-4 text-center font-light">Editar</th>
                                )}
                                {ehNutricionista && (
                                    <th className="px-4 text-center font-light">Ativar/Desativar</th>
                                )}
                            </tr>
                        </thead>

                        <tbody>
                            {pagina?.itens.map((alimento) => (
                                <tr key={alimento.id} className="border-t border-[#fce9d3] h-12">
                                    <td className="p-4 max-w-60 truncate" title={alimento.nome}>
                                        {alimento.nome}
                                    </td>
                                    <td className="p-4">{alimento.categoria.nome}</td>
                                    <td className="p-4">{alimento.valores.caloriasKcal}</td>
                                    <td className="p-4">{alimento.valores.proteinasG} g</td>
                                    <td className="p-4">{alimento.valores.carboidratosG} g</td>
                                    <td className="p-4">
                                        {alimento.vinculos.length === 0 ? (
                                            <span className="text-gray-400">—</span>
                                        ) : (
                                            <div className="flex flex-wrap gap-2">
                                                {alimento.vinculos.map((vinculo) => (
                                                    <span
                                                        key={vinculo.restricaoId}
                                                        className={`rounded-full px-3 py-1 text-sm ${corVinculo(vinculo)}`}
                                                        title={
                                                            vinculo.categoria === "Alergia"
                                                                ? "Bloqueia o uso no cardápio"
                                                                : "Apenas alerta"
                                                        }
                                                    >
                                                        • {vinculo.restricao}
                                                    </span>
                                                ))}
                                            </div>
                                        )}
                                    </td>
                                    <td className="p-4">
                                        <span
                                            className={
                                                alimento.ativo ? "text-verdeClaro" : "text-gray-400"
                                            }
                                        >
                                            {alimento.ativo ? "Ativo" : "Desativado"}
                                        </span>
                                    </td>

                                    {ehNutricionista && (
                                        <td className="p-4 text-center">
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    navigate(`/alimentos/editar/${alimento.id}`)
                                                }
                                                aria-label={`Editar ${alimento.nome}`}
                                                className="text-roxo p-2 hover:cursor-pointer hover:bg-purple-100 rounded-full"
                                            >
                                                <Pencil />
                                            </button>
                                        </td>
                                    )}

                                    {ehNutricionista && (
                                        <td className="p-4 text-center">
                                            <button
                                                type="button"
                                                onClick={() =>
                                                    void alterarSituacao(
                                                        alimento.id,
                                                        alimento.nome,
                                                        alimento.ativo,
                                                    )
                                                }
                                                aria-label={
                                                    alimento.ativo
                                                        ? `Desativar ${alimento.nome}`
                                                        : `Reativar ${alimento.nome}`
                                                }
                                                className={
                                                    alimento.ativo
                                                        ? "text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full"
                                                        : "text-verdeClaro p-2 hover:cursor-pointer hover:bg-green-100 rounded-full"
                                                }
                                            >
                                                <Power />
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
