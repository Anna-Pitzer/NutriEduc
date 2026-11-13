import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { naoAutorizado } from "../../services/erroApi";
import { criarOcorrencia } from "../../services/OcorrenciaApi";
import { FormularioOcorrencia } from "../../components/FormularioOcorrencia";

/**
 * Relato de ocorrência do responsável (Spec 035, RF11–RF14). O tipo é fixo em `Aluno`;
 * o servidor deriva autor, unidade, escola e snapshots.
 */
export function NovaOcorrenciaPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [alunoId, setAlunoId] = useState<number | null>(null);
    const [descricao, setDescricao] = useState("");

    const [carregandoFilhos, setCarregandoFilhos] = useState(true);
    const [enviando, setEnviando] = useState(false);
    const [erro, setErro] = useState("");
    const [erroFilho, setErroFilho] = useState("");
    const [erroDescricao, setErroDescricao] = useState("");
    const [sucesso, setSucesso] = useState("");

    const tratarFalha = useCallback(
        async (falha: unknown, padrao: string) => {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            setErro(falha instanceof Error ? falha.message : padrao);
        },
        [logout],
    );

    useEffect(() => {
        let ativo = true;

        async function carregarFilhos() {
            try {
                const lista = await buscarAlunos();

                if (ativo) {
                    setFilhos(
                        [...lista].sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR")),
                    );
                }
            } catch (falha) {
                if (ativo) {
                    await tratarFalha(falha, "Não foi possível carregar seus filhos.");
                }
            } finally {
                if (ativo) {
                    setCarregandoFilhos(false);
                }
            }
        }

        void carregarFilhos();

        return () => {
            ativo = false;
        };
    }, [tratarFalha]);

    async function enviar() {
        setErro("");
        setErroFilho("");
        setErroDescricao("");
        setSucesso("");

        const texto = descricao.trim();
        let invalido = false;

        if (!alunoId) {
            setErroFilho("Escolha o filho.");
            invalido = true;
        }

        if (texto.length < 5 || texto.length > 1000) {
            setErroDescricao("A descrição deve ter entre 5 e 1000 caracteres.");
            invalido = true;
        }

        if (invalido) {
            return;
        }

        setEnviando(true);

        try {
            await criarOcorrencia({
                tipo: "Aluno",
                alunoId: alunoId ?? undefined,
                descricao: texto,
            });

            setDescricao("");
            setSucesso("Ocorrência registrada.");
        } catch (falha) {
            await tratarFalha(falha, "Não foi possível registrar a ocorrência.");
        } finally {
            setEnviando(false);
        }
    }

    if (carregandoFilhos) {
        return <p className="text-sm text-slate-500">Carregando seus filhos...</p>;
    }

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Nova ocorrência</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Relate um problema relacionado ao seu filho. A ocorrência fica vinculada ao
                    seu nome e à escola do aluno.
                </p>
            </header>

            {filhos.length === 0 ? (
                <div className="rounded-2xl border border-dashed border-slate-300 bg-white/70 p-6 text-center">
                    <p className="text-sm text-slate-600">
                        Nenhum aluno vinculado à sua conta. As ações de aluno estão indisponíveis.
                    </p>
                    <Link
                        to="/pais"
                        className="mt-4 inline-block rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white"
                    >
                        Voltar para o início
                    </Link>
                </div>
            ) : (
                <>
                    {sucesso && (
                        <div
                            role="status"
                            className="mb-4 rounded-xl bg-emerald-50 px-4 py-3 text-sm text-emerald-700"
                        >
                            <p>{sucesso}</p>
                            <Link to="/pais/ocorrencias" className="mt-1 inline-block font-semibold underline">
                                Ver ocorrências
                            </Link>
                        </div>
                    )}

                    <FormularioOcorrencia
                        filhos={filhos}
                        alunoId={alunoId}
                        onAluno={setAlunoId}
                        descricao={descricao}
                        onDescricao={setDescricao}
                        enviando={enviando}
                        erro={erro}
                        erroFilho={erroFilho}
                        erroDescricao={erroDescricao}
                        onSubmit={() => void enviar()}
                    />

                    <Link
                        to="/pais/ocorrencias"
                        className="mt-4 inline-block text-sm font-semibold text-roxo underline"
                    >
                        Voltar para a lista
                    </Link>
                </>
            )}
        </section>
    );
}
