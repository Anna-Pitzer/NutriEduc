import Footer from "../components/Footer";
import Header from "../components/Header";
import Button from "../components/Button";
import { useState, useEffect } from "react";
import type { ChangeEvent } from "react";
import { ChevronLeft, Mail, Phone, User } from "lucide-react";
import { useAuth } from "../context/authContext";
import { buscarPerfil, buscarEstatisticas, atualizarPerfil } from "../services/Perfil";

type DadosPerfil = {
    nome: string;
    email: string;
    telefone: string;
    foto: string;
};

type EstatisticasPerfil = {
    escolasGerenciadas: number;
    alunosComRestricao: number;
    refeicoesGerenciadas: number;
};

export function Perfil() {
    const usuarioId = 15;
    //DESCOMENTA ISSO E COLOCA NO CONTEXT UM ID VINCULADO AO USER PRA EU PODER PUXAR OS DADOS PLS - E REMOVE ESSA LINHA DE CIMA
   /* const { usuarioId } = useAuth();
    const usuarioId = usuario?.id;*/

    const [foto, setFoto] = useState<string | null>(null);
    const [fotoOriginal, setFotoOriginal] = useState<string | null>(null);

    const [form, setForm] = useState<DadosPerfil>({
        nome: "",
        email: "",
        telefone: "",
        foto: "",
    });

    const [dadosOriginais, setDadosOriginais] =
        useState<DadosPerfil>({
            nome: "",
            email: "",
            telefone: "",
            foto: "",
        });

    const [estatisticas, setEstatisticas] =
        useState<EstatisticasPerfil>({
            escolasGerenciadas: 0,
            alunosComRestricao: 0,
            refeicoesGerenciadas: 0,
        });

    const [erro, setErro] = useState("");
    const [mensagem, setMensagem] = useState("");
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);

    const sanitizarNome = (valor: string) => {
        return valor
            .replace(/[^A-Za-zÀ-ÿ\s'-]/g, "")
            .replace(/\s+/g, " ")
            .trimStart()
            .slice(0, 100);
    };

    const sanitizarEmail = (valor: string) => {
        return valor.trim().toLowerCase().slice(0, 150);
    };

    const sanitizarTelefone = (valor: string) => {
        return valor.replace(/\D/g, "").slice(0, 11);
    };

    const formatarTelefone = (telefone: string) => {
        const numeros = sanitizarTelefone(telefone);

        if (!numeros) return "";

        if (numeros.length <= 2) {
            return `(${numeros}`;
        }

        const ddd = numeros.slice(0, 2);
        const restante = numeros.slice(2);

        if (numeros.length <= 10) {
            if (restante.length <= 4) {
                return `(${ddd}) ${restante}`;
            }

            return `(${ddd}) ${restante.slice(0, 4)}-${restante.slice(4)}`;
        }

        if (restante.length <= 5) {
            return `(${ddd}) ${restante}`;
        }

        return `(${ddd}) ${restante.slice(0, 5)}-${restante.slice(5)}`;
    };

    useEffect(() => {
        if (!usuarioId) return;

        const carregarPerfil = async () => {
            try {
                setCarregando(true);
                setErro("");

                const dados = await buscarPerfil(usuarioId);

                const dadosUsuario: DadosPerfil = {
                    nome: sanitizarNome(dados.nome || ""),
                    email: sanitizarEmail(dados.email || ""),
                    telefone: sanitizarTelefone(dados.telefone || ""),
                    foto: dados.foto || "",
                };

                setForm(dadosUsuario);
                setDadosOriginais(dadosUsuario);

                if (dados.foto) {
                    const fotoCarregada = dados.foto.startsWith("http")
                        ? dados.foto
                        : `http://localhost:5243${dados.foto}`;

                    setFoto(fotoCarregada);
                    setFotoOriginal(fotoCarregada);
                }
            } catch (error) {
                console.error("Erro ao carregar perfil:", error);
                setErro("Não foi possível carregar o perfil.");
            } finally {
                setCarregando(false);
            }
        };

        carregarPerfil();
    }, [usuarioId]);

    useEffect(() => {
        if (!usuarioId) return;

        const carregarEstatisticas = async () => {
            try {
                const dados = await buscarEstatisticas(usuarioId);

                setEstatisticas({
                    escolasGerenciadas: dados.escolasGerenciadas ?? 0,
                    alunosComRestricao: dados.alunosComRestricao ?? 0,
                    refeicoesGerenciadas: dados.refeicoesGerenciadas ?? 0,
                });
            } catch (error) {
                console.error("Erro ao carregar estatísticas:", error);
            }
        };

        carregarEstatisticas();
    }, [usuarioId]);

    const trocarFoto = (e: ChangeEvent<HTMLInputElement>) => {
        const arquivo = e.target.files?.[0];

        if (!arquivo) return;

        setErro("");
        setMensagem("");

        const tiposPermitidos = [
            "image/jpeg",
            "image/png",
            "image/webp",
        ];

        if (!tiposPermitidos.includes(arquivo.type)) {
            setErro("Selecione uma imagem PNG, JPG ou WEBP.");
            e.target.value = "";
            return;
        }

        if (arquivo.size > 5 * 1024 * 1024) {
            setErro("A imagem deve ter no máximo 5MB.");
            e.target.value = "";
            return;
        }

        const reader = new FileReader();

        reader.onloadend = () => {
            if (typeof reader.result === "string") {
                setFoto(reader.result);
            }
        };

        reader.onerror = () => {
            setErro("Não foi possível carregar a imagem.");
        };

        reader.readAsDataURL(arquivo);
    };

    const salvarPerfil = async () => {
        setErro("");
        setMensagem("");

        const dadosSanitizados: DadosPerfil = {
            nome: sanitizarNome(form.nome),
            email: sanitizarEmail(form.email),
            telefone: sanitizarTelefone(form.telefone),
            foto: foto || dadosOriginais.foto,
        };

        if (dadosSanitizados.nome.length < 2) {
            setErro("Digite um nome válido.");
            return;
        }

        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
            dadosSanitizados.email
        )
        ) {
            setErro("Digite um e-mail válido.");
            return;
        }

        if (
            dadosSanitizados.telefone.length !== 10 &&
            dadosSanitizados.telefone.length !== 11
        ) {
            setErro("Digite um telefone válido.");
            return;
        }

        try {
            setSalvando(true);
            
            if (!usuarioId) {
                setErro("Usuario  não autenticado."); 
                return;
            }

            await atualizarPerfil(usuarioId, dadosSanitizados);

            setForm(dadosSanitizados);
            setDadosOriginais(dadosSanitizados);

            setMensagem("Informações atualizadas com sucesso!");
        } catch (error) {
            console.error("Erro ao atualizar perfil:", error);
            setErro("Não foi possível atualizar o perfil.");
        } finally {
            setSalvando(false);
        }
    };

    const cancelarAlteracoes = () => {
        setForm(dadosOriginais);
        setFoto(fotoOriginal);
        setErro("");
        setMensagem("");
    };

    const cancelar = () => { window.history.back(); };

    const atualizarCampo = (
        campo: keyof DadosPerfil,
        valor: string
    ) => {
        setForm((anterior) => ({
            ...anterior,
            [campo]:
                campo === "nome"
                    ? sanitizarNome(valor)
                    : campo === "email"
                        ? sanitizarEmail(valor)
                        : sanitizarTelefone(valor),
        }));

        setErro("");
        setMensagem("");
    };

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={cancelar}
                aria-label="Voltar"
                className="group mx-10 flex cursor-pointer items-center justify-center"
            >
                <ChevronLeft
                    size={50}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <div className="mx-4 mb-10 flex flex-col gap-6 md:mx-10 lg:mx-30">

                <div className="rounded-3xl bg-branco p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <div className="flex flex-col">
                        <div className="flex items-center gap-4">
                            <label
                                htmlFor="fotoPerfil"
                                className="cursor-pointer"
                                title="Alterar foto de perfil"
                            >
                                {foto ? (
                                    <img
                                        src={foto}
                                        alt="Foto de perfil"
                                        className="h-24 w-24 rounded-full border border-gray-300 object-cover transition hover:opacity-80"
                                    />
                                ) : (
                                    <div className="flex h-24 w-24 items-center justify-center rounded-full border border-gray-300 bg-white transition hover:opacity-80">
                                        <User
                                            size={42}
                                            className="text-roxo"
                                        />
                                    </div>
                                )}
                            </label>

                            <div>
                                <p className="font-semibold">
                                    {form.nome || "Carregando perfil..."}
                                </p>

                                <label
                                    htmlFor="fotoPerfil"
                                    className="cursor-pointer text-sm text-roxo hover:underline"
                                >
                                    Alterar foto
                                </label>

                                <p className="text-xs text-gray-500">
                                    PNG, JPG ou WEBP (máx. 5MB)
                                </p>
                            </div>

                            <input
                                id="fotoPerfil"
                                type="file"
                                accept="image/png,image/jpeg,image/webp"
                                className="hidden"
                                onChange={trocarFoto}
                            />
                        </div>

                        <div className="my-5 w-full border border-gray-200" />

                        <div className="flex flex-col justify-around gap-4 md:flex-row">
                            <div className="flex items-center gap-2">
                                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gray-200">
                                    <Mail size={20} color="#0B6619" />
                                </div>

                                <p className="break-all">
                                    Email: {form.email || "—"}
                                </p>
                            </div>

                            <div className="flex items-center gap-2">
                                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gray-200">
                                    <Phone size={20} color="#0B6619" />
                                </div>

                                <p>
                                    Telefone: {formatarTelefone(form.telefone) || "—"}
                                </p>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                    <div className="flex flex-col items-center rounded-3xl bg-branco p-6 text-center shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="text-3xl font-bold text-verdelodo">
                            {estatisticas.escolasGerenciadas}
                        </h2>
                        <p className="text-gray-500">
                            Escolas gerenciadas
                        </p>
                    </div>

                    <div className="flex flex-col items-center rounded-3xl bg-branco p-6 text-center shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                        <h2 className="text-3xl font-bold text-laranja">
                            {estatisticas.alunosComRestricao}
                        </h2>
                        <p className="text-gray-500">
                            Alunos com restrição
                        </p>
                    </div>

                    <div className="flex flex-col items-center rounded-3xl bg-branco p-6 text-center shadow-[0_8px_30px_rgba(11,102,25,0.08)] sm:col-span-2 lg:col-span-1">
                        <h2 className="text-3xl font-bold text-roxo">
                            {estatisticas.refeicoesGerenciadas}
                        </h2>
                        <p className="text-gray-500">
                            Refeições gerenciadas
                        </p>
                    </div>
                </div>

                <div className="mt-4 flex flex-col rounded-3xl bg-branco p-4 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <h2 className="text-2xl font-bold">
                        Editar Informações
                    </h2>

                    <div className="my-5 w-full border border-gray-200" />

                    {erro && (
                        <div
                            role="alert"
                            className="mb-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600"
                        >
                            {erro}
                        </div>
                    )}

                    {mensagem && (
                        <div
                            role="status"
                            className="mb-4 rounded-2xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700"
                        >
                            {mensagem}
                        </div>
                    )}

                    {carregando ? (
                        <p className="py-4 text-gray-500">
                            Carregando informações...
                        </p>
                    ) : (
                        <>
                            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">

                                <div className="flex flex-col gap-1">
                                    <label htmlFor="nome" className="text-sm font-medium">
                                        Nome
                                    </label>

                                    <input
                                        id="nome"
                                        type="text"
                                        autoComplete="name"
                                        className="my-2 w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all placeholder:text-gray-400 hover:border-gray-300 focus:border-green-500 focus:ring-4 focus:ring-green-500/10"
                                        placeholder="Digite seu nome"
                                        value={form.nome}
                                        onChange={(e) =>
                                            atualizarCampo("nome", e.target.value)
                                        }
                                        maxLength={100}
                                    />
                                </div>

                                <div className="flex flex-col gap-1">
                                    <label htmlFor="email" className="text-sm font-medium">
                                        E-mail
                                    </label>

                                    <input
                                        id="email"
                                        type="email"
                                        autoComplete="email"
                                        className="my-2 w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all placeholder:text-gray-400 hover:border-gray-300 focus:border-green-500 focus:ring-4 focus:ring-green-500/10"
                                        placeholder="Digite seu e-mail"
                                        value={form.email}
                                        onChange={(e) =>
                                            atualizarCampo("email", e.target.value)
                                        }
                                        maxLength={150}
                                    />
                                </div>

                                <div className="flex flex-col gap-1 md:col-span-2">
                                    <label htmlFor="telefone" className="text-sm font-medium">
                                        Telefone
                                    </label>

                                    <input
                                        id="telefone"
                                        type="tel"
                                        autoComplete="tel"
                                        className="my-2 w-full rounded-2xl border border-gray-200 bg-white px-4 py-3 text-sm text-gray-700 outline-none transition-all placeholder:text-gray-400 hover:border-gray-300 focus:border-green-500 focus:ring-4 focus:ring-green-500/10"
                                        placeholder="(00) 00000-0000"
                                        value={formatarTelefone(form.telefone)}
                                        onChange={(e) =>
                                            atualizarCampo("telefone", e.target.value)
                                        }
                                        maxLength={15}
                                    />
                                </div>
                            </div>

                            <div className="mt-4 flex flex-wrap items-center gap-3">
                                <Button
                                    type="button"
                                    variant="normal"
                                    onClick={salvarPerfil}
                                >
                                    {salvando ? "Salvando..." : "Salvar Alterações"}
                                </Button>

                                <Button
                                    type="button"
                                    variant="cancelar"
                                    onClick={cancelarAlteracoes}
                                >
                                    Cancelar
                                </Button>
                            </div>
                        </>
                    )}
                </div>
            </div>

            <Footer />
        </div>
    );
}