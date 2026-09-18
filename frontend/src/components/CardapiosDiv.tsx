import { Apple, Coffee, Moon, Utensils, ChevronRight, type LucideIcon } from "lucide-react";


type CardapiosDivVariant = "cafe" | "almoco" | "lanche" | "jantar"

export interface AlimentoCardapio {
    id: number;
    nome: string;
    calorias: number;
    carboidratos: number;
    proteinas: number;
    gorduras: number;
    fibras: number;
    sodio: number;
}

interface CardapiosDivProps {
    variant?: CardapiosDivVariant;
    alimentos?: AlimentoCardapio[];
    onClick?: () => void;
}

export function CardapiosDiv({    variant = "cafe",
    onClick, alimentos = [] }: CardapiosDivProps) {

    const styles: Record<CardapiosDivVariant, string> = { 
        cafe: "w-full text-primaria rounded-3xl bg-white p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] border border-gray-100 transition-all duration-200 hover:-translate-y-1 hover:shadow-[0_12px_35px_rgba(11,102,25,0.12)]",
        almoco: "w-full text-amarelo rounded-3xl bg-white p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] border border-gray-100 transition-all duration-200 hover:-translate-y-1 hover:shadow-[0_12px_35px_rgba(11,102,25,0.12)]",
        lanche: "w-full text-laranja rounded-3xl bg-white p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] border border-gray-100 transition-all duration-200 hover:-translate-y-1 hover:shadow-[0_12px_35px_rgba(11,102,25,0.12)]",
        jantar: "w-full text-roxo rounded-3xl bg-white p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)] border border-gray-100 transition-all duration-200 hover:-translate-y-1 hover:shadow-[0_12px_35px_rgba(11,102,25,0.12)]",
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
                <h3 className="text-lg font-bold text-gray-800 my-2">
                    {nomes[variant]}
                </h3>

                {alimentos.length > 0 ? (
                    <div className="flex flex-col gap-1">
                        {alimentos.map((alimento) => (
                            <p key={alimento.id} className="text-sm text-gray-500">
                                - {alimento.nome}
                            </p>
                        ))}
                    </div>
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