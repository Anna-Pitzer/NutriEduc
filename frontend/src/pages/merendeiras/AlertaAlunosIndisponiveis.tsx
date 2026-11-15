import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertTriangle } from "lucide-react";
import {
    listarAdaptados,
    listarCardapiosPublicados,
    type CardapioAdaptadoResumo,
} from "../../services/CardapioApi";

interface VersaoIndisponivel {
    geralId: number;
    geralNome: string;
    escolaNome: string;
    conjunto: string;
    status: string;
}

function statusLegivel(versao: CardapioAdaptadoResumo): string {
    if (versao.statusAdaptacao === "PendenteDeRevisao") {
        return "pendente de revisão";
    }

    if (versao.statusAdaptacao === "Desatualizado") {
        return "desatualizada";
    }

    return versao.situacao === "Rascunho" ? "não publicada" : versao.situacao.toLowerCase();
}

/**
 * Alerta da merendeira (Spec 029, RF15 e T13): mostra as versões adaptadas que não
 * podem ser servidas. Um aluno com restrição cujo conjunto não tem versão publicada
 * fica `Indisponivel` — o geral nunca é usado como fallback.
 */
export function AlertaAlunosIndisponiveis({ unidadeId }: { unidadeId: number | null }) {
    const navigate = useNavigate();
    const [indisponiveis, setIndisponiveis] = useState<VersaoIndisponivel[]>([]);

    useEffect(() => {
        async function carregar() {
            if (unidadeId == null) {
                setIndisponiveis([]);
                return;
            }

            try {
                const publicados = await listarCardapiosPublicados({ unidadeId });
                const gerais = publicados.itens.filter((cardapio) => cardapio.tipo === "Geral");

                const versoes = await Promise.all(
                    gerais.map(async (geral) => ({
                        geral,
                        adaptados: await listarAdaptados(geral.id).catch(() => []),
                    })),
                );

                const alertas: VersaoIndisponivel[] = [];

                for (const { geral, adaptados } of versoes) {
                    for (const versao of adaptados) {
                        const bloqueada =
                            versao.situacao !== "Publicado" ||
                            versao.statusAdaptacao === "PendenteDeRevisao" ||
                            versao.statusAdaptacao === "Desatualizado";

                        if (bloqueada) {
                            alertas.push({
                                geralId: geral.id,
                                geralNome: geral.nome,
                                escolaNome: versao.escolaNome,
                                conjunto: versao.conjuntoRestricoesChave,
                                status: statusLegivel(versao),
                            });
                        }
                    }
                }

                setIndisponiveis(alertas);
            } catch {
                // O alerta é complementar: falha ao carregar não derruba a tela da merendeira.
                setIndisponiveis([]);
            }
        }

        carregar();
    }, [unidadeId]);

    if (indisponiveis.length === 0) {
        return null;
    }

    return (
        <div className="w-full max-w-7xl mx-auto mb-10 rounded-3xl border border-red-200 bg-red-50 p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
            <h2 className="flex items-center gap-2 font-bold text-xl text-red-800">
                <AlertTriangle size={22} />
                Alunos com restrição sem cardápio publicado
            </h2>

            <p className="mt-1 text-sm text-red-700">
                Estas versões não podem ser servidas. O cardápio geral não substitui a versão
                adaptada.
            </p>

            <ul className="mt-3 flex flex-col gap-2">
                {indisponiveis.map((item) => (
                    <li
                        key={`${item.geralId}-${item.escolaNome}-${item.conjunto}`}
                        className="rounded-2xl border border-red-200 bg-white p-3 text-sm"
                    >
                        <p className="font-semibold text-gray-800">
                            {item.escolaNome} · conjunto {item.conjunto}
                        </p>
                        <p className="text-red-700">
                            {item.status} · cardápio &quot;{item.geralNome}&quot;
                        </p>
                        <button
                            type="button"
                            onClick={() => navigate(`/cardapios/${item.geralId}`)}
                            className="mt-1 text-roxo underline hover:text-purple-700"
                        >
                            Ver versão adaptada
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
}
