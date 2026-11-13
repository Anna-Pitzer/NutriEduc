import { useEffect } from "react";
import type { AlunoResposta } from "../services/AlunoApi";

/**
 * Seletor único de filho (Spec 035, RF26). Os alunos vêm de `GET /api/aluno`, que já
 * devolve apenas os filhos vinculados ao responsável autenticado. Com um único filho o
 * valor é pré-selecionado; com vários, a escolha é explícita.
 */
export function SeletorDeFilho({
    filhos,
    value,
    onChange,
    disabled = false,
    id = "seletor-filho",
}: {
    filhos: AlunoResposta[];
    value: number | null;
    onChange: (id: number) => void;
    disabled?: boolean;
    id?: string;
}) {
    useEffect(() => {
        if (value === null && filhos.length === 1) {
            onChange(filhos[0].id);
        }
    }, [filhos, value, onChange]);

    return (
        <div className="flex flex-col gap-1">
            <label htmlFor={id} className="text-sm font-medium text-slate-700">
                Filho
            </label>

            <select
                id={id}
                value={value ?? ""}
                disabled={disabled || filhos.length === 0}
                onChange={(evento) => {
                    if (evento.target.value === "") {
                        return;
                    }

                    onChange(Number(evento.target.value));
                }}
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-roxo focus:ring-2 focus:ring-roxo/20 disabled:opacity-50"
            >
                {filhos.length === 0 ? (
                    <option value="">Nenhum aluno vinculado</option>
                ) : (
                    <>
                        {value === null && <option value="">Selecione</option>}
                        {filhos.map((filho) => (
                            <option key={filho.id} value={filho.id}>
                                {filho.nome}
                            </option>
                        ))}
                    </>
                )}
            </select>

            {filhos.length === 0 && (
                <p className="text-xs text-slate-500">
                    Nenhum aluno está vinculado à sua conta.
                </p>
            )}
        </div>
    );
}
