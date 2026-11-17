import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AlertTriangle, ChevronLeft, Plus, Save, Trash } from "lucide-react";
import Button from "../../../components/Button";
import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import {
    atualizarCardapio,
    criarCardapio,
    obterCardapio,
    obterContextoDeRestricoes,
    type CardapioEntrada,
    type ContextoDeRestricoes,
    type RefeicaoEntrada,
} from "../../../services/CardapioApi";
import { listarNutricionistas, type Nutricionista } from "../../../services/NutricionistaApi";
import { ErroRequisicao } from "../../../services/erroApi";
import { listarAlimentos, type Alimento } from "../../../services/AlimentoApi";
import { listarUnidades, type Unidade } from "../../../services/UnidadeApi";

interface ItemFormulario {
    alimentoId: number | "";
    gramas: string;
}

interface RefeicaoFormulario {
    nome: string;
    ordem: number;
    itens: ItemFormulario[];
}

const refeicaoVazia = (ordem: number): RefeicaoFormulario => ({
    nome: "",
    ordem,
    itens: [{ alimentoId: "", gramas: "100" }],
});

/**
 * Cadastro e edição de cardápio geral (Spec 012, RF13; Spec 029, RF9 a RF12).
 *
 * O formulário monta refeições e itens; os totais não são calculados aqui — quem calcula
 * é o backend (RNF3), e o detalhe exibe o resultado. A montagem manual exige a
 * nutricionista com CRN e mostra os alunos e restrições por escola; alimento
 * incompatível com substituto seguro exige confirmação explícita.
 */
export function FormularioCardapio() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const editando = Boolean(id);

    const [nome, setNome] = useState("");
    const [unidadeId, setUnidadeId] = useState<number | "">("");
    const [inicio, setInicio] = useState("");
    const [fim, setFim] = useState("");
    const [nutricionistaId, setNutricionistaId] = useState<number | "">("");
    const [refeicoes, setRefeicoes] = useState<RefeicaoFormulario[]>([refeicaoVazia(1)]);
    const [alimentos, setAlimentos] = useState<Alimento[]>([]);
    const [unidades, setUnidades] = useState<Unidade[]>([]);
    const [nutricionistas, setNutricionistas] = useState<Nutricionista[]>([]);
    const [contexto, setContexto] = useState<ContextoDeRestricoes | null>(null);
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState<string | null>(null);
    const [avisoIncompatibilidade, setAvisoIncompatibilidade] = useState<string | null>(null);
    const [entradaPendente, setEntradaPendente] = useState<CardapioEntrada | null>(null);

    useEffect(() => {
        async function carregar() {
            try {
                const [listaUnidades, paginaAlimentos, listaNutricionistas] = await Promise.all([
                    listarUnidades(),
                    listarAlimentos({ tamanho: 100 }),
                    listarNutricionistas(),
                ]);

                setUnidades(listaUnidades);
                setAlimentos(paginaAlimentos.itens);
                setNutricionistas(listaNutricionistas);

                if (id) {
                    const cardapio = await obterCardapio(Number(id));

                    setNome(cardapio.nome);
                    setUnidadeId(cardapio.unidadeId);
                    setInicio(cardapio.periodo.inicio);
                    setFim(cardapio.periodo.fim);
                    setNutricionistaId(cardapio.nutricionista?.id ?? "");
                    setRefeicoes(
                        cardapio.refeicoes.map((refeicao) => ({
                            nome: refeicao.nome,
                            ordem: refeicao.ordem,
                            itens: refeicao.itens.map((item) => ({
                                alimentoId: item.alimentoId,
                                gramas: String(item.gramas),
                            })),
                        })),
                    );
                } else if (listaUnidades.length > 0) {
                    setUnidadeId(listaUnidades[0].id);
                }
            } catch (falha) {
                setErro(falha instanceof Error ? falha.message : "Erro ao carregar o formulário");
            } finally {
                setCarregando(false);
            }
        }

        carregar();
    }, [id]);

    useEffect(() => {
        async function carregarContexto() {
            if (unidadeId === "") {
                setContexto(null);
                return;
            }

            try {
                setContexto(await obterContextoDeRestricoes(Number(unidadeId)));
            } catch {
                // O contexto é informativo: falha ao carregar não impede montar o cardápio.
                setContexto(null);
            }
        }

        carregarContexto();
    }, [unidadeId]);

    function atualizarRefeicao(indice: number, campo: "nome" | "ordem", valor: string) {
        setRefeicoes((atuais) =>
            atuais.map((refeicao, posicao) =>
                posicao === indice
                    ? {
                          ...refeicao,
                          [campo]: campo === "ordem" ? Number(valor) : valor,
                      }
                    : refeicao,
            ),
        );
    }

    function atualizarItem(
        indiceRefeicao: number,
        indiceItem: number,
        campo: "alimentoId" | "gramas",
        valor: string,
    ) {
        setRefeicoes((atuais) =>
            atuais.map((refeicao, posicao) => {
                if (posicao !== indiceRefeicao) return refeicao;

                return {
                    ...refeicao,
                    itens: refeicao.itens.map((item, posicaoItem) =>
                        posicaoItem === indiceItem
                            ? {
                                  ...item,
                                  [campo]:
                                      campo === "alimentoId"
                                          ? valor === ""
                                              ? ""
                                              : Number(valor)
                                          : valor,
                              }
                            : item,
                    ),
                };
            }),
        );
    }

    function adicionarRefeicao() {
        setRefeicoes((atuais) => [...atuais, refeicaoVazia(atuais.length + 1)]);
    }

    function removerRefeicao(indice: number) {
        setRefeicoes((atuais) => atuais.filter((_, posicao) => posicao !== indice));
    }

    function adicionarItem(indiceRefeicao: number) {
        setRefeicoes((atuais) =>
            atuais.map((refeicao, posicao) =>
                posicao === indiceRefeicao
                    ? { ...refeicao, itens: [...refeicao.itens, { alimentoId: "", gramas: "100" }] }
                    : refeicao,
            ),
        );
    }

    function removerItem(indiceRefeicao: number, indiceItem: number) {
        setRefeicoes((atuais) =>
            atuais.map((refeicao, posicao) =>
                posicao === indiceRefeicao
                    ? {
                          ...refeicao,
                          itens: refeicao.itens.filter((_, posicaoItem) => posicaoItem !== indiceItem),
                      }
                    : refeicao,
            ),
        );
    }

    function limparAviso() {
        setAvisoIncompatibilidade(null);
        setEntradaPendente(null);
    }

    async function enviar(confirmarIncompatibilidades = false) {
        setErro(null);
        limparAviso();

        if (!nome.trim() || unidadeId === "" || !inicio || !fim) {
            setErro("Informe nome, unidade e período do cardápio.");
            return;
        }

        if (!editando && nutricionistaId === "") {
            setErro("Informe a nutricionista responsável pelo cardápio.");
            return;
        }

        if (refeicoes.length === 0) {
            setErro("O cardápio precisa de ao menos uma refeição.");
            return;
        }

        const refeicoesEntrada: RefeicaoEntrada[] = [];

        for (const refeicao of refeicoes) {
            if (!refeicao.nome.trim()) {
                setErro("Toda refeição precisa de um nome.");
                return;
            }

            const itens = refeicao.itens.filter(
                (item) => item.alimentoId !== "" && item.gramas.trim() !== "",
            );

            if (itens.length === 0) {
                setErro(`A refeição "${refeicao.nome}" precisa de ao menos um alimento.`);
                return;
            }

            refeicoesEntrada.push({
                nome: refeicao.nome.trim(),
                ordem: refeicao.ordem,
                itens: itens.map((item) => ({
                    alimentoId: Number(item.alimentoId),
                    gramas: Number(item.gramas.replace(",", ".")),
                })),
            });
        }

        const entrada: CardapioEntrada = {
            nome: nome.trim(),
            unidadeId: Number(unidadeId),
            periodo: { inicio, fim },
            refeicoes: refeicoesEntrada,
            nutricionistaId: nutricionistaId === "" ? null : Number(nutricionistaId),
            confirmarIncompatibilidades,
        };

        setSalvando(true);

        try {
            if (editando) {
                await atualizarCardapio(Number(id), entrada);
            } else {
                await criarCardapio(entrada);
            }

            navigate("/cardapios");
        } catch (falha) {
            const mensagem = falha instanceof Error ? falha.message : "Erro ao salvar o cardápio";

            if (
                falha instanceof ErroRequisicao &&
                falha.status === 400 &&
                mensagem.includes("confirmarIncompatibilidades")
            ) {
                setAvisoIncompatibilidade(mensagem);
                setEntradaPendente(entrada);
            } else {
                setErro(mensagem);
            }
        } finally {
            setSalvando(false);
        }
    }

    async function confirmarIncompatibilidade() {
        if (!entradaPendente) return;

        setAvisoIncompatibilidade(null);
        setEntradaPendente(null);
        await enviar(true);
    }

    const totalAlunos = contexto?.escolas.reduce((total, escola) => total + escola.alunos.length, 0) ?? 0;
    const totalAlimentosSelecionados = refeicoes.reduce(
        (total, refeicao) =>
            total + refeicao.itens.filter((item) => item.alimentoId !== "").length,
        0,
    );

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate("/cardapios")}
                aria-label="Voltar aos cardápios"
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-10">
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="font-bold text-2xl">
                        {editando ? "Editar cardápio" : "Informações do cardápio"}
                    </h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    {carregando ? (
                        <p className="py-4 text-gray-500">Carregando...</p>
                    ) : (
                        <>
                            <div className="flex gap-4">
                                <input
                                    type="text"
                                    value={nome}
                                    onChange={(evento) => setNome(evento.target.value)}
                                    placeholder="Nome do cardápio"
                                    aria-label="Nome do cardápio"
                                    maxLength={120}
                                    className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                />

                                <select
                                    value={unidadeId}
                                    onChange={(evento) => setUnidadeId(Number(evento.target.value))}
                                    aria-label="Unidade"
                                    disabled={editando}
                                    className="w-full rounded-2xl border border-gray-200 bg-white text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 my-2 disabled:cursor-not-allowed disabled:bg-gray-100"
                                >
                                    <option value="">Unidade</option>
                                    {unidades.map((unidade) => (
                                        <option key={unidade.id} value={unidade.id}>
                                            {unidade.nome}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="flex gap-4">
                                <input
                                    type="date"
                                    value={inicio}
                                    onChange={(evento) => setInicio(evento.target.value)}
                                    aria-label="Início do período"
                                    className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                />

                                <input
                                    type="date"
                                    value={fim}
                                    onChange={(evento) => setFim(evento.target.value)}
                                    aria-label="Fim do período"
                                    className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                />
                            </div>

                            {!editando && (
                                <div className="flex flex-col">
                                    <label htmlFor="inNutricionista" className="text-sm text-gray-600">
                                        Nutricionista responsável (nome e CRN)
                                    </label>
                                    <select
                                        id="inNutricionista"
                                        value={nutricionistaId}
                                        onChange={(evento) =>
                                            setNutricionistaId(
                                                evento.target.value === ""
                                                    ? ""
                                                    : Number(evento.target.value),
                                            )
                                        }
                                        className="w-full rounded-2xl border border-gray-200 bg-white text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 py-3 my-2"
                                    >
                                        <option value="">Selecione a nutricionista</option>
                                        {nutricionistas.map((nutricionista) => (
                                            <option
                                                key={nutricionista.id}
                                                value={nutricionista.id}
                                                disabled={!nutricionista.crn}
                                            >
                                                {nutricionista.nome ?? `Nutricionista #${nutricionista.id}`}
                                                {nutricionista.crn
                                                    ? ` · CRN ${nutricionista.crn}`
                                                    : " · sem CRN (não pode assinar)"}
                                            </option>
                                        ))}
                                    </select>
                                    {nutricionistas.length === 0 && (
                                        <p className="text-sm text-amber-700">
                                            Nenhuma nutricionista ativa na unidade. Cadastre o CRN antes
                                            de montar o cardápio.
                                        </p>
                                    )}
                                </div>
                            )}
                        </>
                    )}
                </div>

                {!carregando && (
                    <>
                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            <div className="flex flex-wrap items-center justify-between gap-2">
                                <h2 className="font-bold text-2xl">Alunos e restrições por escola</h2>
                                <p className="text-sm text-gray-500">
                                    {totalAlunos} aluno(s) ativo(s) · {totalAlimentosSelecionados}{" "}
                                    alimento(s) selecionado(s)
                                </p>
                            </div>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            {contexto === null && (
                                <p className="text-sm text-gray-500">
                                    Selecione a unidade para ver os alunos e as restrições.
                                </p>
                            )}

                            {contexto?.escolas.length === 0 && (
                                <p className="text-sm text-gray-500">
                                    A unidade não possui escolas cadastradas.
                                </p>
                            )}

                            <div className="grid gap-4 md:grid-cols-2">
                                {contexto?.escolas.map((escola) => (
                                    <div
                                        key={escola.escolaId}
                                        className="rounded-2xl border border-[#fce9d3] bg-white p-4"
                                    >
                                        <div className="flex flex-wrap items-center justify-between gap-2">
                                            <h3 className="font-bold text-gray-800">
                                                {escola.escolaNome}
                                            </h3>
                                            {escola.semRestricoes && (
                                                <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-800">
                                                    sem restrições
                                                </span>
                                            )}
                                        </div>

                                        {escola.alunos.length === 0 && (
                                            <p className="mt-2 text-sm text-gray-500">
                                                Nenhum aluno ativo nesta escola.
                                            </p>
                                        )}

                                        <ul className="mt-3 flex flex-col gap-2">
                                            {escola.alunos.map((aluno) => (
                                                <li key={aluno.alunoId} className="text-sm">
                                                    <p className="font-medium text-gray-800">
                                                        {aluno.nome}
                                                    </p>
                                                    {aluno.restricoes.length === 0 ? (
                                                        <p className="text-gray-500">Sem restrições</p>
                                                    ) : (
                                                        <div className="mt-1 flex flex-wrap gap-1">
                                                            {aluno.restricoes.map((restricao) => (
                                                                <span
                                                                    key={restricao.id}
                                                                    className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800"
                                                                    title={restricao.categoria}
                                                                >
                                                                    {restricao.nome}
                                                                </span>
                                                            ))}
                                                        </div>
                                                    )}
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            <div className="flex items-center justify-between">
                                <h2 className="font-bold text-2xl">Refeições</h2>

                                <button
                                    type="button"
                                    onClick={adicionarRefeicao}
                                    className="flex items-center gap-2 rounded-2xl bg-primaria px-4 py-2 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55]"
                                >
                                    <Plus size={16} />
                                    Adicionar refeição
                                </button>
                            </div>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            {refeicoes.length === 0 && (
                                <p className="text-sm text-gray-500">
                                    Nenhuma refeição no cardápio. Um cardápio vazio tem total zero
                                    e é aceito, mas não serve para publicar.
                                </p>
                            )}

                            <div className="flex flex-col gap-6">
                                {refeicoes.map((refeicao, indice) => (
                                    <div
                                        key={indice}
                                        className="rounded-2xl border border-gray-200 bg-white p-4"
                                    >
                                        <div className="flex flex-wrap items-end gap-4">
                                            <div className="flex flex-1 flex-col">
                                                <label
                                                    htmlFor={`nome-refeicao-${indice}`}
                                                    className="text-sm text-gray-600"
                                                >
                                                    Refeição
                                                </label>
                                                <input
                                                    id={`nome-refeicao-${indice}`}
                                                    type="text"
                                                    value={refeicao.nome}
                                                    onChange={(evento) =>
                                                        atualizarRefeicao(
                                                            indice,
                                                            "nome",
                                                            evento.target.value,
                                                        )
                                                    }
                                                    placeholder="Ex.: Almoço"
                                                    maxLength={60}
                                                    className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                                />
                                            </div>

                                            <div className="flex w-28 flex-col">
                                                <label
                                                    htmlFor={`ordem-refeicao-${indice}`}
                                                    className="text-sm text-gray-600"
                                                >
                                                    Ordem
                                                </label>
                                                <input
                                                    id={`ordem-refeicao-${indice}`}
                                                    type="number"
                                                    min="1"
                                                    value={refeicao.ordem}
                                                    onChange={(evento) =>
                                                        atualizarRefeicao(
                                                            indice,
                                                            "ordem",
                                                            evento.target.value,
                                                        )
                                                    }
                                                    className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                                />
                                            </div>

                                            <button
                                                type="button"
                                                onClick={() => removerRefeicao(indice)}
                                                aria-label={`Remover refeição ${refeicao.nome}`}
                                                className="mb-2 text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full"
                                            >
                                                <Trash />
                                            </button>
                                        </div>

                                        <div className="mt-2 flex flex-col gap-3">
                                            {refeicao.itens.map((item, indiceItem) => (
                                                <div
                                                    key={indiceItem}
                                                    className="flex flex-wrap items-end gap-4"
                                                >
                                                    <div className="flex flex-[2] flex-col">
                                                        <label
                                                            htmlFor={`alimento-${indice}-${indiceItem}`}
                                                            className="text-sm text-gray-600"
                                                        >
                                                            Alimento
                                                        </label>
                                                        <select
                                                            id={`alimento-${indice}-${indiceItem}`}
                                                            value={item.alimentoId}
                                                            onChange={(evento) =>
                                                                atualizarItem(
                                                                    indice,
                                                                    indiceItem,
                                                                    "alimentoId",
                                                                    evento.target.value,
                                                                )
                                                            }
                                                            className="w-full rounded-2xl border border-gray-200 bg-white text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 py-3 my-2"
                                                        >
                                                            <option value="">Selecione o alimento</option>
                                                            {alimentos.map((alimento) => (
                                                                <option
                                                                    key={alimento.id}
                                                                    value={alimento.id}
                                                                >
                                                                    {alimento.nome}
                                                                    {alimento.ativo ? "" : " (desativado)"}
                                                                </option>
                                                            ))}
                                                        </select>
                                                    </div>

                                                    <div className="flex w-32 flex-col">
                                                        <label
                                                            htmlFor={`gramas-${indice}-${indiceItem}`}
                                                            className="text-sm text-gray-600"
                                                        >
                                                            Gramas
                                                        </label>
                                                        <input
                                                            id={`gramas-${indice}-${indiceItem}`}
                                                            type="number"
                                                            min="1"
                                                            max="5000"
                                                            step="0.01"
                                                            value={item.gramas}
                                                            onChange={(evento) =>
                                                                atualizarItem(
                                                                    indice,
                                                                    indiceItem,
                                                                    "gramas",
                                                                    evento.target.value,
                                                                )
                                                            }
                                                            className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2"
                                                        />
                                                    </div>

                                                    <button
                                                        type="button"
                                                        onClick={() => removerItem(indice, indiceItem)}
                                                        aria-label="Remover alimento da refeição"
                                                        className="mb-2 text-vermelho p-2 hover:cursor-pointer hover:bg-red-100 rounded-full"
                                                    >
                                                        <Trash size={18} />
                                                    </button>
                                                </div>
                                            ))}

                                            <button
                                                type="button"
                                                onClick={() => adicionarItem(indice)}
                                                className="flex w-fit items-center gap-2 rounded-2xl border border-roxo px-4 py-2 text-sm font-medium text-roxo transition-all duration-200 hover:bg-roxo hover:text-white"
                                            >
                                                <Plus size={16} />
                                                Adicionar alimento
                                            </button>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                            {erro && (
                                <div
                                    role="alert"
                                    className="mb-4 rounded-2xl bg-red-50 p-4 text-center font-semibold text-red-700"
                                >
                                    {erro}
                                </div>
                            )}

                            {avisoIncompatibilidade && (
                                <div
                                    role="alert"
                                    className="mb-4 rounded-2xl border border-amber-300 bg-amber-50 p-4 text-amber-900"
                                >
                                    <p className="flex items-center gap-2 font-semibold">
                                        <AlertTriangle size={18} />
                                        Incompatibilidade com restrição de aluno
                                    </p>
                                    <p className="mt-2 text-sm">{avisoIncompatibilidade}</p>
                                </div>
                            )}

                            <div className="flex flex-wrap justify-center gap-4 m-4">
                                {entradaPendente ? (
                                    <Button
                                        type="button"
                                        variant="normal"
                                        onClick={confirmarIncompatibilidade}
                                        disabled={salvando}
                                    >
                                        <span className="flex items-center gap-2">
                                            <AlertTriangle size={18} />
                                            {salvando
                                                ? "Salvando..."
                                                : "Confirmar e salvar mesmo assim"}
                                        </span>
                                    </Button>
                                ) : (
                                    <Button
                                        type="button"
                                        variant="normal"
                                        onClick={() => enviar()}
                                        disabled={salvando}
                                    >
                                        <span className="flex items-center gap-2">
                                            <Save size={18} />
                                            {salvando ? "Salvando..." : "Salvar cardápio"}
                                        </span>
                                    </Button>
                                )}

                                <Button
                                    type="button"
                                    variant="cancelar"
                                    onClick={() => navigate("/cardapios")}
                                    disabled={salvando}
                                >
                                    Cancelar
                                </Button>
                            </div>
                        </div>
                    </>
                )}
            </div>

            <Footer />
        </div>
    );
}
