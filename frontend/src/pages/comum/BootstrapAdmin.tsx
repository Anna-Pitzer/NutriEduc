import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ShieldCheck } from "lucide-react";
import { consultarBootstrap, criarAdminBootstrap } from "../../services/BootstrapApi";

const vazio = {
    token: "",
    unidade: "",
    nome: "",
    cpf: "",
    dataNascimento: "",
    email: "",
    senha: "",
};

/**
 * Página de bootstrap (Spec 022). Não aparece no menu e fica fora do `ProtectedRoutes`:
 * é o caminho de entrada antes de existir sessão. A proteção é o token de configuração,
 * não o segredo da URL.
 */
export function BootstrapAdmin() {
    const [disponivel, setDisponivel] = useState<boolean | null>(null);
    const [motivo, setMotivo] = useState<string>("");
    const [dados, setDados] = useState(vazio);
    const [erro, setErro] = useState("");
    const [salvando, setSalvando] = useState(false);
    const [criado, setCriado] = useState(false);

    useEffect(() => {
        let ativo = true;

        consultarBootstrap()
            .then((status) => {
                if (!ativo) return;

                setDisponivel(status.disponivel);
                setMotivo(status.motivo);
            })
            .catch((falha) => {
                console.error("Erro ao consultar o bootstrap:", falha);

                if (ativo) {
                    setDisponivel(false);
                    setErro("Não foi possível consultar a rota de bootstrap.");
                }
            });

        return () => {
            ativo = false;
        };
    }, []);

    function atualizarCampo(campo: keyof typeof vazio, valor: string) {
        setDados((atual) => ({ ...atual, [campo]: valor }));
    }

    async function handleCriar(evento: FormEvent) {
        evento.preventDefault();
        setErro("");
        setSalvando(true);

        try {
            await criarAdminBootstrap(dados);
            setCriado(true);
            setDados(vazio);
        } catch (falha) {
            setErro(falha instanceof Error ? falha.message : "Não foi possível criar o administrador.");
        } finally {
            setSalvando(false);
        }
    }

    return (
        <div className="flex min-h-screen items-center justify-center bg-[#FAF9F5] px-4 py-10">
            <main className="w-full max-w-2xl rounded-3xl bg-white p-8 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                <div className="flex items-center gap-3">
                    <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-roxo/10 text-roxo">
                        <ShieldCheck size={22} />
                    </span>
                    <div>
                        <h1 className="text-2xl font-bold text-primaria">Criar administrador</h1>
                        <p className="text-sm text-gray-500">
                            Rota de configuração, protegida por token. Não é preciso acessar o banco.
                        </p>
                    </div>
                </div>

                {disponivel === null && <p className="mt-6 text-sm text-gray-500">Verificando...</p>}

                {disponivel === false && motivo === "desativado-ambiente" && (
                    <p role="alert" className="mt-6 rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-800">
                        Cadastro por token <strong>desativado pelo ambiente</strong>.
                        Para liberar, inicie o backend com <code>Bootstrap__Permitir=true</code>.
                    </p>
                )}

                {disponivel === false && motivo === "sem-token" && (
                    <p role="alert" className="mt-6 rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-800">
                        Rota desativada. Peça à secretaria um token de cadastro (vale 30 minutos)
                        ou configure <code>Bootstrap__Token</code> no backend.
                    </p>
                )}

                {criado && (
                    <div className="mt-6 rounded-xl bg-green-50 px-4 py-3 text-sm text-green-800">
                        <p className="font-medium">Administrador criado.</p>
                        <p className="mt-1">
                            <Link to="/login" className="text-roxo underline">
                                Ir para o login
                            </Link>
                        </p>
                    </div>
                )}

                {erro && (
                    <p role="alert" className="mt-6 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                        {erro}
                    </p>
                )}

                {disponivel === true && !criado && (
                    <form onSubmit={handleCriar} className="mt-6 grid gap-4 md:grid-cols-2">
                        <div className="flex flex-col gap-1 md:col-span-2">
                            <label htmlFor="token" className="text-sm font-medium text-gray-700">
                                Token de configuração
                            </label>
                            <input
                                id="token"
                                type="password"
                                required
                                autoComplete="off"
                                value={dados.token}
                                onChange={(evento) => atualizarCampo("token", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                            <span className="text-xs text-gray-400">
                                Valor recebido da secretaria, ou o <code>Bootstrap__Token</code> do backend.
                            </span>
                        </div>

                        <div className="flex flex-col gap-1 md:col-span-2">
                            <label htmlFor="unidade" className="text-sm font-medium text-gray-700">
                                Unidade
                            </label>
                            <input
                                id="unidade"
                                value={dados.unidade}
                                onChange={(evento) => atualizarCampo("unidade", evento.target.value)}
                                placeholder="Ex.: Secretaria Municipal de Educação do Rio de Janeiro"
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                            <span className="text-xs text-gray-400">
                                Nome exato da unidade. Se ela não existir, é criada agora com este
                                nome. Com o token de 30 minutos a unidade vem de quem o gerou.
                            </span>
                        </div>

                        <div className="flex flex-col gap-1 md:col-span-2">
                            <label htmlFor="nome" className="text-sm font-medium text-gray-700">Nome</label>
                            <input
                                id="nome"
                                required
                                value={dados.nome}
                                onChange={(evento) => atualizarCampo("nome", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                        </div>

                        <div className="flex flex-col gap-1">
                            <label htmlFor="cpf" className="text-sm font-medium text-gray-700">CPF</label>
                            <input
                                id="cpf"
                                required
                                inputMode="numeric"
                                maxLength={11}
                                value={dados.cpf}
                                onChange={(evento) => atualizarCampo("cpf", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                        </div>

                        <div className="flex flex-col gap-1">
                            <label htmlFor="nascimento" className="text-sm font-medium text-gray-700">Nascimento</label>
                            <input
                                id="nascimento"
                                type="date"
                                required
                                value={dados.dataNascimento}
                                onChange={(evento) => atualizarCampo("dataNascimento", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                        </div>

                        <div className="flex flex-col gap-1 md:col-span-2">
                            <label htmlFor="email" className="text-sm font-medium text-gray-700">E-mail</label>
                            <input
                                id="email"
                                type="email"
                                required
                                value={dados.email}
                                onChange={(evento) => atualizarCampo("email", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                        </div>

                        <div className="flex flex-col gap-1 md:col-span-2">
                            <label htmlFor="senha" className="text-sm font-medium text-gray-700">Senha</label>
                            <input
                                id="senha"
                                type="password"
                                required
                                minLength={8}
                                value={dados.senha}
                                onChange={(evento) => atualizarCampo("senha", evento.target.value)}
                                className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                            />
                        </div>

                        <div className="md:col-span-2">
                            <button
                                type="submit"
                                disabled={salvando}
                                className="rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white transition-colors hover:bg-[#b31f55] disabled:opacity-50"
                            >
                                {salvando ? "Criando..." : "Criar administrador"}
                            </button>
                        </div>
                    </form>
                )}

                {disponivel === true && criado && (
                    <button
                        type="button"
                        onClick={() => setCriado(false)}
                        className="mt-6 rounded-2xl border border-roxo px-5 py-3 text-sm font-medium text-roxo hover:bg-roxo/10"
                    >
                        Criar outro administrador
                    </button>
                )}
            </main>
        </div>
    );
}
