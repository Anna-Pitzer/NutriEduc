import Footer from "../components/Footer";
import Header from "../components/Header";
import { useState, useEffect } from "react";
import { ChevronLeft, Mail, Phone, User } from "lucide-react";


export default function Perfil() {

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

    const trocarFoto = (e: React.ChangeEvent<HTMLInputElement>) => {
        const arquivo = e.target.files?.[0];
        if (arquivo) {
            const reader = new FileReader();
            reader.onloadend = () => {
                setFoto(reader.result as string);
            };
            reader.readAsDataURL(arquivo);
        }
    };

    const usuarioId = 15;

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
                    nome: dados.nome || "",
                    email: dados.email || "",
                    instituicao: dados.instituicao || "",
                    categoria: dados.categoria || "",
                    telefone: dados.telefone || ""
                };

                setForm(dadosUsuario);
                setDadosOriginais(dadosUsuario);

                if (dados.foto) {
                    setFoto(`http://localhost:3003${dados.foto}`);
                }

            } catch (error) {
                console.error("Erro ao carregar perfil:", error);
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
        try {
            const resp = await fetch(
                //COLOCAR A URL REAL 
                `http://localhost:3003/usuarios/15`,
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(form)
                }
            );

            const dados = await resp.json();
            //TEM QUE TIRAR AQ DPS 
            console.log(dados);
        } catch (error) {
            console.error("Erro ao atualizar perfil:", error);
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
                {/*Perfil em si*/}
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
                            <input id="fotoPerfil" type="file" accept="image/*" className="hidden" onChange={trocarFoto} />
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
                                <p>Telefone: {form.telefone}</p>
                            </div>
                        </div>
                    </div>

                </div>
                {/*Estatisticas*/}
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

                {/*Infos*/}
                <div className="bg-[F2F2F2] rounded-3xl p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)] mt-10 flex flex-col">
                    <h2 className="font-bold text-2xl ">Editar Informações</h2>
                    <div className="my-5 w-full border border-gray-200 rounded-3xl" />

                    <div className="flex gap-2">
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2" placeholder="nome"
                            value={form.nome} onChange={(e) => setForm({ ...form, nome: e.target.value })} />
                        <input type="email" name="" id="" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2"
                            placeholder="email"
                            value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                    </div>
                    <div>
                        <input type="text" className="w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all duration-200 placeholder:text-gray-400 hover:border-gray-300 
                        hover:bg-gray-200 focus:border-green-500 focus:ring-4 focus:ring-green-500/10 my-2" placeholder="Instituição"
                            value={form.instituicao} onChange={(e) => setForm({ ...form, instituicao: e.target.value })} />
                    </div>
                    <div className="flex gap-2">
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
                            placeholder="(00) 00000-0000" value={form.telefone} onChange={(e) => setForm({ ...form, telefone: e.target.value })} />
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
                                className="rounded-2xl border border-gray-200 bg-white px-5 py-3 text-sm font-medium text-gray-600 transition-all duration-200 hover:border-gray-300 hover:bg-gray-50 active:scale-[0.98]" onClick={() => setForm(dadosOriginais)}>
                                Cancelar
                            </button>

                            <button
                                type="button"
                                className="rounded-2xl border border-[#E6C229] bg-[#fffae5] px-5 py-3 text-sm font-medium text-[#E6C229] transition-all duration-200 hover:border-[#E6C229] hover:bg-[#fff3c4] active:scale-[0.98]" onClick={() => setMostrarSenha(true)}>
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
                                    />

                                    <input
                                        type="password"
                                        placeholder="Nova senha"
                                        className="w-full rounded-2xl border border-gray-200 px-4 py-3 outline-none focus:border-[#E6C229] focus:ring-4 focus:ring-[#E6C229]/10"
                                    />

                                    <input
                                        type="password"
                                        placeholder="Confirme a nova senha"
                                        className="w-full rounded-2xl border border-gray-200 px-4 py-3 outline-none focus:border-[#E6C229] focus:ring-4 focus:ring-[#E6C229]/10"
                                    />

                                    <div className="flex justify-end gap-3 mt-2">

                                        <button
                                            type="button"
                                            onClick={() => setMostrarSenha(false)}
                                            className="rounded-2xl border border-gray-200 px-5 py-3 text-sm text-gray-600 hover:bg-gray-50"
                                        >
                                            Cancelar
                                        </button>

                                        <button
                                            type="button"
                                            className="rounded-2xl bg-[#E6C229] px-5 py-3 text-sm font-medium text-white hover:bg-[#c9a91f]"
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