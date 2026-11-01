import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Building2, ChevronRight, Copy, KeyRound, Users } from "lucide-react";
import { useAuth } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import { listarEscolas, type EscolaResposta } from "../../services/EscolaApi";
import { listarUsuarios, type UsuarioResposta } from "../../services/UsuarioAdminApi";
import {
    consultarTokenCadastro,
    gerarTokenCadastro,
    type TokenAtual,
    type TokenGerado,
} from "../../services/BootstrapApi";
import { LayoutSecretaria } from "./layoutSecretaria";

/** Contagem regressiva em mm:ss, nunca negativa. */
function formatarRestante(milissegundos: number) {
    const total = Math.max(0, Math.floor(milissegundos / 1000));
    const minutos = Math.floor(total / 60);
    const segundos = total % 60;

    return `${String(minutos).padStart(2, "0")}:${String(segundos).padStart(2, "0")}`;
}

export function HomeSecretaria() {
    const { logout } = useAuth();
    const [escolas, setEscolas] = useState<EscolaResposta[]>([]);
    const [usuarios, setUsuarios] = useState<UsuarioResposta[]>([]);
    const [token, setToken] = useState<TokenAtual | null>(null);
    const [tokenGerado, setTokenGerado] = useState<TokenGerado | null>(null);
    const [agora, setAgora] = useState(() => Date.now());
    const [carregando, setCarregando] = useState(true);
    const [gerando, setGerando] = useState(false);
    const [copiado, setCopiado] = useState(false);
    const [erro, setErro] = useState("");

    useEffect(() => {
        let ativo = true;

        async function carregar() {
            try {
                const [escolasCarregadas, usuariosCarregados, tokenAtual] = await Promise.all([
                    listarEscolas(),
                    listarUsuarios(),
                    consultarTokenCadastro(),
                ]);

                if (!ativo) return;

                setEscolas(escolasCarregadas);
                setUsuarios(usuariosCarregados);
                setToken(tokenAtual);
            } catch (falha) {
                if (naoAutorizado(falha)) {
                    void logout();
                    return;
                }

                console.error("Erro ao carregar o painel da secretaria:", falha);

                if (ativo) {
                    setErro("Não foi possível carregar o painel.");
                }
            } finally {
                if (ativo) {
                    setCarregando(false);
                }
            }
        }

        void carregar();

        return () => {
            ativo = false;
        };
    }, [logout]);

    // Relógio da contagem regressiva: só existe enquanto há token ativo.
    useEffect(() => {
        if (!token?.ativo) {
            return;
        }

        const intervalo = window.setInterval(() => setAgora(Date.now()), 1000);

        return () => window.clearInterval(intervalo);
    }, [token]);

    const escolasAtivas = escolas.filter((escola) => escola.ativa).length;
    const usuariosAtivos = usuarios.filter((usuario) => usuario.ativo).length;

    const atalhos = [
        {
            titulo: "Escolas",
            descricao: `${escolas.length} cadastrada(s) · ${escolasAtivas} ativa(s)`,
            caminho: "/secretaria/escolas",
            icone: Building2,
        },
        {
            titulo: "Unidades",
            descricao: "Estrutura usada pelo cadastro por token",
            caminho: "/secretaria/unidades",
            icone: Building2,
        },
        {
            titulo: "Usuários",
            descricao: `${usuarios.length} conta(s) · ${usuariosAtivos} ativa(s)`,
            caminho: "/secretaria/usuarios",
            icone: Users,
        },
    ];

    const expiraEm = token?.expiraEm ? new Date(token.expiraEm).getTime() : 0;
    const restante = Math.max(0, expiraEm - agora);
    const tokenAtivo = token?.ativo === true && restante > 0;

    async function gerarToken() {
        setGerando(true);
        setErro("");
        setCopiado(false);

        try {
            const gerado = await gerarTokenCadastro();

            setTokenGerado(gerado);
            setToken(await consultarTokenCadastro());
            setAgora(Date.now());
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErro(falha instanceof Error ? falha.message : "Não foi possível gerar o token.");
        } finally {
            setGerando(false);
        }
    }

    async function copiarToken() {
        if (tokenGerado === null) {
            return;
        }

        try {
            await navigator.clipboard.writeText(tokenGerado.token);
            setCopiado(true);
        } catch {
            setErro("Não foi possível copiar. Selecione o token e copie manualmente.");
        }
    }

    return (
        <LayoutSecretaria>
            <h1 className="text-2xl font-bold text-vermelho">Secretaria</h1>
            <p className="mt-2 text-sm text-gray-500">
                Gerencie a rede: escolas, unidades, contas de acesso e o cadastro por token.
            </p>

            {erro && (
                <p role="alert" className="mt-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            {carregando ? (
                <p className="mt-6 text-sm text-gray-500">Carregando...</p>
            ) : (
                <>
                    <section className="mt-6 rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                        <div className="flex items-center gap-3">
                            <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-roxo/10 text-roxo">
                                <KeyRound size={20} />
                            </span>
                            <div>
                                <h2 className="text-lg font-semibold text-gray-800">
                                    Cadastro de administrador por token
                                </h2>
                                <p className="text-sm text-gray-500">
                                    Gere um token de <strong>30 minutos</strong> para criar outro
                                    administrador da sua unidade. O token vale uma única vez.
                                </p>
                            </div>
                        </div>

                        <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                            <span
                                className={`w-fit rounded-full px-3 py-1 text-xs font-medium ${tokenAtivo
                                    ? "bg-green-100 text-green-700"
                                    : "bg-gray-200 text-gray-600"
                                    }`}
                            >
                                {tokenAtivo ? "Token ativo" : "Sem token válido"}
                            </span>

                            <div className="flex flex-col items-start gap-2 sm:items-end">
                                <button
                                    type="button"
                                    onClick={gerarToken}
                                    disabled={tokenAtivo || gerando}
                                    className={`flex items-center gap-2 rounded-2xl border px-5 py-2.5 text-sm font-medium transition-colors disabled:opacity-50 ${tokenAtivo
                                        ? "border-gray-200 text-gray-500"
                                        : "border-green-200 text-green-700 hover:bg-green-50"
                                        }`}
                                >
                                    <KeyRound size={16} />
                                    {gerando
                                        ? "Gerando..."
                                        : tokenAtivo
                                            ? `Aguarde ${formatarRestante(restante)}`
                                            : "Gerar token de 30 minutos"}
                                </button>

                                <Link
                                    to="/bootstrap"
                                    className="text-xs text-roxo underline hover:no-underline"
                                >
                                    Abrir a página de cadastro
                                </Link>
                            </div>
                        </div>

                        {tokenGerado !== null && (
                            <div className="mt-4 rounded-xl border border-green-200 bg-green-50 p-4">
                                <p className="text-sm font-medium text-green-800">
                                    Copie agora: este token não será exibido novamente.
                                </p>

                                <div className="mt-2 flex flex-col gap-2 sm:flex-row sm:items-center">
                                    <code className="min-w-0 flex-1 break-all rounded-xl bg-white px-3 py-2 text-xs text-gray-700">
                                        {tokenGerado.token}
                                    </code>

                                    <button
                                        type="button"
                                        onClick={copiarToken}
                                        className="flex items-center justify-center gap-2 rounded-xl border border-green-300 px-4 py-2 text-sm font-medium text-green-700 hover:bg-green-100"
                                    >
                                        <Copy size={16} />
                                        {copiado ? "Copiado" : "Copiar"}
                                    </button>
                                </div>

                                <p className="mt-2 text-xs text-green-700">
                                    Válido até{" "}
                                    {new Date(tokenGerado.expiraEm).toLocaleTimeString("pt-BR", {
                                        hour: "2-digit",
                                        minute: "2-digit",
                                    })}
                                    . Depois disso o botão volta a liberar um novo token.
                                </p>
                            </div>
                        )}

                        {!tokenAtivo && tokenGerado === null && (
                            <p className="mt-4 rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-800">
                                Com o botão desabilitado, o cadastro fica bloqueado até o token
                                expirar. Sua conta precisa estar vinculada a uma unidade para gerar.
                            </p>
                        )}
                    </section>

                    <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                        {atalhos.map((atalho) => {
                            const Icone = atalho.icone;

                            return (
                                <Link
                                    key={atalho.caminho}
                                    to={atalho.caminho}
                                    className="group flex items-center justify-between rounded-2xl bg-white p-5 shadow-[0_5px_20px_rgba(1,1,1,0.1)] transition-shadow hover:shadow-[0_8px_25px_rgba(1,1,1,0.15)]"
                                >
                                    <div className="flex items-center gap-4">
                                        <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-roxo/10 text-roxo">
                                            <Icone size={22} />
                                        </span>
                                        <div>
                                            <h2 className="font-semibold text-gray-800">{atalho.titulo}</h2>
                                            <p className="text-sm text-gray-500">{atalho.descricao}</p>
                                        </div>
                                    </div>

                                    <ChevronRight
                                        size={20}
                                        className="text-gray-300 transition-transform group-hover:translate-x-1 group-hover:text-roxo"
                                    />
                                </Link>
                            );
                        })}
                    </div>
                </>
            )}
        </LayoutSecretaria>
    );
}
