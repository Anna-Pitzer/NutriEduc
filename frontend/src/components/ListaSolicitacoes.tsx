import type {
    SolicitacaoResumo,
    StatusSolicitacao,
    TipoSolicitacao,
} from "../services/SolicitacaoApi";

export const rotuloTipoSolicitacao: Record<TipoSolicitacao, string> = {
    MudancaCardapio: "Mudança de cardápio",
    Ingrediente: "Ingrediente",
    FaltaIngrediente: "Falta de ingrediente",
    InclusaoAlergia: "Inclusão de alergia",
    AlteracaoRestricao: "Alteração de restrição",
    DadosCadastrais: "Dados cadastrais",
};

export const rotuloStatusSolicitacao: Record<StatusSolicitacao, string> = {
    Aberta: "Aberta",
    EmAnalise: "Em análise",
    Aprovada: "Aprovada",
    Recusada: "Recusada",
    Concluida: "Concluída",
    Cancelada: "Cancelada",
};

export const corStatusSolicitacao: Record<StatusSolicitacao, string> = {
    Aberta: "bg-sky-50 text-sky-700",
    EmAnalise: "bg-amber-50 text-amber-700",
    Aprovada: "bg-emerald-50 text-emerald-700",
    Recusada: "bg-red-50 text-red-700",
    Concluida: "bg-emerald-100 text-emerald-800",
    Cancelada: "bg-slate-100 text-slate-600",
};

/** Datas sempre em `dd/MM/yyyy` (Spec 035, RF32). */
export function formatarDataSolicitacao(valor: string): string {
    const data = new Date(valor);

    return Number.isNaN(data.getTime()) ? valor : data.toLocaleDateString("pt-BR");
}

/**
 * Lista reutilizável de solicitações (Spec 035, T11). Consumida pela área dos pais e pela
 * página compartilhada; nenhuma decisão de acesso acontece aqui.
 */
export function ListaSolicitacoes({
    itens,
    onSelecionar,
    carregando,
    selecionadaId = null,
}: {
    itens: SolicitacaoResumo[];
    onSelecionar: (id: number) => void;
    carregando: boolean;
    selecionadaId?: number | null;
}) {
    if (carregando) {
        return <p className="mt-4 text-sm text-slate-500">Carregando...</p>;
    }

    if (itens.length === 0) {
        return <p className="mt-4 text-sm text-slate-500">Nenhuma solicitação encontrada.</p>;
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
                                #{item.id} · {rotuloTipoSolicitacao[item.tipo]}
                            </span>
                            <span className="block truncate text-sm text-slate-600">
                                {item.descricao}
                            </span>
                            <span className="block text-xs text-slate-400">
                                {item.aluno ?? "Sem aluno"} · {item.solicitante} ·{" "}
                                {formatarDataSolicitacao(item.criadoEm)}
                            </span>
                        </span>

                        <span
                            className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatusSolicitacao[item.status]}`}
                        >
                            {rotuloStatusSolicitacao[item.status]}
                        </span>
                    </button>
                </li>
            ))}
        </ul>
    );
}
