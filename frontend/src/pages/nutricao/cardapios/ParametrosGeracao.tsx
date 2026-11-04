import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChevronLeft, Save, Sparkles } from "lucide-react";
import Button from "../../../components/Button";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import {
    gerarCardapio,
    obterParametrosGeracao,
    salvarParametrosGeracao,
    type GeracaoCardapio,
    type ParametroGeracao,
} from "../../../services/GeracaoApi";
import { listarUnidades, type Unidade } from "../../../services/UnidadeApi";
import { listarNutricionistas, type Nutricionista } from "../../../services/NutricionistaApi";

/**
 * Tela de parâmetros de geração automática (Spec 013, RF12): a nutrição configura a faixa
 * calórica, as opções por refeição, a janela de repetição e as refeições padrão de uma
 * unidade e dispara a geração com semente opcional.
 */
export function ParametrosGeracao() {
    const navigate = useNavigate();

    const [unidades, setUnidades] = useState<Unidade[]>([]);
    const [unidadeId, setUnidadeId] = useState<number | "">("");
    const [nutricionistas, setNutricionistas] = useState<Nutricionista[]>([]);
    const [nutricionistaId, setNutricionistaId] = useState<number | "">("");
    const [, setParametro] = useState<ParametroGeracao | null>(null);

    const [caloriasMinimas, setCaloriasMinimas] = useState(0);
    const [caloriasMaximas, setCaloriasMaximas] = useState(500);
    const [opcoesPorRefeicao, setOpcoesPorRefeicao] = useState(2);
    const [janelaRepeticaoDias, setJanelaRepeticaoDias] = useState(0);
    const [excluirIncompativeis, setExcluirIncompativeis] = useState(false);
    const [refeicoesPadrao, setRefeicoesPadrao] = useState("Café da manhã, Almoço, Jantar");

    const [nome, setNome] = useState("");
    const [inicio, setInicio] = useState("");
    const [fim, setFim] = useState("");
    const [seed, setSeed] = useState("");

    const [resultado, setResultado] = useState<GeracaoCardapio | null>(null);
    const [carregando, setCarregando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);
    const [mensagem, setMensagem] = useState<string | null>(null);

    useEffect(() => {
        listarUnidades()
            .then(setUnidades)
            .catch(() => setUnidades([]));

        listarNutricionistas()
            .then(setNutricionistas)
            .catch(() => setNutricionistas([]));
    }, []);

    useEffect(() => {
        if (unidadeId === "") {
            setParametro(null);
            return;
        }

        obterParametrosGeracao(Number(unidadeId))
            .then((parametro) => {
                setParametro(parametro);
                setCaloriasMinimas(parametro.caloriasMinimasRefeicao);
                setCaloriasMaximas(parametro.caloriasMaximasRefeicao);
                setOpcoesPorRefeicao(parametro.opcoesPorRefeicao);
                setJanelaRepeticaoDias(parametro.janelaRepeticaoDias);
                setExcluirIncompativeis(parametro.excluirAlimentosIncompativeis);
                setRefeicoesPadrao(parametro.refeicoesPadrao.join(", "));
            })
            .catch(() => setParametro(null));
    }, [unidadeId]);

    function refeicoes(): string[] {
        return refeicoesPadrao
            .split(",")
            .map((nome) => nome.trim())
            .filter((nome) => nome.length > 0);
    }

    async function salvar() {
        if (unidadeId === "") {
            setErro("Selecione uma unidade.");
            return;
        }

        setErro(null);
        setMensagem(null);

        try {
            const salvo = await salvarParametrosGeracao(Number(unidadeId), {
                caloriasMinimasRefeicao: caloriasMinimas,
                caloriasMaximasRefeicao: caloriasMaximas,
                opcoesPorRefeicao: opcoesPorRefeicao,
                janelaRepeticaoDias: janelaRepeticaoDias,
                excluirAlimentosIncompativeis: excluirIncompativeis,
                categoriasObrigatorias: [],
                refeicoesPadrao: refeicoes(),
            });

            setParametro(salvo);
            setMensagem("Parâmetros salvos.");
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao salvar os parâmetros");
        }
    }

    async function gerar() {
        if (unidadeId === "") {
            setErro("Selecione uma unidade.");
            return;
        }

        if (!nome.trim() || !inicio || !fim) {
            setErro("Informe o nome e o período do cardápio.");
            return;
        }

        if (nutricionistaId === "") {
            setErro("Selecione a nutricionista responsável pelo cardápio.");
            return;
        }

        setErro(null);
        setMensagem(null);
        setCarregando(true);

        try {
            const gerado = await gerarCardapio({
                unidadeId: Number(unidadeId),
                nutricionistaId: Number(nutricionistaId),
                nome: nome.trim(),
                periodo: { inicio, fim },
                seed: seed.trim() === "" ? undefined : Number(seed),
            });

            setResultado(gerado);
            setMensagem(`Cardápio #${gerado.cardapioId} gerado (semente ${gerado.seed}).`);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao gerar o cardápio");
        } finally {
            setCarregando(false);
        }
    }

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate("/cardapios")}
                aria-label="Voltar aos cardápios"
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-10">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Parâmetros de geração automática</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex flex-col gap-4">
                        <div className="flex flex-col">
                            <label htmlFor="inUnidade">Unidade</label>
                            <select
                                id="inUnidade"
                                value={unidadeId}
                                onChange={(evento) =>
                                    setUnidadeId(evento.target.value === "" ? "" : Number(evento.target.value))
                                }
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            >
                                <option value="">Selecione uma unidade</option>
                                {unidades.map((unidade) => (
                                    <option key={unidade.id} value={unidade.id}>
                                        {unidade.nome}
                                    </option>
                                ))}
                            </select>
                            <p className="mt-1 text-xs text-gray-500">
                                O cardápio gerado vale para todas as escolas desta unidade.
                            </p>
                        </div>

                        <div className="grid gap-4 md:grid-cols-2">
                            <div className="flex flex-col">
                                <label htmlFor="inMin">Calorias mínimas por refeição</label>
                                <input
                                    id="inMin"
                                    type="number"
                                    value={caloriasMinimas}
                                    onChange={(evento) => setCaloriasMinimas(Number(evento.target.value))}
                                    className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                                />
                            </div>
                            <div className="flex flex-col">
                                <label htmlFor="inMax">Calorias máximas por refeição</label>
                                <input
                                    id="inMax"
                                    type="number"
                                    value={caloriasMaximas}
                                    onChange={(evento) => setCaloriasMaximas(Number(evento.target.value))}
                                    className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                                />
                            </div>
                            <div className="flex flex-col">
                                <label htmlFor="inOpcoes">Opções por refeição (1–10)</label>
                                <input
                                    id="inOpcoes"
                                    type="number"
                                    min={1}
                                    max={10}
                                    value={opcoesPorRefeicao}
                                    onChange={(evento) => setOpcoesPorRefeicao(Number(evento.target.value))}
                                    className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                                />
                            </div>
                            <div className="flex flex-col">
                                <label htmlFor="inJanela">Janela de repetição (dias, 0–30)</label>
                                <input
                                    id="inJanela"
                                    type="number"
                                    min={0}
                                    max={30}
                                    value={janelaRepeticaoDias}
                                    onChange={(evento) => setJanelaRepeticaoDias(Number(evento.target.value))}
                                    className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                                />
                            </div>
                        </div>

                        <div className="flex flex-col">
                            <label htmlFor="inRefeicoes">Refeições padrão (separadas por vírgula)</label>
                            <input
                                id="inRefeicoes"
                                value={refeicoesPadrao}
                                onChange={(evento) => setRefeicoesPadrao(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            />
                        </div>

                        <label className="flex items-center gap-2 text-sm text-gray-700">
                            <input
                                type="checkbox"
                                checked={excluirIncompativeis}
                                onChange={(evento) => setExcluirIncompativeis(evento.target.checked)}
                            />
                            Excluir alimentos incompatíveis com alergias dos alunos
                        </label>

                        <div className="mt-2 flex justify-start">
                            <Button type="button" variant="ver" onClick={salvar}>
                                <span className="flex items-center gap-2">
                                    <Save size={18} />
                                    Salvar parâmetros
                                </span>
                            </Button>
                        </div>
                    </div>
                </div>

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Gerar cardápio</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="grid gap-4 md:grid-cols-2">
                        <div className="flex flex-col">
                            <label htmlFor="inNutricionista">Nutricionista responsável</label>
                            <select
                                id="inNutricionista"
                                value={nutricionistaId}
                                onChange={(evento) =>
                                    setNutricionistaId(evento.target.value === "" ? "" : Number(evento.target.value))
                                }
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            >
                                <option value="">Selecione a nutricionista</option>
                                {nutricionistas.map((nutricionista) => (
                                    <option key={nutricionista.id} value={nutricionista.id}>
                                        {nutricionista.nome}
                                        {nutricionista.crn ? ` — CRN ${nutricionista.crn}` : ""}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="flex flex-col">
                            <label htmlFor="inNome">Nome do cardápio</label>
                            <input
                                id="inNome"
                                value={nome}
                                onChange={(evento) => setNome(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            />
                        </div>
                        <div className="flex flex-col">
                            <label htmlFor="inSeed">Semente (opcional)</label>
                            <input
                                id="inSeed"
                                type="number"
                                value={seed}
                                onChange={(evento) => setSeed(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            />
                        </div>
                        <div className="flex flex-col">
                            <label htmlFor="inInicio">Início</label>
                            <input
                                id="inInicio"
                                type="date"
                                value={inicio}
                                onChange={(evento) => setInicio(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            />
                        </div>
                        <div className="flex flex-col">
                            <label htmlFor="inFim">Fim</label>
                            <input
                                id="inFim"
                                type="date"
                                value={fim}
                                onChange={(evento) => setFim(evento.target.value)}
                                className="h-12 rounded-2xl border border-gray-200 bg-white px-4 text-sm text-gray-600 outline-none focus:border-roxo focus:ring-4 focus:ring-roxo/10"
                            />
                        </div>
                    </div>

                    <div className="mt-4 flex justify-start">
                        <Button type="button" variant="normal" onClick={gerar} disabled={carregando}>
                            <span className="flex items-center gap-2">
                                <Sparkles size={18} />
                                {carregando ? "Gerando..." : "Gerar cardápio"}
                            </span>
                        </Button>
                    </div>

                    {erro && (
                        <p role="alert" className="mt-4 text-red-700">
                            {erro}
                        </p>
                    )}

                    {mensagem && (
                        <p role="status" className="mt-4 text-green-700">
                            {mensagem}
                        </p>
                    )}

                    {resultado && (
                        <div className="mt-4">
                            <p className="text-sm text-gray-700">
                                <button
                                    type="button"
                                    onClick={() => navigate(`/cardapios/${resultado.cardapioId}`)}
                                    className="text-roxo underline"
                                >
                                    Abrir rascunho #{resultado.cardapioId}
                                </button>
                            </p>
                            {resultado.avisos.length > 0 && (
                                <ul className="mt-2 list-disc pl-6 text-sm text-amber-700">
                                    {resultado.avisos.map((aviso) => (
                                        <li key={aviso}>{aviso}</li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    )}
                </div>
            </div>

            <Footer />
        </div>
    );
}
