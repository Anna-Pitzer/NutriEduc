import { Apple, Coffee, Moon, Utensils, ChevronRight, type LucideIcon } from "lucide-react";


type CardapiosDivVariant = "cafe" | "almoco" | "lanche" | "jantar"

export interface AlimentoCardapio {
    id: number;
    nome: string;
    quantidade: number;
    unidade: string;
}

interface CardapiosDivProps {
    variant?: CardapiosDivVariant;
    alimentos?: AlimentoCardapio[];
    onClick?: () => void;
}

export function CardapiosDiv({    variant = "cafe",
    onClick, alimentos = [] }: CardapiosDivProps) {

    const styles: Record<CardapiosDivVariant, string> = { 
        cafe: "",
        almoco: "",
        lanche: "",
        jantar: "",
    }

    const nomes: Record<CardapiosDivVariant, string> = {
        cafe: "Café da manhã",
        almoco: "Almoço",
        lanche: "Lanche",
        jantar: "Jantar",
    }

    const Icones: Record<CardapiosDivVariant, LucideIcon> = {
        cafe: Coffee,
        almoco: Utensils,
        lanche: Apple,
        jantar: Moon,
    }

    const Icone = Icones[variant]

    return (

        <button type="button" onClick={onClick} className={styles[variant]}>

            <div className="flex items-center justify-center">
                <Icone size={32} strokeWidth={2} />
            </div>

            <div>
                <h3 className="text-lg font-bold text-gray-800">
                    {nomes[variant]}
                </h3>

                {alimentos.length > 0 ? (
                    <p>
                        {alimentos.map((alimento)=> alimento.nome).join(" * ")}
                    </p>
                ) : ( 
                <p className="text-sm text-gray-500">
                    Clique para montar o cardápio
                </p>)}
            </div>

            <ChevronRight
                size={22}
                className="shrink-0 text-gray-500"
            />

        </button>


    );
}