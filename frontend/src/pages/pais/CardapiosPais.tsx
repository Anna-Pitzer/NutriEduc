import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/authContext";
import { buscarAlunos, type AlunoResposta } from "../../services/AlunoApi";
import { buscarCardapioDoAluno, type CardapioServido } from "../../services/CardapioApi";
import { ErroRequisicao, naoAutorizado } from "../../services/erroApi";
import { CardapioDoAluno } from "../../components/CardapioDoAluno";
import { SeletorDeFilho } from "../../components/SeletorDeFilho";

/** Data local no formato `yyyy-MM-dd` (Spec 035, RF2). */
function dataLocalHoje(): string {
    const agora = new Date();
    const mes = String(agora.getMonth() + 1).padStart(2, "0");
    const dia = String(agora.getDate()).padStart(2, "0");

    return `${agora.getFullYear()}-${mes}-${dia}`;
}

function formatarDataIso(valor: string): string {
    const [ano, mes, dia] = valor.split("-");

    return ano && mes && dia ? `${dia}/${mes}/${ano}` : valor;
}

/**
 * Cardápio servido por filho e data (Spec 035, RF2–RF6). A versão servida (geral ou
 * adaptada) vem de `GET /api/aluno/{id}/cardapio`; nenhum cardápio público é usado como
 * substituto.
 */
export function CardapiosPais() {
    const { logout } = useAuth();

    const [filhos, setFilhos] = useState<AlunoResposta[]>([]);
    const [alunoId, setAlunoId] = useState<number | null>(null);
    const [data, setData] = useState(dataLocalHoje());
    const [resultado, setResultado] = useState<CardapioServido | null>(null);
    const [refeicao, setRefeicao] = useState<string | "todas">("todas");

    const [carregandoFilhos, setCarregandoFilhos] = useState(true);
    const [consultando, setConsultando] = useState(false);
    const [erro, setErro] = useState("");
    const [erroFilhos, setErroFilhos] = useState("");
    const [erroData, setErroData] = useState("");
    const [tentativa, setTentativa] = useState(0);

    const tratarFalha = useCallback(
        async (falha: unknown, mensagemPadrao: string) => {
            if (naoAutorizado(falha)) {
                await logout();
                return;
            }

            if (falha instanceof ErroRequisicao) {
                if (falha.status === 403) {
                    setErro("Você não tem acesso a este aluno.");
                    return;
                }

                if (falha.status === 404) {
                    setErro("Aluno não encontrado.");
                    return;
                }

                if (falha.status === 400) {
                    setErroData(falha.message);
                    return;
                }
            }

            setErro(falha instanceof Error ? falha.message : mensagemPadrao);
        },
        [logout],
    );

    useEffect(() => {
        let ativo = true;

        async function carregarFilhos() {
            try {
                const lista = await buscarAlunos();
                const ordenada = [...lista].sort((a, b) =>
                    a.nome.localeCompare(b.nome, "pt-BR"),
                );

                if (ativo) {
                    setFilhos(ordenada);
                }
            } catch (falha) {
                if (ativo) {
                    if (naoAutorizado(falha)) {
                        await logout();
                    } else {
                        setErroFilhos(
                            falha instanceof Error
                                ? falha.message
                                : "Não foi possível carregar seus filhos.",
                        );
                    }
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
    }, [logout, tentativa]);

    async function consultar() {
        setErro("");
        setErroData("");

        if (!alunoId) {
            setErro("Escolha o filho para consultar o cardápio.");
            return;
        }

        if (!data) {
            setErroData("Escolha uma data válida.");
            return;
        }

        setConsultando(true);

        try {
            const resposta = await buscarCardapioDoAluno(alunoId, data);
            setResultado(resposta);
            setRefeicao("todas");
        } catch (falha) {
            setResultado(null);
            await tratarFalha(falha, "Não foi possível carregar o cardápio do aluno.");
        } finally {
            setConsultando(false);
        }
    }

    if (carregandoFilhos) {
        return <p className="text-sm text-slate-500">Carregando seus filhos...</p>;
    }

    if (erroFilhos) {
        return (
            <div className="rounded-2xl border border-red-200 bg-red-50 p-5">
                <p role="alert" className="text-sm text-red-700">{erroFilhos}</p>
                <button
                    type="button"
                    onClick={() => {
                        setErroFilhos("");
                        setCarregandoFilhos(true);
                        setTentativa((atual) => atual + 1);
                    }}
                    className="mt-3 rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white"
                >
                    Tentar novamente
                </button>
            </div>
        );
    }

    if (filhos.length === 0) {
        return (
            <div className="rounded-2xl border border-dashed border-slate-300 bg-white/70 p-6 text-center">
                <h1 className="text-lg font-semibold text-roxo">Nenhum aluno vinculado</h1>
                <p className="mt-2 text-sm text-slate-600">
                    Não há filho vinculado à sua conta para consultar o cardápio.
                </p>
                <Link
                    to="/pais"
                    className="mt-4 inline-block rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40"
                >
                    Voltar para o início
                </Link>
            </div>
        );
    }

    return (
        <section>
            <header className="mb-5">
                <h1 className="text-2xl font-bold text-roxo">Cardápios</h1>
                <p className="mt-1 text-sm text-slate-600">
                    Consulte o cardápio geral e a versão adaptada do filho na data escolhida.
                </p>
            </header>

            <div className="grid gap-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm md:grid-cols-[1fr_1fr_auto] md:items-end">
                <SeletorDeFilho filhos={filhos} value={alunoId} onChange={setAlunoId} id="cardapio-filho" />

                <div className="flex flex-col gap-1">
                    <label htmlFor="cardapio-data" className="text-sm font-medium text-slate-700">
                        Data
                    </label>
                    <input
                        id="cardapio-data"
                        type="date"
                        value={data}
                        onChange={(evento) => setData(evento.target.value)}
                        aria-invalid={Boolean(erroData)}
                        aria-describedby={erroData ? "erro-data" : undefined}
                        className="rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-roxo focus:ring-2 focus:ring-roxo/20"
                    />
                    {erroData && (
                        <p id="erro-data" role="alert" className="text-sm text-red-600">
                            {erroData}
                        </p>
                    )}
                </div>

                <button
                    type="button"
                    onClick={() => void consultar()}
                    disabled={consultando}
                    className="rounded-xl bg-roxo px-4 py-2 text-sm font-semibold text-white transition hover:opacity-90 focus:outline-none focus-visible:ring-2 focus-visible:ring-roxo/40 disabled:opacity-50"
                >
                    {consultando ? "Consultando..." : "Consultar cardápio"}
                </button>
            </div>

            {erro && (
                <p role="alert" className="mt-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            <div className="mt-5">
                {resultado ? (
                    <>
                        <p className="mb-2 text-sm text-slate-500">
                            Cardápios de {formatarDataIso(data)}.
                        </p>
                        <CardapioDoAluno
                            resultado={resultado}
                            refeicao={refeicao}
                            onRefeicao={setRefeicao}
                        />
                    </>
                ) : (
                    !erro && (
                        <p className="rounded-2xl border border-dashed border-slate-300 bg-white/60 p-5 text-sm text-slate-500">
                            Escolha o filho e a data e consulte o cardápio.
                        </p>
                    )
                )}
            </div>
        </section>
    );
}
