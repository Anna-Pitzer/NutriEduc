import Footer from "../../../components/Footer";
import Header from "../../../components/Header";
import Button from "../../../components/Button";
import { ChevronLeft, Apple, Candy, CircleHelp, Milk, Wheat } from "lucide-react";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { atualizarAluno, buscarAlunoPorId, cadastrarAlunos, type AlunoCadastro, type AlunoResposta } from "../../../services/AlunoApi";
import { listarEscolas, type EscolaResposta } from "../../../services/EscolaApi";
import { naoAutorizado } from "../../../services/erroApi";
import { listarUnidades, type Unidade } from "../../../services/UnidadeApi";
import { useAuth } from "../../../context/authContext";

interface FormularioAluno {
    nome: string;
    nascimento: string;
    serie: string;
    escola: string;
    escolaId: number | null;
    telefone: string;
    observacoes: string;
    restricoes: string[];
    anafilaxia: boolean;
    contatoEmerNome: string;
    contatoEmerTelefone: string;
    unidadeId: number | null;
}

const estadoInicial: FormularioAluno = {
    nome: "",
    nascimento: "",
    serie: "",
    escola: "",
    escolaId: null,
    telefone: "",
    observacoes: "",
    restricoes: [],
    anafilaxia: false,
    contatoEmerNome: "",
    contatoEmerTelefone: "",
    unidadeId: null
};

/** Converte o aluno vindo da API no estado do formulário (cadastro e edição). */
function mapearAlunoParaFormulario(aluno: AlunoResposta): FormularioAluno {
    return {
        nome: aluno.nome,
        nascimento: aluno.nascimento,
        serie: aluno.serie,
        escola: aluno.escola,
        escolaId: aluno.escolaId ?? null,
        telefone: aluno.telefone,
        observacoes: aluno.observacao,
        restricoes: aluno.restricoes,
        anafilaxia: aluno.anafilaxia,
        contatoEmerNome: aluno.nomeResponsavel,
        contatoEmerTelefone: aluno.telefoneResponsavel,
        unidadeId: aluno.unidadeId ?? null,
    };
}


export function CadastroAlunos() {

    // Opções fixas de restrição e alergia exibidas ao aluno. Revisão da Spec 010
    // (2026-11-01): o catálogo é semeado pela migration e não é editável pela interface.
    const opcoes = [
        { nome: "Integral", valor: "Integral", icone: Apple },
        { nome: "Intolerância à Lactose", valor: "Intolerância à Lactose", icone: Milk },
        { nome: "APVL", valor: "APVL", icone: Milk },
        { nome: "Diabétes", valor: "Diabetes", icone: Candy },
        { nome: "Intolerância Glúten", valor: "Intolerância ao Glúten", icone: Wheat },
        { nome: "Outros", valor: "Outros", icone: CircleHelp },
    ];

    //IDEAL PUXAR DO BACK DPS
    const seriesPermitidas = [
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

    //IDEAL PUXAR DO BACK DPS
    const limparTexto = (valor: string, limite: number): string => {
        return valor.replace(/[<>]/g, "")
            .replace(/\s+/g, " ")
            .trimStart()
            .slice(0, limite);
    };

    const limparTelefone = (valor: string): string => {
        return valor.replace(/\D/g, "").slice(0, 11)
    };

    const { id } = useParams<{ id: string }>();
    const { logout } = useAuth();
    const idNumerico = Number(id);
    const idEdicao = Number.isInteger(idNumerico) && idNumerico > 0 ? idNumerico : null;
    const modoEdicao = idEdicao !== null;

    const [form, setForm] = useState<FormularioAluno>(estadoInicial);
    const [formOriginal, setFormOriginal] = useState<FormularioAluno | null>(null);
    const [unidades, setUnidades] = useState<Unidade[]>([]);
    const [escolas, setEscolas] = useState<EscolaResposta[]>([]);
    const [mensagem, setMensagem] = useState("");
    const [tipoMensagem, setTipoMensagem] = useState<"erro" | "sucesso" | "">("");
    const [carregando, setCarregando] = useState(false);
    const [carregandoAluno, setCarregandoAluno] = useState(modoEdicao);

    // A unidade é obrigatória: sem ela a restrição do aluno não entra na análise de
    // compatibilidade do cardápio, então o select não pode ficar vazio.
    useEffect(() => {
        listarUnidades()
            .then(setUnidades)
            .catch(() => setUnidades([]));
    }, []);

    useEffect(() => {
        // A escola é o vínculo institucional do aluno; a unidade é derivada dela.
        listarEscolas()
            .then(setEscolas)
            .catch(() => setEscolas([]));
    }, []);

    useEffect(() => {
        if (idEdicao === null) return;

        let ativo = true;

        buscarAlunoPorId(idEdicao)
            .then((aluno) => {
                if (!ativo) return;

                const carregado = mapearAlunoParaFormulario(aluno);
                setForm(carregado);
                setFormOriginal(carregado);
            })
            .catch((error) => {
                if (naoAutorizado(error)) {
                    void logout();
                    return;
                }

                if (ativo) {
                    setMensagem("Não foi possível carregar o aluno para edição.");
                    setTipoMensagem("erro");
                }
            })
            .finally(() => {
                if (ativo) setCarregandoAluno(false);
            });

        return () => {
            ativo = false;
        };
    }, [idEdicao, logout]);

    const mostrarErro = (texto: string) => {
        setMensagem(texto);
        setTipoMensagem("erro");
    };

    const atualizarCampo = <Campo extends keyof FormularioAluno,>(campo: Campo, valor: FormularioAluno[Campo]) => {
        setForm((prev) => ({
            ...prev, [campo]: valor,
        }))

        if (mensagem) {
            setMensagem("");
            setTipoMensagem("");
        }
    };

    const alterarRestricao = (
        restricao: string
    ) => {
        setForm((prev) => {
            const existe = prev.restricoes.includes(restricao);

            return {
                ...prev, restricoes: existe ? prev.restricoes.filter((item) => item !== restricao) : [...prev.restricoes, restricao],
            };
        });

        setMensagem("");
        setTipoMensagem("");
    };

    const validaFormulario = (): boolean => {
        const nome = form.nome.trim();
        const nascimento = new Date(`${form.nascimento}T00:00:00`);
        const hoje = new Date();
        let idade = hoje.getFullYear() - nascimento.getFullYear();
        if (
            hoje.getMonth() < nascimento.getMonth() ||
            (hoje.getMonth() === nascimento.getMonth() && hoje.getDate() < nascimento.getDate())
        ) {
            idade--;
        }
        const telefone = form.telefone.trim();
        const contatoNome = form.contatoEmerNome.trim();
        const contatoTelefone = form.contatoEmerTelefone.trim();

        if (!nome) {
            mostrarErro("Informe o nome completo do aluno.");
            return false;
        }

        if (nome.length < 3) {
            mostrarErro("O nome deve ter pelo menos 3 caracteres.");
            return false;
        }

        if (!form.nascimento || Number.isNaN(nascimento.getTime()) || idade < 1 || idade > 18) {
            mostrarErro("Informe uma data de nascimento válida (idade entre 1 e 18 anos).");
            return false;
        }

        if (form.unidadeId === null) {
            mostrarErro("Selecione a unidade escolar do aluno.");
            return false;
        }

        const escolaSelecionada = escolas.find(
            (escola) =>
                escola.id === form.escolaId &&
                escola.unidadeId === form.unidadeId &&
                escola.ativa,
        );

        if (!escolaSelecionada) {
            mostrarErro("Selecione uma escola ativa da unidade escolhida.");
            return false;
        }

        if (!telefone || telefone.length < 10) {
            mostrarErro("Informe um telefone válido");
            return false;
        }

        if (!contatoNome) {
            mostrarErro("Informe o nome do contato de emergência.");
            return false;
        }

        if (contatoNome.length < 3) {
            mostrarErro("Informe corretamente o nome do contato de emergência.");
            return false;
        }

        if (!contatoTelefone || contatoTelefone.length < 10) {
            mostrarErro("Informe um telefone de contato de emergência válido.");
            return false;
        }

        return true;
    };

    const prepararDados = (): AlunoCadastro => {
        const escolaSelecionada = escolas.find((escola) => escola.id === form.escolaId);

        return {
            nome: limparTexto(form.nome, 100),
            nascimento: form.nascimento,
            serie: form.serie,
            escola: escolaSelecionada?.nome ?? form.escola,
            escolaId: form.escolaId,
            telefone: limparTelefone(form.telefone),
            observacao: limparTexto(form.observacoes, 350),
            restricoes: form.restricoes,
            anafilaxia: form.anafilaxia,
            nomeResponsavel: limparTexto(form.contatoEmerNome, 100),
            telefoneResponsavel: limparTelefone(form.contatoEmerTelefone),
        };
    };

    const salvarAluno = async () => {
        if (carregando) return;

        setMensagem("");
        setTipoMensagem("");

        if (!validaFormulario()) return;

        const dados = prepararDados();

        setCarregando(true);

        try {
            if (idEdicao !== null) {
                const atualizado = await atualizarAluno(idEdicao, dados);

                const recarregado = mapearAlunoParaFormulario(atualizado);
                setForm(recarregado);
                setFormOriginal(recarregado);

                setMensagem("Aluno atualizado com sucesso!");
            } else {
                await cadastrarAlunos(dados);

                setMensagem("Aluno cadastrado com sucesso!");

                setForm(estadoInicial);
            }

            setTipoMensagem("sucesso");

        } catch (error) {
            if (naoAutorizado(error)) {
                void logout();
                return;
            }

            console.error("Erro ao salvar aluno:", error);

            const motivo = error instanceof Error && !(error instanceof TypeError) && error.message
                ? error.message
                : "Erro ao salvar aluno. Tente novamente mais tarde.";

            setMensagem(motivo);

            setTipoMensagem("erro");

        } finally {
            setCarregando(false);
        }
    };

    const limparCampos = () => {
        setForm(formOriginal ?? estadoInicial);
        setMensagem("");
        setTipoMensagem("");
    };

    const cancelar = () => window.history.back();

    if (carregandoAluno) {
        return (
            <div className="relative min-h-screen bg-[#FAF9F5]">
                <Header />
                <p className="mx-auto my-10 w-full max-w-7xl text-gray-500">Carregando aluno...</p>
                <Footer />
            </div>
        );
    }

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">

            <Header />

            <button
                type="button"
                onClick={cancelar}
                className="group mx-auto flex w-full max-w-7xl cursor-pointer items-center justify-start"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex w-full max-w-7xl flex-col mx-auto mb-10 gap-10">

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">

                    <h2 className="font-bold text-2xl">Informações do aluno</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-4">
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nome Completo" value={form.nome} maxLength={100} autoComplete="name" onChange={(e) => atualizarCampo("nome", limparTexto(e.target.value, 100))} />
                    </div>

                    <div className="flex gap-4">
                        <input type="date" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nascimento" value={form.nascimento} onChange={(e) => atualizarCampo("nascimento", e.target.value)} />

                        <select className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3" value={form.serie} onChange={(e) => atualizarCampo("serie", e.target.value)}>
                            <option value="">
                                Série
                            </option>
                            {seriesPermitidas.map((serie) => (
                                <option key={serie} value={serie}>
                                    {serie.replace(" ano", "°ano")}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="flex flex-col gap-1 my-2">
                        <select className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 py-3" value={form.unidadeId ?? ""} onChange={(e) => {
                                atualizarCampo("unidadeId", e.target.value === "" ? null : Number(e.target.value));
                                atualizarCampo("escolaId", null);
                                atualizarCampo("escola", "");
                            }}>
                            <option value="">
                                Unidade escolar
                            </option>
                            {unidades.map(
                                (unidade) => (
                                    <option
                                        key={unidade.id}
                                        value={unidade.id}
                                    >
                                        {unidade.nome}
                                    </option>
                                )
                            )}
                        </select>
                        <p className="pl-2 text-xs text-gray-500">
                            Obrigatória: define as escolas disponíveis para o aluno.
                        </p>
                    </div>

                    <div className="flex flex-col gap-1 my-2">
                        <select className="w-full rounded-2xl border border-gray-200 bg-white text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3 py-3" value={form.escolaId ?? ""} onChange={(e) => {
                                const escolaId = e.target.value === "" ? null : Number(e.target.value);
                                const escola = escolas.find((item) => item.id === escolaId);

                                atualizarCampo("escolaId", escolaId);
                                atualizarCampo("escola", escola?.nome ?? "");
                            }}>
                            <option value="">
                                Escola
                            </option>
                            {escolas
                                .filter((escola) =>
                                    escola.ativa &&
                                    (form.unidadeId === null || escola.unidadeId === form.unidadeId))
                                .map((escola) => (
                                    <option key={escola.id} value={escola.id}>
                                        {escola.nome}
                                    </option>
                                ))}
                        </select>
                        <p className="pl-2 text-xs text-gray-500">
                            A escola precisa existir e estar ativa na unidade selecionada.
                        </p>
                    </div>

                    <div>
                        <input type="tel" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Telefone" value={form.telefone} maxLength={11} autoComplete="tel" onChange={(e) => atualizarCampo("telefone", limparTelefone(e.target.value))} />

                        <textarea name="" id="" cols={30} rows={10} className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Observações importantes" value={form.observacoes} maxLength={350} onChange={(e) => atualizarCampo("observacoes", limparTexto(e.target.value, 350))} />
                        <p className="text-right text-xs text-gray-400">
                            {form.observacoes.length}/350
                        </p>
                    </div>

                </div>

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]  gap-y-4">

                    <div className="w-full flex flex-col items-center">

                        <div className="w-full">
                            <h2 className="text-xl font-bold text-gray-800 mb-4">
                                Restrições Alimentares
                            </h2>

                            <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                            <div className="grid grid-cols-2 gap-x-8 gap-y-3 w-full">
                                {opcoes.map((opcao) => {
                                    const Icon = opcao.icone;

                                    const selecionado =
                                        form.restricoes.includes(
                                            opcao.valor
                                        );

                                    return (
                                        <button
                                            type="button"
                                            key={opcao.valor}
                                            onClick={() =>
                                                alterarRestricao(
                                                    opcao.valor
                                                )
                                            }
                                            aria-pressed={
                                                selecionado}
                                            className={`group flex items-center gap-3 rounded-xl px-3 py-3 text-left text-sm font-semibold transition-all duration-200 ${selecionado
                                                ? "bg-roxo text-white shadow-sm"
                                                : "text-roxo hover:bg-[rgba(172,178,173,0.07)] hover:text-amarelo hover:shadow-sm"
                                                }`}
                                        >
                                            <span
                                                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl shadow-sm transition-all duration-200 ${selecionado
                                                    ? "bg-white text-roxo"
                                                    : "bg-white text-roxo group-hover:bg-roxo group-hover:text-white"
                                                    }`}
                                            >
                                                <Icon
                                                    size={20}
                                                    strokeWidth={2}
                                                />
                                            </span>

                                            <span>
                                                {opcao.nome}
                                            </span>
                                        </button>
                                    );
                                })}
                            </div>
                        </div>
                    </div>
                    <div className="group flex shrink-0 flex-col items-center justify-center rounded-3xl border border-vermelho bg-white p-4 text-vermelho shadow-sm transition-all duration-200 hover:bg-[rgba(196,15,15,0.12)] group-hover:bg-vermelho group-hover:text-white group-hover:shadow-md mt-4">
                        <h3 className="text-xl font-bold text-vermelho mb-4">
                            Alergia Severa/Risco de Anafilaxia
                        </h3>

                        <div className="flex gap-4">
                            <label className="cursor-pointer">
                                <input type="radio" name="alergia" value="sim"
                                    checked={
                                        form.anafilaxia ===
                                        true
                                    }
                                    onChange={() => atualizarCampo("anafilaxia", true)} className="peer sr-only" />

                                <div className="flex items-center justify-center rounded-2xl border-2 border-gray-200 bg-white px-8 py-4 font-semibold text-vermelho transition-all duration-200 hover:border-vermelho hover:bg-vermelho/5 peer-checked:border-vermelho peer-checked:bg-vermelho peer-checked:text-white">
                                    Sim
                                </div>
                            </label>

                            {/* NÃO */}
                            <label className="cursor-pointer">
                                <input type="radio" name="alergia" value="nao"
                                    checked={
                                        form.anafilaxia ===
                                        false
                                    }
                                    onChange={() => atualizarCampo("anafilaxia", false)} className="peer sr-only" />

                                <div className="flex items-center justify-center rounded-2xl border-2 border-gray-200 bg-white px-8 py-4 font-semibold text-vermelho transition-all duration-200 hover:border-vermelho hover:bg-vermelho/5 peer-checked:border-vermelho peer-checked:bg-vermelho peer-checked:text-white">
                                    Não
                                </div>
                            </label>

                        </div>
                    </div>
                </div>
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)] ">
                    <div className="flex flex-col">
                        <h2 className="font-bold text-2xl">Contato de Emergencia</h2>
                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />
                        <div className="flex gap-4">

                            <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nome Completo / Parentesco" value={form.contatoEmerNome} maxLength={100} autoComplete="name" onChange={(e) => atualizarCampo("contatoEmerNome", limparTexto(e.target.value, 100))} />

                            <input type="tel" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Telefone - 00 00000-0000" value={form.contatoEmerTelefone} maxLength={11} autoComplete="tel" onChange={(e) => atualizarCampo("contatoEmerTelefone", limparTelefone(e.target.value))} />
                        </div>
                    </div>
                    {mensagem && (
                        <div
                            role="alert"
                            className={`mt-4 rounded-2xl p-4 text-center font-semibold ${tipoMensagem ===
                                "erro"
                                ? "bg-red-50 text-red-700"
                                : "bg-green-50 text-green-700"
                                }`}
                        >
                            {mensagem}
                        </div>
                    )}
                    <div className="flex justify-center gap-4 m-4">
                        <Button type="button" variant="normal" onClick={salvarAluno} disabled={carregando}>
                            {carregando
                                ? "Salvando..."
                                : modoEdicao
                                    ? "Salvar alterações"
                                    : "Cadastrar Aluno"}
                        </Button>

                        <Button type="button" variant="limpar" onClick={limparCampos} disabled={carregando}>
                            {modoEdicao ? "Desfazer alterações" : "Limpar campos"}
                        </Button>

                        <Button type="button" variant="cancelar" onClick={cancelar} disabled={carregando}>
                            Cancelar
                        </Button>
                    </div>
                    <div className="border-verdeLodo border-2 rounded-3xl p-4 flex">
                        <div className="my-5 w-4 bg-verdeLodo rounded-3xl mr-4" />
                        <div>
                            <h2 className="font-bold text-2xl text-amarelo">Importante!</h2>
                            <p>Preencha corretamente todas as restrições alimentares do aluno. Essas informações serão utilizadas no planejamento diário do cardápio escolar e na segurança alimentar. Em caso de dúvida, consulte o responsável do aluno ou revise os documentos médicos.</p>
                        </div>
                    </div>
                </div>
            </div>

            <Footer />
        </div>
    );
}