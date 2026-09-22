import Button from "../components/Button";
import { CardapiosDiv } from "../components/CardapiosDiv";
import Footer from "../components/Footer";
import Header from "../components/Header";
import { ChevronLeft } from "lucide-react";
import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { buscarCardapio, excluirCardapio, type Refeicao, type AlimentoCardapio } from "../services/CardapioApi";

const refeicoes: Refeicao[] = [
    "cafe",
    "almoco",
    "lanche",
    "jantar"
];

interface Responsavel {
    id: number;
    nome: string;
    cargo: string;
    crn: string;
}

export function Cardapio() {
    const navigate = useNavigate();

    const [cardapios, setCardapios] = useState<Record<Refeicao, AlimentoCardapio[]>
    >({
        cafe: [],
        almoco: [],
        lanche: [],
        jantar: []
    });

    const cancelar = () => {
        navigate("/");
    };

    const[dataSelecionada, setDataSelecionada] = useState("");
    const[tipoSelecionado, setTipoSelecionado] = useState("");
    const[responsavel, setResponsavel] = useState<Responsavel | null>(null);

    const gerarDatas = () => {
        const datas: string[] = []; 
        const inicio = new Date(2026, 7, 1);
        for (let i = 0; i < 12; i++) {
            const data = new Date(inicio);
            data.setDate(inicio.getDate() + i * 15);

            datas.push(data.toLocaleDateString("pt-BR"));
        }
        return datas;
    }

    const datas = gerarDatas();

    useEffect(() => {
        async function carregarCardapios() {
            try {
                const resultados = await Promise.all(
                    refeicoes.map(async (refeicao) => {
                        const alimentos = await buscarCardapio(refeicao);

                        return {
                            refeicao,
                            alimentos: alimentos
                        };
                    })
                );

                setCardapios((anterior) => ({
                    ...anterior,
                    ...Object.fromEntries(
                        resultados.map(({ refeicao, alimentos }) => [
                            refeicao,
                            alimentos
                        ])
                    )
                }));
            } catch (erro) {
                console.error("Erro ao carregar cardápios:", erro);
            }
        }

        carregarCardapios();
    }, []);

    useEffect(() => {
        const carregarCardapios = () => {
            const cafe = localStorage.getItem("cardapio_cafe");
            const almoco = localStorage.getItem("cardapio_almoco");
            const lanche = localStorage.getItem("cardapio_lanche");
            const jantar = localStorage.getItem("cardapio_jantar");

            setCardapios({
                cafe: cafe ? JSON.parse(cafe) : [],
                almoco: almoco ? JSON.parse(almoco) : [],
                lanche: lanche ? JSON.parse(lanche) : [],
                jantar: jantar ? JSON.parse(jantar) : [],
            });
        };
        carregarCardapios();
    }, []);

    const todosAlimentos = [
    ...cardapios.cafe,
    ...cardapios.almoco,
    ...cardapios.lanche,
    ...cardapios.jantar,
];

async function handleExcluirCardapio(refeicao: Refeicao) {
    try {
        await excluirCardapio(refeicao);

        setCardapios((anterior) => ({
            ...anterior,
            [refeicao]: []
        }));
    } catch (erro) {
        console.error("Erro ao excluir cardápio:", erro);
    }
}

function handleSalvarCardapio() {
    if (!tipoSelecionado || !dataSelecionada) {
        alert("Selecione o tipo e a data antes de salvar.");
        return;
    }

    const historico = JSON.parse(localStorage.getItem("historico_cardapios") || "[]");
    const novo = {
        id: Date.now(),
        refeicoes: cardapios,
        tipo: tipoSelecionado,
        data: dataSelecionada,
        dataCriacao: new Date().toISOString(),
    };
    localStorage.setItem("historico_cardapios", JSON.stringify([...historico, novo]));
    navigate("/");
}

const composicaoNutricional = todosAlimentos.reduce(
    (total, alimento) => ({
        calorias: total.calorias + (Number(alimento.calorias) || 0),
        carboidratos: total.carboidratos + (Number(alimento.carboidratos) || 0),
        proteinas: total.proteinas + (Number(alimento.proteinas) || 0),
        gorduras: total.gorduras + (Number(alimento.gorduras) || 0),
        fibras: total.fibras + (Number(alimento.fibras) || 0),
        sodio: total.sodio + (Number(alimento.sodio) || 0),
    }),
    {
        calorias: 0,
        carboidratos: 0,
        proteinas: 0,
        gorduras: 0,
        fibras: 0,
        sodio: 0,
    }
);

const energiaCarboidratos = composicaoNutricional.carboidratos * 4;
const energiaProteinas = composicaoNutricional.proteinas * 4;
const energiaGorduras = composicaoNutricional.gorduras * 9;

const energiaCalculada = energiaCarboidratos + energiaGorduras + energiaProteinas;

const percentualCarboidratos = energiaCalculada > 0 ? (energiaCarboidratos / energiaCalculada) * 100 : 0

const percentualProteinas = energiaCalculada > 0 ? (energiaProteinas / energiaCalculada) * 100 : 0

const percentualGorduras = energiaCalculada > 0 ? (energiaGorduras / energiaCalculada) * 100 : 0

const carboidratoCerto = percentualCarboidratos >= 55 && percentualCarboidratos <= 65;

const proteinaCerto = percentualProteinas >= 10 && percentualProteinas <= 15;

const gorduraCerto = percentualGorduras >= 15 && percentualGorduras <= 30;

const fibrasCerto = composicaoNutricional.fibras > 0;
const sodioCerto = composicaoNutricional.sodio <= 600;

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

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

            <div className="flex flex-col mx-30 mb-10 gap-10">
                <div className="bg-branco rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Configuração do cardápio</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-4">
                      
                        <select name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3" value={tipoSelecionado} onChange={(e) => setTipoSelecionado(e.target.value)}>
                            <option value="">Selecione uma Opcao</option>
                            <option value="creche-1-3">Creche (1 a 3 anos)</option>
                            <option value="creche-4-5">Creche (4 a 5 anos)</option>
                            <option value="integral">Escola Integral</option>
                            <option value="geral">Escola Geral</option>
                        </select>
                        
                    
                        <select name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 py-4" value={dataSelecionada} onChange={(e) => setDataSelecionada(e.target.value)}>
                            {datas.map((data, index) => (
                                <option key={index} value={data}>
                                    {data}
                                </option>
                            ))}
                        </select>

                        {responsavel && (
                            <div className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2">
                                <p>{responsavel.nome}</p>
                                <p>{responsavel.cargo}</p>
                                <p>{responsavel.crn}</p>
                            </div>
                        )}
                    </div>

                    <div className=" border-2 border-verdeClaro rounded-2xl p-4 bg-verdeClaro/15 my-4">

                        <h3 className="font-bold text-verdeEscuro">Requisitos PNAE</h3>
                        <p>Lorem ipsum dolor sit amet consectetur adipisicing elit. Illo a repellendus itaque earum corporis officia quae necessitatibus, quis eum commodi aliquam unde. Debitis ea minima fugiat aspernatur. Eligendi, iusto maxime.</p>

                    </div>
                
                </div>

                <div className="bg-branco rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">Refeições PNAE</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />
                    <div className="flex gap-4">
                        <CardapiosDiv variant="cafe" alimentos={cardapios.cafe} onClick={() => navigate("/cardapio/montar/cafe")} />

                        <CardapiosDiv variant="almoco" alimentos={cardapios.almoco} onClick={() => navigate("/cardapio/montar/almoco")} />

                        <CardapiosDiv variant="lanche" alimentos={cardapios.lanche} onClick={() => navigate("/cardapio/montar/lanche")} />

                        <CardapiosDiv variant="jantar" alimentos={cardapios.jantar} onClick={() => navigate("/cardapio/montar/jantar")} />
                    </div>

                </div>

                <div className="bg-branco rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl mb-2">Composição nutricional</h2>

                    <div className="space-y-3">
                        <div className="flex justify-between">
                            <p>Calorias</p>
                            <strong>{composicaoNutricional.calorias.toFixed(1)} kcal</strong>
                        </div>
                        <div className="flex justify-between">
                            <p>Carboidratos</p>
                            <strong>{composicaoNutricional.carboidratos.toFixed(1)} g</strong>
                        </div>
                        <div className="flex justify-between">
                            <p>Proteínas</p>
                            <strong>{composicaoNutricional.proteinas.toFixed(1)} g</strong>
                        </div>
                        <div className="flex justify-between">
                            <p>Gorduras totais</p>
                            <strong>{composicaoNutricional.gorduras.toFixed(1)} g</strong>
                        </div>
                        <div className="flex justify-between">
                            <p>Fibras</p>
                            <strong>{composicaoNutricional.fibras.toFixed(1)} g</strong>
                        </div>
                        <div className="flex justify-between">
                            <p>Sódio</p>
                            <strong>{composicaoNutricional.sodio.toFixed(1)} mg</strong>
                        </div>
                    </div>
                </div>

                <div className="bg-branco rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">

                <h2 className="font-bold text-2xl mb-2">
                    Analise de Conformidade PNAE
                </h2>

                <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                <div className="space-y-3 mt-4">
                    <div className={`flex justify-between items-center border p-4 rounded-2xl ${ carboidratoCerto ? "bg-green-100 border-green-500" : "bg-red-100 border-red-500"}`}>
                        <div>
                            <p className="font-bold">
                                Carboidratos
                            </p>

                            <p className="text-sm text-gray-500">
                                {percentualCarboidratos.toFixed(1)}% da energia
                            </p>

                            <p className="text-sm text-gray-500">
                                Referência: 55% – 65%
                            </p>
                        </div>

                        <strong
                            className={ carboidratoCerto? "text-green-600" : "text-red-600"}>
                            {carboidratoCerto? "✓ Conforme" : "✗ Não conforme"}
                        </strong>
                    </div>

                    <div className={`flex justify-between items-center border p-4 rounded-2xl ${ proteinaCerto ? "bg-green-100 border-green-500" : "bg-red-100 border-red-500"}`}>
                        <div>
                            <p className="font-bold">
                                Proteínas
                            </p>

                            <p className="text-sm text-gray-500">
                                {percentualProteinas.toFixed(1)}% da energia
                            </p>

                            <p className="text-sm text-gray-500">
                                Referência: 10% – 15%
                            </p>
                        </div>

                        <strong className={ proteinaCerto ? "text-green-600" : "text-red-600" }>
                            {proteinaCerto ? "✓ Conforme" : "✗ Não conforme"}
                        </strong>
                    </div>

                    <div className={`flex justify-between items-center border p-4 rounded-2xl ${ gorduraCerto ? "bg-green-100 border-green-500" : "bg-red-100 border-red-500"}`}>
                        <div>
                            <p className="font-bold">
                                Gorduras totais
                            </p>

                            <p className="text-sm text-gray-500">
                                {percentualGorduras.toFixed(1)}% da energia
                            </p>

                            <p className="text-sm text-gray-500">
                                Referência: 15% – 30%
                            </p>
                        </div>

                        <strong className={ gorduraCerto ? "text-green-600" : "text-red-600"}>
                            {gorduraCerto ? "✓ Conforme" : "✗ Não conforme"}
                        </strong>
                    </div>

                    <div className={`flex justify-between items-center border p-4 rounded-2xl ${ fibrasCerto ? "bg-green-100 border-green-500" : "bg-red-100 border-red-500"}`}>
                        <div>
                            <p className="font-bold">
                                Fibras
                            </p>

                            <p className="text-sm text-gray-500">
                                {composicaoNutricional.fibras.toFixed(1)} g
                            </p>
                        </div>

                        <strong className={ fibrasCerto ? "text-green-600" : "text-red-600"}>
                            {fibrasCerto ? "✓ Conforme" : "✗ Não conforme"}
                        </strong>
                    </div>

                    <div className={`flex justify-between items-center border p-4 rounded-2xl ${ sodioCerto ? "bg-green-100 border-green-500" : "bg-red-100 border-red-500" }`}>
                        <div>
                            <p className="font-bold">
                                Sódio
                            </p>

                            <p className="text-sm text-gray-500">
                                {composicaoNutricional.sodio.toFixed(1)} mg
                            </p>

                            <p className="text-sm text-gray-500">
                                Limite: 600 mg
                            </p>
                        </div>

                        <strong className={sodioCerto ? "text-green-600" : "text-red-600"}>
                            {sodioCerto ? "✓ Conforme" : "✗ Não conforme"}
                        </strong>
                    </div>
                </div>
            </div>

                <div className="bg-branco rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl mb-2">Exportar em Formatos PNAE</h2>
                    
                    <div className="flex gap-4">
                        <Button variant="normal" type="button">Exportar Excel</Button>
                        <Button variant="ver" type="button">Exportar Word</Button>
                        <Button variant="alteracao" type="button">Exportar PDF</Button>
                        <Button variant="link" type="button">Exportar Relatório FNDE</Button>
                    </div>

                </div>

                <div className="flex gap-3">
                    <Button type="button" onClick={handleSalvarCardapio}>
                        Salvar
                    </Button>
                </div>

            </div>

            <Footer />
        </div>
    );
}
