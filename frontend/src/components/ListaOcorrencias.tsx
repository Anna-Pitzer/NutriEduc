import type {
    OcorrenciaResumo,
    StatusOcorrencia,
    TipoOcorrencia,
} from "../services/OcorrenciaApi";

export const rotuloTipoOcorrencia: Record<TipoOcorrencia, string> = {
    Aluno: "Aluno",
    Refeicao: "Refeição",
    Alimento: "Alimento",
    Outro: "Outro",
};

export const rotuloStatusOcorrencia: Record<StatusOcorrencia, string> = {
    Aberta: "Aberta",
    EmAnalise: "Em análise",
    Resolvida: "Resolvida",
    Arquivada: "Arquivada",
    Cancelada: "Cancelada",
};

export const corStatusOcorrencia: Record<StatusOcorrencia, string> = {
    Aberta: "bg-sky-50 text-sky-700",
    EmAnalise: "bg-amber-50 text-amber-700",
    Resolvida: "bg-emerald-50 text-emerald-700",
    Arquivada: "bg-slate-100 text-slate-600",
    Cancelada: "bg-red-50 text-red-700",
};

/** Datas sempre em `dd/MM/yyyy` (Spec 035, RF32). */
export function formatarData(valor: string): string {
    const data = new Date(valor);

    return Number.isNaN(data.getTime()) ? valor : data.toLocaleDateString("pt-BR");
}

/**
 * Lista reutilizável de ocorrências (Spec 035, T11), consumida pela área dos pais e pela
 * página compartilhada de ocorrências. A autorização nunca é decidida aqui.
 */
export function ListaOcorrencias({
    itens,
    onSelecionar,
    carregando,
    selecionadaId = null,
}: {
    itens: OcorrenciaResumo[];
    onSelecionar: (id: number) => void;
    carregando: boolean;
    selecionadaId?: number | null;
}) {
    if (carregando) {
        return <p className="mt-4 text-sm text-slate-500">Carregando...</p>;
    }

    if (itens.length === 0) {
        return <p className="mt-4 text-sm text-slate-500">Nenhuma ocorrência encontrada.</p>;
    }

    return (
        <ul className="mt-4 divide-y divide-slate-100">
            {itens.map((item) => (
                <li key={item.id}>
                    <button
                        type="button"
                        onClick={() => onSelecionar(item.id)}
                        aria-pressed={selecionadaId === item.id}
                        className={`flex w-full flex-wrap items-center justify-between gap-2 px-2 py-3 text-left hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 ${
                            selecionadaId === item.id ? "rounded-xl bg-slate-100" : ""
                        }`}
                    >
                        <span className="min-w-0">
                            <span className="block text-sm font-semibold text-slate-800">
                                #{item.id} · {rotuloTipoOcorrencia[item.tipo]}
                            </span>
                            <span className="block truncate text-sm text-slate-600">
                                {item.descricao}
                            </span>
                            <span className="block text-xs text-slate-400">
                                {item.aluno ?? "Sem aluno"} · {item.autor} ·{" "}
                                {formatarData(item.criadoEm)}
                            </span>
                        </span>

                        <span
                            className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatusOcorrencia[item.status]}`}
                        >
                            {rotuloStatusOcorrencia[item.status]}
                        </span>
                    </button>
                </li>
            ))}
        </ul>
    );
}
