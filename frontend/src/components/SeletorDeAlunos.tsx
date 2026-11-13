import { useCallback, useEffect, useState } from "react";
import { Search } from "lucide-react";
import { buscarAlunos, type AlunoResposta } from "../services/AlunoApi";

const series = [
    "1 ano",
    "2 ano",
    "3 ano",
    "4 ano",
    "5 ano",
    "6 ano",
    "7 ano",
    "8 ano",
    "9 ano",
];

type EscolaOpcao = { id: number; nome: string };

/**
 * Busca e seleção de alunos para vínculo com responsável (Spec 034).
 *
 * Consome `GET /api/aluno?busca=&escolaId=&serie=`, que já devolve apenas o recorte
 * autorizado ao solicitante. A seleção é controlada pelo componente pai.
 */
export function SeletorDeAlunos({
    selecionados,
    aoAlternar,
    escolas = [],
    titulo = "Alunos",
}: {
    selecionados: number[];
    aoAlternar: (alunoId: number) => void;
    escolas?: EscolaOpcao[];
    titulo?: string;
}) {
    const [busca, setBusca] = useState("");
    const [escolaId, setEscolaId] = useState<number | "">("");
    const [serie, setSerie] = useState("");
    const [alunos, setAlunos] = useState<AlunoResposta[]>([]);
    const [carregando, setCarregando] = useState(false);
    const [erro, setErro] = useState("");

    const carregar = useCallback(async () => {
        setCarregando(true);
        setErro("");

        try {
            setAlunos(await buscarAlunos({ busca, escolaId, serie }));
        } catch (falha) {
            setAlunos([]);
            setErro(falha instanceof Error ? falha.message : "Não foi possível buscar os alunos.");
        } finally {
            setCarregando(false);
        }
    }, [busca, escolaId, serie]);

    useEffect(() => {
        const timer = setTimeout(() => {
            void carregar();
        }, 300);

        return () => clearTimeout(timer);
    }, [carregar]);

    return (
        <div className="space-y-2">
            <p className="text-sm font-medium text-gray-700">
                {titulo} <span className="text-gray-500">({selecionados.length} selecionado(s))</span>
            </p>

            <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
                <label className="flex h-10 flex-1 items-center gap-2 rounded-xl border border-gray-200 bg-white px-3 text-sm sm:min-w-56">
                    <Search size={16} className="text-gray-400" />
                    <input
                        type="text"
                        value={busca}
                        onChange={(evento) => setBusca(evento.target.value)}
                        placeholder="Buscar aluno pelo nome"
                        aria-label="Buscar aluno pelo nome"
                        className="w-full outline-none"
                    />
                </label>

                <select
                    value={escolaId}
                    onChange={(evento) =>
                        setEscolaId(evento.target.value === "" ? "" : Number(evento.target.value))
                    }
                    aria-label="Filtrar por escola"
                    className="h-10 rounded-xl border border-gray-200 bg-white px-3 text-sm outline-none focus:border-roxo"
                >
                    <option value="">Todas as escolas</option>
                    {escolas.map((escola) => (
                        <option key={escola.id} value={escola.id}>{escola.nome}</option>
                    ))}
                </select>

                <select
                    value={serie}
                    onChange={(evento) => setSerie(evento.target.value)}
                    aria-label="Filtrar por série"
                    className="h-10 rounded-xl border border-gray-200 bg-white px-3 text-sm outline-none focus:border-roxo"
                >
                    <option value="">Todas as séries</option>
                    {series.map((valor) => (
                        <option key={valor} value={valor}>{valor.replace(" ano", "° ano")}</option>
                    ))}
                </select>
            </div>

            {erro && <p role="alert" className="text-sm text-red-600">{erro}</p>}

            <div className="max-h-56 overflow-y-auto rounded-xl border border-gray-200 bg-white p-2">
                {carregando ? (
                    <p className="p-2 text-sm text-gray-500">Buscando alunos…</p>
                ) : alunos.length === 0 ? (
                    <p className="p-2 text-sm text-gray-500">Nenhum aluno encontrado.</p>
                ) : (
                    alunos.map((aluno) => (
                        <label
                            key={aluno.id}
                            className="flex cursor-pointer items-start gap-2 rounded-lg p-2 text-sm hover:bg-gray-50"
                        >
                            <input
                                type="checkbox"
                                className="mt-1"
                                checked={selecionados.includes(aluno.id)}
                                onChange={() => aoAlternar(aluno.id)}
                            />
                            <span>
                                <span className="font-medium text-gray-800">{aluno.nome}</span>
                                <span className="block text-xs text-gray-500">
                                    {[aluno.escola, aluno.serie].filter(Boolean).join(" · ")}
                                </span>
                            </span>
                        </label>
                    ))
                )}
            </div>
        </div>
    );
}
