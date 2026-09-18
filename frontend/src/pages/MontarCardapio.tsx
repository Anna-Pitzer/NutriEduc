import { useEffect, useState } from "react";
import { ChervonLeft, Plus, Trash2, } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { CardapiosDivVariant, type AlimentoCardapio } from "../components/CardapiosDiv";

interface Alimento {
    id: number;
    nome: string;
    categoria: string;
}

const nomesRefeicoes: Record<CardapiosDivVariant, string> = {
    cafe: "Café da manhã",
    almoco: "Almoço",
    lanche: "Lanche",
    jantar: "Jantar",
};

export function MontarCardapio() {

    const navigate = useNavigate();

    const { refeicao } = useParams<{
        refeicao: CardapiosDivVariant;
    }>();

    const [alimentosDisponeiveis, setAlimentosDisponiveis] = useState<Alimento[]>([]);
    const[alimentos, setAlimentos] = useState<AlimentoCardapio[]>([]);
    const[alimentoSelecionado, setAlimentoSelecionado] = useState("");
    const[quantidade, setQuantidade] = useState("");
    const[unidade, setUnidade] = useState("");
    const[carregando, setCarregando] = useState(true);

    useEffect(() => {
        const buscarAlimentos = async () => {
            try {
                const token = localStorage.getItem("token");

                const resposta = await fetch(                    "http://localhost:3000/alimentos",
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
    }, []);

    useEffect(() => {
        if (!refeicao) {
            return;
        }

        const salvo = localStorage.getItem(`cardapio_${refeicao}`);

        if (!salvo) return

        try {
            const dados: AlimentoCardapio[] = JSON.parse(salvo);
            setAlimentos(dados);
        } catch (error) {
            console.error("Erro ao carregar cardapio");
        }
    }, [refeicao]);

    const alimentoPorCategoria = alimentosDisponeiveis.reduce<Record<string, Alimento[]>>((grupos, alimento) => {
        const categoria = alimento.categoria || "Outros";

        if (!grupos[categoria]) {
            grupos[categoria] = [];
        }

        grupos[categoria].push(alimento);
        return grupos;
    }, {});

    
    return (

    );
}