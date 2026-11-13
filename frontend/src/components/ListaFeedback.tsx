import type { ReactNode } from "react";
import type { FeedbackResumo, StatusFeedback } from "../services/FeedbackApi";

export const rotuloStatusFeedback: Record<StatusFeedback, string> = {
    Aberto: "Aberto",
    Respondido: "Respondido",
    Encerrado: "Encerrado",
};

export const corStatusFeedback: Record<StatusFeedback, string> = {
    Aberto: "bg-sky-50 text-sky-700",
    Respondido: "bg-emerald-50 text-emerald-700",
    Encerrado: "bg-slate-100 text-slate-600",
};

/** Datas sempre em `dd/MM/yyyy` (Spec 035, RF32). */
export function formatarDataFeedback(valor: string): string {
    const data = new Date(valor);

    return Number.isNaN(data.getTime()) ? valor : data.toLocaleDateString("pt-BR");
}

/**
 * Lista reutilizável de feedbacks (Spec 035, T11). A área dos pais encerra o próprio
 * feedback; a página compartilhada injeta a ação de resposta via `renderAcoes`.
 */
export function ListaFeedback({
    itens,
    carregando = false,
    onEncerrar,
    renderAcoes,
    vazioTexto = "Nenhum feedback.",
}: {
    itens: FeedbackResumo[];
    carregando?: boolean;
    onEncerrar?: (id: number) => void;
    renderAcoes?: (item: FeedbackResumo) => ReactNode;
    vazioTexto?: string;
}) {
    if (carregando) {
        return <p className="mt-4 text-sm text-slate-500">Carregando...</p>;
    }

    if (itens.length === 0) {
        return <p className="mt-4 text-sm text-slate-500">{vazioTexto}</p>;
    }

    return (
        <ul className="mt-4 space-y-4">
            {itens.map((item) => (
                <li key={item.id} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                    <div className="flex flex-wrap items-start justify-between gap-2">
                        <div>
                            <h3 className="font-semibold text-roxo">{item.assunto}</h3>
                            <p className="text-xs text-slate-500">
                                Aluno: {item.aluno ?? "Sem aluno"}
                            </p>
                        </div>

                        <span
                            className={`rounded-full px-3 py-1 text-xs font-semibold ${corStatusFeedback[item.status]}`}
                        >
                            {rotuloStatusFeedback[item.status]}
                        </span>
                    </div>

                    <p className="mt-2 text-sm text-slate-700">{item.mensagem}</p>

                    <p className="mt-2 text-xs text-slate-500">
                        Por {item.autor} ({item.papelAutor}) · {formatarDataFeedback(item.criadoEm)}
                    </p>

                    {item.resposta && (
                        <div className="mt-3 rounded-xl bg-slate-50 px-3 py-2">
                            <p className="text-xs font-semibold text-slate-600">Resposta</p>
                            <p className="text-sm text-slate-800">{item.resposta}</p>
                            {item.respondidoPor && (
                                <p className="text-xs text-slate-500">Por {item.respondidoPor}</p>
                            )}
                        </div>
                    )}

                    {renderAcoes?.(item)}

                    {item.podeEncerrar && onEncerrar && (
                        <button
                            type="button"
                            onClick={() => onEncerrar(item.id)}
                            className="mt-3 rounded-xl border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                        >
                            Encerrar
                        </button>
                    )}
                </li>
            ))}
        </ul>
    );
}
