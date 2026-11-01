import { useEffect, useState } from "react";
import { ChevronLeft, Save } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import Button from "../../../components/Button";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import {
    atualizarAlimento,
    criarAlimento,
    listarCategoriasAlimento,
    obterAlimento,
    restricaoIdsDe,
    type CategoriaAlimento,
    type ValoresNutricionais,
} from "../../../services/AlimentoApi";
import { listarRestricoes, type Restricao } from "../../../services/RestricaoApi";

type CamposNutricionais = Record<keyof ValoresNutricionais, string>;

const medidas: { campo: keyof ValoresNutricionais; rotulo: string; unidade: string }[] = [
    { campo: "caloriasKcal", rotulo: "Calorias", unidade: "kcal" },
    { campo: "proteinasG", rotulo: "Proteínas", unidade: "g" },
    { campo: "carboidratosG", rotulo: "Carboidratos", unidade: "g" },
    { campo: "gordurasG", rotulo: "Gorduras", unidade: "g" },
    { campo: "fibrasG", rotulo: "Fibras", unidade: "g" },
    { campo: "sodioMg", rotulo: "Sódio", unidade: "mg" },
];

const valoresIniciais: CamposNutricionais = {
    caloriasKcal: "0",
    proteinasG: "0",
    carboidratosG: "0",
    gordurasG: "0",
    fibrasG: "0",
    sodioMg: "0",
};

function paraNumero(texto: string): number {
    return Number(texto.replace(",", "."));
}

/**
 * Cadastro e edição de alimento (Spec 011, RF10), no mesmo padrão visual do cadastro de
 * alunos: cabeçalho, cartões de seção com divisor e ações ao final.
 *
 * O vínculo envia apenas `restricaoId` — a severidade é a categoria da restrição, não
 * uma escolha do formulário (RF4).
 */
export function FormularioAlimento() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const editando = Boolean(id);

    const [categorias, setCategorias] = useState<CategoriaAlimento[]>([]);
    const [restricoes, setRestricoes] = useState<Restricao[]>([]);
    const [nome, setNome] = useState("");
    const [categoriaId, setCategoriaId] = useState<number | "">("");
    const [valores, setValores] = useState<CamposNutricionais>(valoresIniciais);
    const [selecionadas, setSelecionadas] = useState<number[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);

    useEffect(() => {
        async function carregar() {
            try {
                const [listaCategorias, listaRestricoes] = await Promise.all([
                    listarCategoriasAlimento(true),
                    listarRestricoes(),
                ]);

                setCategorias(listaCategorias);
                setRestricoes(listaRestricoes);

                if (id) {
                    const alimento = await obterAlimento(Number(id));

                    setNome(alimento.nome);
                    setCategoriaId(alimento.categoria.id);
                    setValores({
                        caloriasKcal: String(alimento.valores.caloriasKcal),
                        proteinasG: String(alimento.valores.proteinasG),
                        carboidratosG: String(alimento.valores.carboidratosG),
                        gordurasG: String(alimento.valores.gordurasG),
                        fibrasG: String(alimento.valores.fibrasG),
                        sodioMg: String(alimento.valores.sodioMg),
                    });
                    setSelecionadas(restricaoIdsDe(alimento));
                } else {
                    setCategoriaId(listaCategorias.length > 0 ? listaCategorias[0].id : "");
                }
            } catch (falha) {
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar o formulário");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [id]);

    function alternarRestricao(restricaoId: number) {
        setSelecionadas((atuais) =>
            atuais.includes(restricaoId)
                ? atuais.filter((atual) => atual !== restricaoId)
                : [...atuais, restricaoId],
        );
    }

    async function enviar() {
        setErro(null);

        if (categoriaId === "") {
            setErro("Selecione a categoria do alimento.");
            return;
        }

        const medidasConvertidas = Object.fromEntries(
            medidas.map(({ campo }) => [campo, paraNumero(valores[campo])]),
        ) as unknown as ValoresNutricionais;

        if (Object.values(medidasConvertidas).some((valor) => Number.isNaN(valor))) {
            setErro("Informe as seis medidas nutricionais com números válidos.");
            return;
        }

        const entrada = {
            nome,
            categoriaId,
            valores: medidasConvertidas,
            restricaoIds: selecionadas,
        };

        setSalvando(true);

        try {
            if (editando) {
                await atualizarAlimento(Number(id), entrada);
            } else {
                await criarAlimento(entrada);
            }

            navigate("/alimentos");
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Erro ao salvar o alimento");
        } finally {
            setSalvando(false);
        }
    }

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate("/alimentos")}
                aria-label="Voltar ao catálogo"
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-10">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">
                        {editando ? "Editar alimento" : "Informações do alimento"}
                    </h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    {carregando ? (
                        <p className="py-4 text-gray-500">Carregando...</p>
                    ) : (
                        <div className="flex gap-4">
                            <input
                                type="text"
                                value={nome}
                                onChange={(evento) => setNome(evento.target.value)}
                                placeholder="Nome do alimento"
                                aria-label="Nome do alimento"
                                maxLength={100}
                                autoComplete="off"
                                className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                            />

                            <select
                                value={categoriaId}
                                onChange={(evento) => setCategoriaId(Number(evento.target.value))}
                                aria-label="Categoria"
                                className="w-full rounded-2xl border border-gray-200 bg-white text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 my-2"
                            >
                                <option value="">Categoria</option>
                                {categorias.map((categoria) => (
                                    <option
                                        key={categoria.id}
                                        value={categoria.id}
                                        disabled={!categoria.ativa}
                                    >
                                        {categoria.nome}
                                        {categoria.ativa ? "" : " (desativada)"}
                                    </option>
                                ))}
                            </select>
                        </div>
                    )}
                </div>

                {!carregando && (
                    <>
                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            <h2 className="font-bold text-2xl">Composição por 100 g</h2>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                {medidas.map(({ campo, rotulo, unidade }) => (
                                    <input
                                        key={campo}
                                        type="number"
                                        min="0"
                                        step="0.01"
                                        value={valores[campo]}
                                        onChange={(evento) =>
                                            setValores((atuais) => ({
                                                ...atuais,
                                                [campo]: evento.target.value,
                                            }))
                                        }
                                        placeholder={`${rotulo} (${unidade})`}
                                        aria-label={`${rotulo} em ${unidade} por 100 g`}
                                        className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                    />
                                ))}
                            </div>
                        </div>

                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            <h2 className="font-bold text-2xl">Restrições e alergias</h2>
                            <p className="text-sm text-gray-500 mt-1">
                                Alergia bloqueia o uso no cardápio; restrição apenas alerta.
                            </p>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            {restricoes.length === 0 ? (
                                <p className="text-sm text-gray-500">
                                    Nenhuma restrição ativa no catálogo.
                                </p>
                            ) : (
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-3 w-full">
                                    {restricoes.map((restricao) => {
                                        const selecionado = selecionadas.includes(restricao.id);

                                        return (
                                            <button
                                                type="button"
                                                key={restricao.id}
                                                onClick={() => alternarRestricao(restricao.id)}
                                                aria-pressed={selecionado}
                                                className={`group flex items-center gap-3 rounded-xl px-3 py-3 text-left text-sm font-semibold transition-all duration-200 ${
                                                    selecionado
                                                        ? "bg-roxo text-white shadow-sm"
                                                        : "text-roxo hover:bg-[rgba(172,178,173,0.07)] hover:text-amarelo hover:shadow-sm"
                                                }`}
                                            >
                                                <span
                                                    className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xs font-bold shadow-sm transition-all duration-200 ${
                                                        selecionado
                                                            ? "bg-white text-roxo"
                                                            : "bg-white text-roxo group-hover:bg-roxo group-hover:text-white"
                                                    }`}
                                                >
                                                    {restricao.categoria === "Alergia" ? "AL" : "RE"}
                                                </span>

                                                <span>
                                                    {restricao.nome}
                                                    <span
                                                        className={
                                                            selecionado
                                                                ? " text-white/70"
                                                                : " text-gray-400"
                                                        }
                                                    >
                                                        {" "}
                                                        · {restricao.categoria}
                                                    </span>
                                                </span>
                                            </button>
                                        );
                                    })}
                                </div>
                            )}

                            <div className="border-verdeLodo border-2 rounded-3xl p-4 flex mt-6">
                                <div className="my-5 w-4 bg-verdeLodo rounded-3xl mr-4" />
                                <div>
                                    <h2 className="font-bold text-2xl text-amarelo">Importante!</h2>
                                    <p>
                                        A severidade do vínculo vem da categoria da restrição, não
                                        do formulário: alergia bloqueia o alimento no cardápio e
                                        restrição apenas alerta. As medidas são sempre por 100 g.
                                    </p>
                                </div>
                            </div>
                        </div>

                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            {erro && (
                                <div
                                    role="alert"
                                    className="mb-4 rounded-2xl bg-red-50 p-4 text-center font-semibold text-red-700"
                                >
                                    {erro}
                                </div>
                            )}

                            <div className="flex justify-center gap-4 m-4">
                                <Button
                                    type="button"
                                    variant="normal"
                                    onClick={enviar}
                                    disabled={salvando}
                                >
                                    <span className="flex items-center gap-2">
                                        <Save size={18} />
                                        {salvando ? "Salvando..." : "Salvar alimento"}
                                    </span>
                                </Button>

                                <Button
                                    type="button"
                                    variant="cancelar"
                                    onClick={() => navigate("/alimentos")}
                                    disabled={salvando}
                                >
                                    Cancelar
                                </Button>
                            </div>
                        </div>
                    </>
                )}

                {carregando && erro && (
                    <div
                        role="alert"
                        className="rounded-2xl bg-red-50 p-4 text-center font-semibold text-red-700"
                    >
                        {erro}
                    </div>
                )}
            </div>

            <Footer />
        </div>
    );
}
