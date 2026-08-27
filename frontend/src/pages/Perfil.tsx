import Footer from "../components/Footer";
import Header from "../components/Header";
import { useState, useEffect } from "react";
import { ChevronLeft, Mail, Phone, User } from "lucide-react";


export function Perfil() {
    //Remover dps
    const usuarioId = 15;

    const [foto, setFoto] = useState<string | null>(null);
    const [form, setForm] = useState({
        nome: "",
        email: "",
        instituicao: "",
        categoria: "",
        telefone: ""
    });
    const [estatisticas, setEstatisticas] = useState({
        escolasGerenciadas: 0,
        alunosComRestricao: 0,
        refeicoesGerenciadas: 0
    });
    const [dadosOriginais, setDadosOriginais] = useState({
        nome: "",
        email: "",
        instituicao: "",
        categoria: "",
        telefone: ""
    });

    const [mostrarSenha, setMostrarSenha] = useState(false);
    const [senhaAtual, setSenhaAtual] = useState("");
    const [novaSenha, setNovaSenha] = useState("");
    const [confirmarSenha, setConfirmarSenha] = useState("");
    const [erro, setErro] = useState("");
    const [mensagem, setMensagem] = useState("")

    const sanitizarNome = (valor: string) => {
        return valor.replace(/[^A-Za-zÀ-ÿ\s'-]/g, "").replace(/\s+/g, " ").trimStart().slice(0, 100);
    };

    const sanitizarEmail = (valor: string) => {
        return valor.trim().toLocaleLowerCase().slice(0, 150);
    };

    const sanitizarInstituicao = (valor: string) => {
        return valor.replace(/\+/g, " ").trim().slice(0, 150);
    }

    const sanitizarTelefone = (valor: string) => {
        return valor.replace(/\D/g, "").slice(0, 11);
    }

    const formatarTelefone = (telefone: string) => {
        const numeros = telefone.replace(/\D/g, "");

        if (numeros.length <= 10) {
            return numeros.replace(
                /^(\d{2})(\d{4})(\d{0,4})$/,
                "($1) $2-$3"
            );
        }

        return numeros.replace(
            /^(\d{2})(\d{5})(\d{0,4})$/,
            "($1) $2-$3"
        );
    };

    const validarSenha = (senha: string) => {
        return senha.length >= 8;
    };

    const trocarFoto = (e: React.ChangeEvent<HTMLInputElement>) => {

        const arquivo = e.target.files?.[0];

        if (!arquivo) {
            return;
        }
        if (arquivo.size > 5 * 1024 * 1024) {
            setErro("A imagem deve ter no máximo 5MB.");
            return;
        }
        if (!arquivo.type.startsWith("image/")) {
            setErro("Selecione um arquivo de imagem válido.");
            return;
        }

        setErro("");

        const reader = new FileReader();

        reader.onloadend = () => {
            setFoto(reader.result as string);
        };

        reader.readAsDataURL(arquivo);
    };

    useEffect(() => {
        const carregarPerfil = async () => {
            try {
                const resposta = await fetch(
                    `http://localhost:3003/usuarios/${usuarioId}`
                );

                if (!resposta.ok) {
                    throw new Error("Erro ao buscar perfil");
                }

                const dados = await resposta.json();

                const dadosUsuario = {
                    nome: sanitizarNome(dados.nome || ""),
                    email: sanitizarEmail(dados.email || ""),
                    instituicao: sanitizarInstituicao(
                        dados.instituicao || ""
                    ),
                    categoria: dados.categoria || "",
                    telefone: sanitizarTelefone(
                        dados.telefone || ""
                    )
                };

                setForm(dadosUsuario);
                setDadosOriginais(dadosUsuario);

                if (dados.foto) {
                    setFoto(
                        `http://localhost:3003${dados.foto}`
                    );
                }

            } catch (error) {
                console.error(
                    "Erro ao carregar perfil:",
                    error
                );
                setErro(
                    "Não foi possível carregar o perfil."
                );
            }
        };
        carregarPerfil();
    }, []);

    useEffect(() => {
        const carregarEstatisticas = async () => {
            try {
                const resposta = await fetch(
                    `http://localhost:3003/usuarios/15/estatisticas`
                );
                if (!resposta.ok) {
                    throw new Error("Erro ao buscar estatísticas");
                }
                const dados = await resposta.json();
                setEstatisticas(dados);
            } catch (error) {
                console.error("Erro:", error);
            }
        };
        carregarEstatisticas();
    }, []);

    const salvarPerfil = async () => {

        setErro("");
        setMensagem("");

        const dadosSanitizados = {
            nome: sanitizarNome(form.nome),
            email: sanitizarEmail(form.email),
            instituicao: sanitizarInstituicao(form.instituicao),
            categoria: form.categoria,
            telefone: sanitizarTelefone(form.telefone)
        };

        if (dadosSanitizados.nome.length < 2) {
            setErro("Digite um nome válido.");
            return;
        }

        if (dadosSanitizados.email.length < 5 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(dadosSanitizados.email)) {
            setErro("Digite um e-mail válido.");
            return;
        }

        if (dadosSanitizados.instituicao.length < 2) {
            setErro("Digite uma instituição válida.");
            return;
        }

        const categoriasValidas = [
            "opcao1",
            "opcao2",
            "opcao3"
        ];

        if (!categoriasValidas.includes(dadosSanitizados.categoria)) {
            setErro("Selecione uma categoria válida.");
            return;
        }

        if (dadosSanitizados.telefone.length !== 10 &&dadosSanitizados.telefone.length !== 11) {
            setErro("Digite um telefone válido.");
            return;
        }

        try {
            const resp = await fetch(
                `http://localhost:3003/usuarios/${usuarioId}`,
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(
                        dadosSanitizados
                    )
                }
            );

            if (!resp.ok) {
                throw new Error("Erro ao atualizar perfil.");
            }

            const dados = await resp.json();

            console.log(dados);
            setForm(dadosSanitizados);

            setDadosOriginais(dadosSanitizados);

            setMensagem("Informações atualizadas com sucesso!");

        } catch (error) {

            console.error("Erro ao atualizar perfil:", error);

            setErro("Não foi possível atualizar o perfil.");
        }
    };

    const cancelarAlteracoes = () => {
        setForm(dadosOriginais);
        setErro("");
        setMensagem("");
    };

    const cancelarAlteracaoSenha = () => {
        setSenhaAtual("");
        setNovaSenha("");
        setConfirmarSenha("");
        setErro("");
        setMensagem("");
        setMostrarSenha(false);
    };

    const alterarSenha = async () => {
        setErro("");
        setMensagem("");

        if (!senhaAtual) {
            setErro("Digite sua senha atual.");
            return;
        }

        if (!novaSenha) {
            setErro("Digite a nova senha.");
            return;
        }

        if (!validarSenha(novaSenha)) {
            setErro("A nova senha deve possuir pelo menos 8 caracteres.");
            return;
        }

        if (!confirmarSenha) {
            setErro("Confirme sua nova senha.");
            return;
        }

        if (novaSenha !== confirmarSenha) {

            setErro("A confirmação da senha não corresponde.");
            return;
        }

        if (senhaAtual === novaSenha) {
            setErro("A nova senha deve ser diferente da senha atual.");
            return;
        }

        try {
            //Canto do endpoint
            
            const resposta = await fetch(`http://localhost:3003/usuarios/${usuarioId}/senha`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    senhaAtual,
                    novaSenha
                })
                  }
              );
             
            if (!resposta.ok) {
                throw new Error("Erro ao alterar senha.");
            }

            setSenhaAtual("");
            setNovaSenha("");
            setConfirmarSenha("");
            setMostrarSenha(false);

            setMensagem("Senha alterada com sucesso!");

        } catch (error) {

            console.error("Erro ao alterar senha:", error);

            setErro("Não foi possível alterar a senha.");
        }
    };

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">

            <Header />

            <button
                type="button"
                onClick={() => window.history.back()}
                className="group flex items-center justify-center cursor-pointer mx-10"
            >
                <ChevronLeft
                    size={50}
                    className="text-[#8F6BC8] transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="flex flex-col mx-30 mb-10">
               
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <div className="flex flex-col">
                        <div className="flex">
                            <label htmlFor="fotoPerfil" className="cursor-pointer ">
                                {foto ? (
                                    <img
                                        src={foto}
                                        alt="Foto de perfil"
                                        className="w-24 h-24 rounded-full object-cover border border-gray-300 hover:opacity-80 transition"
                                    />
                                ) : (
                                    <div className="w-24 h-24 rounded-full bg-[#F2F2F2] flex items-center justify-center border border-gray-300 hover:opacity-80 transition">
                                        <User
                                            size={42}
                                            className="text-[#8F6BC8]"
                                        />
                                    </div>
                                )}
                            </label>
                            <input id="fotoPerfil" type="file" accept="image/png,image/jpeg,image/webp" className="hidden" onChange={trocarFoto} />
                            <div className="">
                                <h2 className="px-5 font-bold text-2xl">{form.nome}</h2>
                                <p className="px-5">{form.categoria}</p>
                            </div>
                        </div>
                        <div className="my-5 w-full border border-gray-200 rounded-3xl" />
                        <div className="flex justify-around">
                            <div className="flex items-center justify-center gap-2">
                                <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                                    <Mail size={20} color="#FAF9F5" />
                                </div>
                                <p>Email: {form.email}</p>
                            </div>
                            <div className="flex items-center justify-center gap-2">
                                <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                                    <Phone size={20} color="#FAF9F5" />
                                </div>
                                <p>Telefone: {formatarTelefone(form.telefone)}</p>
                            </div>
                        </div>
                    </div>

                </div>
 
                <div className="flex justify-around mt-10">
                    <div className="bg-[F2F2F2] rounded-3xl p-10 shadow-[0_8px_30px_rgba(11,102,25,0.08)] flex flex-col items-center">
                        <h2 className="font-bold text-3xl text-[#91A644]">{estatisticas.escolasGerenciadas}</h2>
                        <p className="text-gray-500">Escolas gerenciadas</p>
                    </div>
                    <div className="bg-[F2F2F2] rounded-3xl p-10 shadow-[0_8px_30px_rgba(11,102,25,0.08)] flex flex-col items-center">
                        <h2 className="font-bold text-3xl text-[#D9631E]">{estatisticas.alunosComRestricao}</h2>
                        <p className="text-gray-500">Alunos com restrição</p>
                    </div>
                    <div className="bg-[F2F2F2] rounded-3xl p-10 shadow-[0_8px_30px_rgba(11,102,25,0.08)] flex flex-col items-center">
                        <h2 className="font-bold text-3xl text-[#8F6BC8]">{estatisticas.refeicoesGerenciadas}</h2>
                        <p className="text-gray-500">Refeições Gerenciadas</p>
                    </div>

                </div>

                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)] mt-10 flex flex-col">
                    <h2 className="font-bold text-2xl ">Editar Informações</h2>
                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    {erro && (

                        <div className="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                            {erro}
                        </div>
                    )}

                    {mensagem && (

                        <div className="mb-4 rounded-2xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700">
                            {mensagem}
                        </div>
                    )}

                    <div className="flex gap-2">
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2" placeholder="nome"
                            value={form.nome} onChange={(e) => setForm({ ...form, nome: sanitizarNome(e.target.value) })} maxLength={100}/>
                        <input type="email" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2"
                            placeholder="email"
                            value={form.email} onChange={(e) => setForm({ ...form, email: sanitizarEmail(e.target.value) })} maxLength={150}/>
                    </div>
                    <div>
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2" placeholder="Instituição"
                            value={form.instituicao} onChange={(e) => setForm({ ...form, instituicao: sanitizarInstituicao(e.target.value) })} maxLength={150}/>
                    </div>
                    <div className="flex gap-2">
                        {/*DPS TEM QUE PUXAR DO BACK AS OPCOES */}
                        <select
                            name="categoria"
                            id="categoria"
                            value={form.categoria} onChange={(e) => setForm({ ...form, categoria: e.target.value })}
                            className="w-full rounded-2xl border border-gray-200 bg-white  text-sm text-gray-700 outline-none transition-all duration-200 hover:border-gray-300 
                            hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 cursor-pointer px-3"
                        >
                            <option value="">Selecione uma opção</option>
                            <option value="opcao1">Opção 1</option>
                            <option value="opcao2">Opção 2</option>
                            <option value="opcao3">Opção 3</option>
                        </select>
                        <input type="tel" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2"
                            placeholder="(00) 00000-0000" value={form.telefone} onChange={(e) => setForm({ ...form, telefone: sanitizarTelefone(e.target.value) })} maxLength={11}/>
                    </div>
                    
                    <div className="flex flex-col justify-around mt-4">
                        <div className="flex flex-wrap items-center gap-3">
                            <button
                                type="button"
                                onClick={salvarPerfil}
                                className="rounded-2xl bg-[#D92567] px-5 py-3 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:bg-[#b31f55] hover:shadow-md active:scale-[0.98]"
                            >
                                Salvar Alterações
                            </button>

                            <button
                                type="button"
                                onClick={cancelarAlteracoes}
                                className="rounded-2xl border border-gray-200 bg-white px-5 py-3 text-sm font-medium text-gray-600 transition-all duration-200 hover:border-gray-300 hover:bg-gray-50 active:scale-[0.98]">
                                Cancelar
                            </button>

                            <button
                                type="button"
                                className="rounded-2xl border border-[#E6C229] bg-[#fffae5] px-5 py-3 text-sm font-medium text-[#E6C229] transition-all duration-200 hover:border-[#E6C229] hover:bg-[#fff3c4] active:scale-[0.98]" onClick={() => {setErro(""); setMensagem(""); setMostrarSenha(true);}}>
                                Alterar senha
                            </button>
                        </div>
                        {mostrarSenha && (
                            <div className="w-full mt-6 rounded-2xl border border-gray-200 bg-white p-5">

                                <h3 className="text-lg font-semibold text-gray-800">
                                    Alterar senha
                                </h3>

                                <div className="mt-4 flex flex-col gap-3">

                                    <input
                                        type="password"
                                        placeholder="Senha atual"
                                        className="w-full rounded-2xl border border-gray-200 px-4 py-3 outline-none focus:border-[#E6C229] focus:ring-4 focus:ring-[#E6C229]/10"
                                        onChange={(e) => setSenhaAtual(e.target.value)}
                                    />

                                    <input
                                        type="password"
                                        placeholder="Nova senha"
                                        className="w-full rounded-2xl border border-gray-200 px-4 py-3 outline-none focus:border-[#E6C229] focus:ring-4 focus:ring-[#E6C229]/10"
                                        onChange={(e) => setNovaSenha(e.target.value)}
                                    />

                                    <input
                                        type="password"
                                        placeholder="Confirme a nova senha"
                                        className="w-full rounded-2xl border border-gray-200 px-4 py-3 outline-none focus:border-[#E6C229] focus:ring-4 focus:ring-[#E6C229]/10"
                                        onChange={(e) => setConfirmarSenha(e.target.value)}
                                    />

                                    <div className="flex justify-end gap-3 mt-2">

                                        <button
                                            type="button"
                                            onClick={() => cancelarAlteracaoSenha()}
                                            className="rounded-2xl border border-gray-200 px-5 py-3 text-sm text-gray-600 hover:bg-gray-50"
                                        >
                                            Cancelar
                                        </button>

                                        <button
                                            type="button"
                                            className="rounded-2xl bg-[#E6C229] px-5 py-3 text-sm font-medium text-white hover:bg-[#c9a91f]" onClick={alterarSenha}
                                        >
                                            Confirmar alteração
                                        </button>

                                    </div>

                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <Footer />

        </div>
    );

}