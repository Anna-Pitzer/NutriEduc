import Footer from "../components/Footer";
import Header from "../components/Header";
import Button from "../components/Button";
import { ChevronLeft, Apple, Candy, CircleHelp, Milk, Wheat } from "lucide-react";
import { useState } from "react";


export function CadastroAlunos() {

    const opcoes = [
        {
            nome: "Integral",
            valor: "Integral",
            icone: Apple
        },
        {
            nome: "Intolerância à Lactose",
            valor: "Intolerância à Lactose",
            icone: Milk,
        },
        {
            nome: "APVL",
            valor: "APVL",
            icone: Milk,
        },
        {
            nome: "Diabétes",
            valor: "Diabetes",
            icone: Candy,
        },
        {
            nome: "Intolerância Glúten",
            valor: "Intolerância ao Glúten",
            icone: Wheat,
        },
        {
            nome: "Outros",
            valor: "Outros",
            icone: CircleHelp,
        }
    ];

    interface FormularioAluno {
        nome: string;
        matricula: string;
        Nascimento: string;
        serie: string;
        escola: string;
        telefone: string;
        observacoes: string;
        restricoes: string[];
        alergiaSevera: string;
        contatoEmerNome: string;
        contatoEmerTelefone: string;
    }

    const estadoInicial: FormularioAluno = {
        nome: "",
        matricula: "",
        Nascimento: "",
        serie: "",
        escola: "",
        telefone: "",
        observacoes: "",
        restricoes: [],
        alergiaSevera: "",
        contatoEmerNome: "",
        contatoEmerTelefone: ""
    }

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
    const escolasPermitidas = [
        "Escola 1",
        "Escola 2",
        "Escola 3",
    ];

    //IDEAL PUXAR DO BACK DPS
    const restricoesPermitidas = [
        "Integral",
        "Intolerância à Lactose",
        "APVL",
        "Diabetes",
        "Intolerância ao Glúten",
        "Outros",
    ];

    const limparTexto = (valor: string, limite: number): string => {
        return valor.replace(/[<>]/g, "")
        .replace(/\s+/g, " ")
        .trimStart()
        .slice(0, limite);
    };

    const limparMatricula = (valor: string): string => {
        return valor.replace(/[^a-zA-Z0-9]/g, "").slice(0, 20)
    };

    const limparTelefone = (valor: string): string => {
        return valor.replace(/\D/g, "").slice(0, 11)};

    const limparNascimento = (valor: string): string => {
        return valor.replace(/\D/g, "").slice(0, 2)};

    const [form, setForm] = useState<FormularioAluno>(estadoInicial);
    const [mensagem, setMensagem] = useState("");
    const [tipoMensagem, setTipoMensagem] = useState<"erro" | "sucesso" | "">("");
    const [carregando, setCarregando] = useState(false);

    const mostrarErro = (texto: string) => {
        setMensagem(texto);
        setTipoMensagem("erro");
    };

    const atualizarCampo = (campo: keyof FormularioAluno, valor: string) => {
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
        if (!restricoesPermitidas.includes(restricao)) {
            return
        }

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
        const matricula = form.matricula.trim();
        const Nascimento = Number(form.Nascimento);
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

        if (!matricula) {
            mostrarErro("Informe a matricula do aluno.");
            return false;
        }

        if (matricula.length < 2) {
            mostrarErro("Informe uma matrícula válida.");
            return false;
        }

        if (!form.Nascimento || !Number.isInteger(Nascimento) || Nascimento < 1 || Nascimento > 18) {
            mostrarErro("Informe uma Nascimento válida (entre 1 e 18 anos).");
            return false;
        }

        if (!escolasPermitidas.includes(form.escola)) {
            mostrarErro("Selecione uma escola válida.");
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

        const restricoesInvalidas = form.restricoes.some((restricao) => !restricoesPermitidas.includes(restricao));

        if (restricoesInvalidas) {
            mostrarErro("Uma das restrições alimentares selecionadas é inválida");
            return false;
        }

        return true;
    };

    const prepararDados = () => {
        return {
            nome: limparTexto(form.nome, 100),
            matricula: limparMatricula(form.matricula),
            Nascimento: limparNascimento(form.Nascimento),
            serie: form.serie,
            escola: form.escola,
            telefone: limparTelefone(form.telefone),
            observacoes: limparTexto(form.observacoes, 350),
            restricoes: form.restricoes,
            alergiaSevera: form.alergiaSevera,
            contatoEmerNome: limparTexto(form.contatoEmerNome, 100),
            contatoEmerTelefone: limparTelefone(form.contatoEmerTelefone),
        };
    };

    const cadastrarALuno = async () => {
        if (carregando) {
            return;
        }

        setMensagem("");
        setTipoMensagem("")

        if (!validaFormulario()) {
            return;
        }

        const dados = prepararDados();
        setCarregando(true);

        try {
            // const resposta = await fetch(
            //    "http://localhost:3003/api/alunos",
            //     {
            //         method: "POST",
            //         headers: {
            //             "Content-Type":
            //             "application/json",
            //         },
            //         body: JSON.stringify(dados),
            //     }
            // );
             
            // if (!resposta.ok) {
            //     throw new Error(
            //         "Erro ao cadastrar aluno"
            //     );
            // }

            console.log("Dados preparados para Api", dados);

            setMensagem("Aluno validado e pronto para cadastro.")
            setTipoMensagem("sucesso");

            setForm(estadoInicial);
        } catch {
            setMensagem("Erro ao cadastrar aluno. Tente novamente mais tarde."
            );
            setTipoMensagem("erro");
        } finally {
            setCarregando(false)
        }
        
    };

    const limparCampos = () => {
        setForm(estadoInicial);
        setMensagem("");
        setTipoMensagem("");
    };

    const cancelar = () => window.history.back();

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">

            <Header />

            <button
                type="button"
                onClick={cancelar}
                className="group flex items-center justify-center cursor-pointer mx-10"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex flex-col mx-30 mb-10 gap-10">

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">

                    <h2 className="font-bold text-2xl">Informações do aluno</h2>

                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-4">
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nome Completo" value={form.nome} maxLength={100} autoComplete="name" onChange={(e) => atualizarCampo("nome", limparTexto(e.target.value, 100))}/>
                    </div>

                    <div className="flex gap-4">
                        <input type="number" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nascimento" value={form.Nascimento} minLength={1} maxLength={18} onChange={(e) => atualizarCampo("Nascimento", limparNascimento(e.target.value))}/>

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
                        <select className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-[border] focus:ring-4 focus:ring-roxo/10 cursor-pointer px-3" value={form.escola} onChange={(e) => atualizarCampo("escola", (e.target.value))}>
                            <option value="">
                                Escola
                            </option>
                            {escolasPermitidas.map(
                                (escola) => (
                                    <option
                                        key={escola}
                                        value={escola}
                                    >
                                        {escola}
                                    </option>
                                )
                            )}
                        </select>
                    </div>

                    <div>
                        <input type="tel" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Telefone" value={form.telefone} maxLength={11} autoComplete="tel" onChange={(e) => atualizarCampo("telefone", limparTelefone(e.target.value))}/>

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
                                            selecionado                                        }
                                        className={`group flex items-center gap-3 rounded-xl px-3 py-3 text-left text-sm font-semibold transition-all duration-200 ${
                                            selecionado
                                                ? "bg-roxo text-white shadow-sm"
                                                : "text-roxo hover:bg-[rgba(172,178,173,0.07)] hover:text-amarelo hover:shadow-sm"
                                        }`}
                                    >
                                        <span
                                            className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl shadow-sm transition-all duration-200 ${
                                                selecionado
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
                                    <input
                                        type="radio"
                                        name="alergia"
                                        value="sim"
                                        checked={
                                            form.alergiaSevera ===
                                            "sim"
                                        }
                                        onChange={(e) =>
                                            atualizarCampo(
                                                "alergiaSevera",
                                                e.target.value
                                            )
                                        }
                                        className="peer sr-only"
                                    />

                                    <div className="flex items-center justify-center rounded-2xl border-2 border-gray-200 bg-white px-8 py-4 font-semibold text-vermelho transition-all duration-200 hover:border-vermelho hover:bg-vermelho/5 peer-checked:border-vermelho peer-checked:bg-vermelho peer-checked:text-white">
                                        Sim
                                    </div>
                                </label>

                                {/* NÃO */}
                                <label className="cursor-pointer">
                                    <input
                                        type="radio"
                                        name="alergia"
                                        value="nao"
                                        checked={
                                            form.alergiaSevera ===
                                            "nao"
                                        }
                                        onChange={(e) =>
                                            atualizarCampo(
                                                "alergiaSevera",
                                                e.target.value
                                            )
                                        }
                                        className="peer sr-only"
                                    />
                                    
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
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Nome Completo / Parentesco" value={form.contatoEmerNome} maxLength={100}autoComplete="name" onChange={(e) => atualizarCampo( "contatoEmerNome",limparTexto(e.target.value, 100))}/>

                            <input type="tel" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-roxo focus:ring-4 focus:ring-roxo/10 my-2" placeholder="Telefone - 00 00000-0000" value={form.contatoEmerTelefone} maxLength={11} autoComplete="tel" onChange={(e) => atualizarCampo( "contatoEmerNome",limparTelefone(e.target.value))}/>
                        </div>
                    </div>
                    {mensagem && (
                        <div
                            role="alert"
                            className={`mt-4 rounded-2xl p-4 text-center font-semibold ${
                                tipoMensagem ===
                                "erro"
                                    ? "bg-red-50 text-red-700"
                                    : "bg-green-50 text-green-700"
                            }`}
                        >
                            {mensagem}
                        </div>
                    )}
                    <div className="flex justify-center gap-4 m-4">
                        <Button type="button" variant="normal" onClick={cadastrarALuno} disabled={carregando}>
                            {carregando ? "Cadastrando..." : "Cadastrar Aluno"}
                        </Button>
                        
                        <Button type="button" variant="limpar" onClick={limparCampos} disabled={carregando}>
                            Limpar campos
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