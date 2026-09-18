import { useEffect, useState } from "react";
import { ChevronLeft, Plus, Trash2 } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { type AlimentoCardapio } from "../components/CardapiosDiv";
import Button from "../components/Button";
import background1 from "../assets/background1.png";

type CardapiosDivVariant = "cafe" | "almoco" | "lanche" | "jantar";

interface Alimento {
    id: number;
    nome: string;
    categoria: string;
    calorias: number;
    carboidratos: number;
    proteinas: number;
    gorduras: number;
    fibras: number;
    sodio: number;
}

const nomesRefeicoes: Record<CardapiosDivVariant, string> = {
    cafe: "Café da manhã",
    almoco: "Almoço",
    lanche: "Lanche",
    jantar: "Jantar",
};

const alimentosFake: Alimento[] = [
    {
        id: 1,
        nome: "Arroz branco",
        categoria: "Cereais",
        calorias: 130,
        carboidratos: 28.2,
        proteinas: 2.7,
        gorduras: 0.3,
        fibras: 0.4,
        sodio: 1,
    },
    {
        id: 2,
        nome: "Arroz integral",
        categoria: "Cereais",
        calorias: 123,
        carboidratos: 25.6,
        proteinas: 2.6,
        gorduras: 1.0,
        fibras: 1.6,
        sodio: 1,
    },
    {
        id: 3,
        nome: "Aveia",
        categoria: "Cereais",
        calorias: 394,
        carboidratos: 66.6,
        proteinas: 13.9,
        gorduras: 8.5,
        fibras: 9.1,
        sodio: 5,
    },
    {
        id: 4,
        nome: "Feijão carioca",
        categoria: "Leguminosas",
        calorias: 76,
        carboidratos: 13.6,
        proteinas: 4.8,
        gorduras: 0.5,
        fibras: 8.5,
        sodio: 2,
    },
    {
        id: 5,
        nome: "Lentilha",
        categoria: "Leguminosas",
        calorias: 93,
        carboidratos: 16.3,
        proteinas: 6.3,
        gorduras: 0.5,
        fibras: 7.9,
        sodio: 2,
    },
    {
        id: 6,
        nome: "Frango grelhado",
        categoria: "Carnes",
        calorias: 159,
        carboidratos: 0,
        proteinas: 32.0,
        gorduras: 3.2,
        fibras: 0,
        sodio: 74,
    },
    {
        id: 7,
        nome: "Carne moída",
        categoria: "Carnes",
        calorias: 212,
        carboidratos: 0,
        proteinas: 26.7,
        gorduras: 11.0,
        fibras: 0,
        sodio: 65,
    },
    {
        id: 8,
        nome: "Peixe assado",
        categoria: "Carnes",
        calorias: 136,
        carboidratos: 0,
        proteinas: 28.0,
        gorduras: 2.9,
        fibras: 0,
        sodio: 60,
    },
    {
        id: 9,
        nome: "Ovo cozido",
        categoria: "Ovos",
        calorias: 146,
        carboidratos: 0.6,
        proteinas: 13.3,
        gorduras: 9.5,
        fibras: 0,
        sodio: 146,
    },
    {
        id: 10,
        nome: "Alface",
        categoria: "Hortaliças",
        calorias: 15,
        carboidratos: 2.9,
        proteinas: 1.4,
        gorduras: 0.2,
        fibras: 1.8,
        sodio: 9,
    },
    {
        id: 11,
        nome: "Tomate",
        categoria: "Hortaliças",
        calorias: 15,
        carboidratos: 3.1,
        proteinas: 1.1,
        gorduras: 0.2,
        fibras: 1.2,
        sodio: 5,
    },
    {
        id: 12,
        nome: "Cenoura",
        categoria: "Hortaliças",
        calorias: 34,
        carboidratos: 7.7,
        proteinas: 1.3,
        gorduras: 0.2,
        fibras: 3.2,
        sodio: 3,
    },
    {
        id: 13,
        nome: "Beterraba",
        categoria: "Hortaliças",
        calorias: 49,
        carboidratos: 11.1,
        proteinas: 1.9,
        gorduras: 0.1,
        fibras: 3.4,
        sodio: 77,
    },
    {
        id: 14,
        nome: "Banana",
        categoria: "Frutas",
        calorias: 98,
        carboidratos: 26.0,
        proteinas: 1.3,
        gorduras: 0.1,
        fibras: 2.0,
        sodio: 1,
    },
    {
        id: 15,
        nome: "Maçã",
        categoria: "Frutas",
        calorias: 56,
        carboidratos: 15.2,
        proteinas: 0.3,
        gorduras: 0.1,
        fibras: 1.3,
        sodio: 0,
    },
    {
        id: 16,
        nome: "Mamão",
        categoria: "Frutas",
        calorias: 40,
        carboidratos: 10.4,
        proteinas: 0.5,
        gorduras: 0.1,
        fibras: 1.0,
        sodio: 3,
    },
    {
        id: 17,
        nome: "Laranja",
        categoria: "Frutas",
        calorias: 37,
        carboidratos: 8.9,
        proteinas: 1.0,
        gorduras: 0.1,
        fibras: 0.8,
        sodio: 1,
    },
    {
        id: 18,
        nome: "Pão francês",
        categoria: "Pães",
        calorias: 300,
        carboidratos: 58.6,
        proteinas: 8.0,
        gorduras: 3.1,
        fibras: 2.3,
        sodio: 648,
    },
    {
        id: 19,
        nome: "Pão integral",
        categoria: "Pães",
        calorias: 253,
        carboidratos: 49.9,
        proteinas: 9.4,
        gorduras: 3.7,
        fibras: 6.9,
        sodio: 450,
    },
    {
        id: 20,
        nome: "Leite",
        categoria: "Laticínios",
        calorias: 61,
        carboidratos: 4.8,
        proteinas: 3.2,
        gorduras: 3.3,
        fibras: 0,
        sodio: 43,
    },
    {
        id: 21,
        nome: "Iogurte natural",
        categoria: "Laticínios",
        calorias: 61,
        carboidratos: 4.7,
        proteinas: 3.5,
        gorduras: 3.3,
        fibras: 0,
        sodio: 46,
    },
    {
        id: 22,
        nome: "Queijo",
        categoria: "Laticínios",
        calorias: 264,
        carboidratos: 3.1,
        proteinas: 17.4,
        gorduras: 20.3,
        fibras: 0,
        sodio: 560,
    },
    {
        id: 23,
        nome: "Suco de laranja",
        categoria: "Bebidas",
        calorias: 42,
        carboidratos: 9.8,
        proteinas: 0.7,
        gorduras: 0.2,
        fibras: 0.2,
        sodio: 1,
    },
    {
        id: 24,
        nome: "Água",
        categoria: "Bebidas",
        calorias: 0,
        carboidratos: 0,
        proteinas: 0,
        gorduras: 0,
        fibras: 0,
        sodio: 0,
    },
];

export function MontarCardapio() {
    const navigate = useNavigate();
    const { refeicao } = useParams<{ refeicao: CardapiosDivVariant; }>();
    const [alimentosDisponiveis, setAlimentosDisponiveis] = useState<Alimento[]>([]);
    const [alimentos, setAlimentos] = useState<AlimentoCardapio[]>([]);
    const [alimentoSelecionado, setAlimentoSelecionado] = useState("");
    const [carregando, setCarregando] = useState(true);

    useEffect(() => {

        setAlimentosDisponiveis(alimentosFake);
        setCarregando(false);
        /*
                const buscarAlimentos = async () => {
                    try {
                        const token = localStorage.getItem("token");
        
                        const resposta = await fetch(
                            "http://localhost:3000/alimentos",
                            {
                                headers: {
                                    "Content-Type": "application/json",
                                    Authorization: `Bearer ${token}`,
                                },
                            }
                        );
        
                        if (!resposta.ok) {
                            throw new Error("Erro ao buscar alimentos");
                        }
        
                        const dados: Alimento[] = await resposta.json();
        
                        setAlimentosDisponiveis(dados);
                    } catch (error) {
                        console.error("Erro ao buscar alimentos:", error);
                    } finally {
                        setCarregando(false);
                    }
                };
        
                buscarAlimentos();
                */
    }, []);

    useEffect(() => {
        if (!refeicao) {
            return;
        }

        const salvo = localStorage.getItem(`cardapio_${refeicao}`);

        if (!salvo) {
            return;
        }

        try {
            const dados: AlimentoCardapio[] = JSON.parse(salvo);

            setAlimentos(dados);
        } catch (error) {
            console.error("Erro ao carregar cardápio:", error);
        }
    }, [refeicao]);

    const alimentoPorCategoria = alimentosDisponiveis.reduce<
        Record<string, Alimento[]>
    >((grupos, alimento) => {
        const categoria = alimento.categoria || "Outros";

        if (!grupos[categoria]) {
            grupos[categoria] = [];
        }

        grupos[categoria].push(alimento);

        return grupos;
    }, {});

    const adicionarAlimento = () => {
        if (!alimentoSelecionado) {
            alert("Selecione um alimento");
            return;
        }

        const alimento = alimentosDisponiveis.find(
            (item) => item.id.toString() === alimentoSelecionado
        );

        if (!alimento) {
            alert("Alimento não encontrado");
            return;
        }

        const novoAlimento: AlimentoCardapio = {
            id: alimento.id,
            nome: alimento.nome,
            calorias: alimento.calorias,
            carboidratos: alimento.carboidratos,
            proteinas: alimento.proteinas,
            gorduras: alimento.gorduras,
            fibras: alimento.fibras,
            sodio: alimento.sodio,
        };

        setAlimentos((listaAtual) => [
            ...listaAtual,
            novoAlimento,
        ]);

        setAlimentoSelecionado("");
    };

    const removerAlimento = (index: number) => {
        setAlimentos((listaAtual) =>
            listaAtual.filter(
                (_, indice) => indice !== index
            )
        );
    };

    const salvarCardapio = () => {
        if (!refeicao) {
            alert("Refeição inválida");
            return;
        }

        localStorage.setItem(
            `cardapio_${refeicao}`,
            JSON.stringify(alimentos)
        );

        alert(
            `${nomesRefeicoes[refeicao]} salvo com sucesso!`
        );

        navigate("/cardapio");
    };

    const cancelar = () => {
        navigate("/cardapio");
    };

    if (
        !refeicao ||
        !["cafe", "almoco", "lanche", "jantar"].includes(refeicao)
    ) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-branco">
                <div className="text-center">
                    <h2 className="text-2xl font-bold text-red-600">
                        Refeição inválida
                    </h2>

                    <Button type="button" variant="cancelar" onClick={cancelar} disabled={carregando}>
                        Cancelar
                    </Button>
                </div>
            </div>
        );
    }

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <div
                className="fixed inset-0 z-0 bg-cover bg-center bg-no-repeat opacity-60"
                style={{
                    backgroundImage: `url(${background1})`,
                }}
            />

            <div className="relative z-20 mt-10 mx-30">
                <div className="bg-[#FAF9F5] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)] ">
                    <button
                        type="button"
                        onClick={cancelar}
                        className="group flex items-center justify-center cursor-pointer mx-10"
                    >
                        <ChevronLeft
                            size={50}
                            className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                        />
                    </button>
                    <div>
                        <div className="max-w-6xl mx-auto">
                            <div className="mb-8">
                                <p className="text-sm text-gray-500 mb-1">
                                    Montagem do cardápio
                                </p>
                                <h1 className="text-3xl font-bold text-gray-800">
                                    {nomesRefeicoes[refeicao]}
                                </h1>
                                <p className="text-gray-500 mt-2">
                                    Adicione os alimentos
                                </p>
                            </div>
                            <div className="bg-white rounded-3xl p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] mb-8">
                                <h2 className="text-xl font-bold text-gray-800 mb-5">
                                    Adicionar alimento
                                </h2>
                                {carregando ? (
                                    <p className="text-gray-500">
                                        Carregando alimentos...
                                    </p>
                                ) : (
                                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                        <div>
                                            <label
                                                htmlFor="alimento"
                                                className="block text-sm font-medium text-gray-700 mb-2"
                                            >
                                                Alimento
                                            </label>
                                            <select
                                                id="alimento"
                                                value={alimentoSelecionado}
                                                onChange={(e) =>
                                                    setAlimentoSelecionado(
                                                        e.target.value
                                                    )
                                                }
                                                className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition hover:border-gray-300 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer"
                                            >
                                                <option value="">
                                                    Selecione um alimento
                                                </option>
                                                {Object.entries(
                                                    alimentoPorCategoria
                                                ).map(
                                                    ([categoria,alimentosCategoria,]) => (
                                                        <optgroup key={categoria} label={categoria}>
                                                            {alimentosCategoria.map(
                                                                (alimento) => (
                                                                    <option key={alimento.id} value={alimento.id}>
                                                                        {alimento.nome}
                                                                    </option>
                                                                )
                                                            )}
                                                        </optgroup>
                                                    )
                                                )}
                                            </select>
                                        </div>

                                        <div className="flex items-end">
                                            <Button type="button" variant="normal" onClick={adicionarAlimento} disabled={carregando}>
                                                    <Plus size={20} />
                                                    Adicionar Alimento
                                            </Button>
                                        </div>
                                    </div>
                                )}
                            </div>
                            <div className="bg-white rounded-3xl p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                                <div className="flex items-center justify-between mb-5">
                                    <div>
                                        <h2 className="text-xl font-bold text-gray-800">
                                            Alimentos da refeição
                                        </h2>
                                        <p className="text-sm text-gray-500 mt-1">
                                            {alimentos.length}{" "}
                                            {alimentos.length === 1 ? "alimento" : "alimentos"}{" "} adicionado
                                            {alimentos.length === 1 ? "" : "s"}
                                        </p>
                                    </div>
                                </div>
                        
                                {alimentos.length === 0 ? (
                                    <div className="rounded-2xl border-2 border-dashed border-gray-200 p-10 text-center">
                                        <p className="text-gray-500">
                                            Nenhum alimento adicionado
                                            ainda.
                                        </p>
                                        <p className="text-sm text-gray-400 mt-1">
                                            Selecione um alimento acima
                                            para começar.
                                        </p>
                                    </div>
                                ) : (
                                    <div className="space-y-3">
                                        {alimentos.map(
                                            (alimento, index) => (
                                                <div
                                                    key={`${alimento.id}-${index}`}
                                                    className="flex items-center justify-between gap-4 rounded-2xl border border-gray-200 bg-gray-50 px-5 py-4"
                                                >
                                                    <div>
                                                        <h3 className="font-semibold text-gray-800">
                                                            {
                                                                alimento.nome
                                                            }
                                                        </h3>
                                                    </div>
                                                    <Button type="button" onClick={() => removerAlimento(index)} variant="apagar">
                                                        <Trash2 size={20} />
                                                    </Button>
                                                </div>
                                            )
                                        )}
                                    </div>
                                )}
                                <div className="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 mt-8 pt-6 border-t border-gray-200">
                                    <Button type="button" onClick={cancelar} variant="cancelar">
                                        Cancelar
                                    </Button>
                                    <Button type="button" onClick={salvarCardapio} variant="ver">
                                        Salvar refeição
                                    </Button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}