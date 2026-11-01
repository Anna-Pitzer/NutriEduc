import { useEffect, useState, type FormEvent } from "react";
import { Ban, RotateCcw, Trash2 } from "lucide-react";
import { useAuth, type PapelUsuario } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import { listarEscolas, listarUnidades, type EscolaResposta, type UnidadeResposta } from "../../services/EscolaApi";
import {
    atualizarUsuario,
    criarUsuario,
    definirUsuarioAtivo,
    excluirUsuario,
    exigeEscola,
    listarUsuarios,
    papeisDisponiveis,
    rotuloPapel,
    type UsuarioResposta,
} from "../../services/UsuarioAdminApi";
import { LayoutSecretaria } from "./layoutSecretaria";

/** Papel sugerido para uma conta que ainda não tem papel: menor privilégio. */
const papelInicial: PapelUsuario = "Merendeira";

const usuarioVazio = {
    nome: "",
    cpf: "",
    nascimento: "",
    email: "",
    senha: "",
    papel: "Nutricionista" as PapelUsuario,
    escolaId: "" as number | "",
};

export function Usuarios() {
    const { userLogin, logout } = useAuth();

    const [usuarios, setUsuarios] = useState<UsuarioResposta[]>([]);
    const [escolas, setEscolas] = useState<EscolaResposta[]>([]);
    // Carregadas sob demanda: só a edição de um AdminSecretaria precisa da lista.
    const [unidades, setUnidades] = useState<UnidadeResposta[] | null>(null);

    const [carregando, setCarregando] = useState(true);
    const [erroLista, setErroLista] = useState("");
    const [erroFormulario, setErroFormulario] = useState("");
    const [salvando, setSalvando] = useState(false);
    const [versao, setVersao] = useState(0);

    const [filtroPapel, setFiltroPapel] = useState<PapelUsuario | "">("");
    const [filtroEscola, setFiltroEscola] = useState<number | "">("");
    const [filtroAtivo, setFiltroAtivo] = useState<"todos" | "ativos" | "desativados">("todos");
    const [busca, setBusca] = useState("");
    const [buscaAplicada, setBuscaAplicada] = useState("");

    const [novo, setNovo] = useState(usuarioVazio);

    // Linha em edição de papel: existe no máximo uma por vez.
    const [edicao, setEdicao] = useState<{ id: number; papel: PapelUsuario; escolaId: number | ""; unidadeId: number | "" } | null>(null);
    const [salvandoPapel, setSalvandoPapel] = useState(false);

    useEffect(() => {
        let ativo = true;

        listarEscolas()
            .then((dados) => {
                if (ativo) setEscolas(dados);
            })
            .catch((falha) => {
                if (naoAutorizado(falha)) {
                    void logout();
                    return;
                }

                console.error("Erro ao carregar escolas:", falha);
            });

        return () => {
            ativo = false;
        };
    }, [logout]);

    useEffect(() => {
        let ativo = true;

        listarUsuarios({
            papel: filtroPapel,
            escolaId: filtroEscola,
            busca: buscaAplicada,
            ativo: filtroAtivo === "todos" ? undefined : filtroAtivo === "ativos",
        })
            .then((dados) => {
                if (ativo) setUsuarios(dados);
            })
            .catch((falha) => {
                if (naoAutorizado(falha)) {
                    void logout();
                    return;
                }

                console.error("Erro ao carregar usuários:", falha);

                if (ativo) setErroLista("Não foi possível carregar os usuários.");
            })
            .finally(() => {
                if (ativo) setCarregando(false);
            });

        return () => {
            ativo = false;
        };
    }, [filtroPapel, filtroEscola, filtroAtivo, buscaAplicada, versao, logout]);

    function atualizarCampo<K extends keyof typeof usuarioVazio>(campo: K, valor: (typeof usuarioVazio)[K]) {
        setNovo((atual) => ({ ...atual, [campo]: valor }));
    }

    function aoTrocarPapel(papel: PapelUsuario) {
        setNovo((atual) => ({
            ...atual,
            papel,
            escolaId: exigeEscola(papel) ? atual.escolaId : "",
        }));
    }

    async function handleCriar(evento: FormEvent) {
        evento.preventDefault();
        setErroFormulario("");

        if (exigeEscola(novo.papel) && novo.escolaId === "") {
            setErroFormulario("Selecione a escola deste usuário.");
            return;
        }

        setSalvando(true);

        try {
            await criarUsuario({
                nome: novo.nome.trim(),
                cpf: novo.cpf.trim(),
                dataNascimento: novo.nascimento,
                email: novo.email.trim(),
                senha: novo.senha,
                papel: novo.papel,
                escolaId: exigeEscola(novo.papel) && novo.escolaId !== "" ? novo.escolaId : null,
            });

            setNovo(usuarioVazio);
            setVersao((atual) => atual + 1);
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErroFormulario(falha instanceof Error ? falha.message : "Não foi possível criar o usuário.");
        } finally {
            setSalvando(false);
        }
    }

    async function alternarAtivo(usuario: UsuarioResposta) {
        try {
            const atualizado = await definirUsuarioAtivo(usuario.id, !usuario.ativo);
            setUsuarios((atuais) => atuais.map((item) => (item.id === usuario.id ? atualizado : item)));
            setErroLista("");
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErroLista(falha instanceof Error ? falha.message : "Não foi possível alterar a situação.");
        }
    }

    async function excluir(usuario: UsuarioResposta) {
        const rotulo = usuario.nome ?? usuario.email ?? `#${usuario.id}`;

        if (!window.confirm(`Excluir “${rotulo}”? Esta ação não pode ser desfeita.`)) {
            return;
        }

        try {
            await excluirUsuario(usuario.id);
            setUsuarios((atuais) => atuais.filter((item) => item.id !== usuario.id));
            setErroLista("");
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErroLista(falha instanceof Error ? falha.message : "Não foi possível excluir a conta.");
        }
    }

    function iniciarEdicao(usuario: UsuarioResposta) {
        setErroLista("");

        const papel = usuario.papel ?? papelInicial;

        setEdicao({
            id: usuario.id,
            papel,
            escolaId: usuario.escolaId ?? "",
            unidadeId: usuario.unidadeId ?? "",
        });

        if (papel === "AdminSecretaria") {
            void carregarUnidades();
        }
    }

    /**
     * Carregada sob demanda: só a edição de um `AdminSecretaria` precisa da lista.
     */
    async function carregarUnidades() {
        if (unidades !== null) {
            return;
        }

        try {
            setUnidades(await listarUnidades());
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErroLista("Não foi possível carregar as unidades.");
        }
    }

    async function salvarPapel(usuario: UsuarioResposta) {
        if (edicao === null) {
            return;
        }

        if (exigeEscola(edicao.papel) && edicao.escolaId === "") {
            setErroLista("Selecione a escola exigida por este papel.");
            return;
        }

        setSalvandoPapel(true);

        try {
            const atualizado = await atualizarUsuario(usuario.id, {
                nome: usuario.nome ?? "",
                email: usuario.email ?? "",
                // Enviado para não apagar o telefone: o backend trata `null` como "limpar".
                telefone: usuario.telefone,
                papel: edicao.papel,
                escolaId: exigeEscola(edicao.papel) && edicao.escolaId !== "" ? edicao.escolaId : null,
                // Unidade só é aceita pelo domínio para AdminSecretaria (atribuição).
                unidadeId: edicao.papel === "AdminSecretaria" && edicao.unidadeId !== ""
                    ? edicao.unidadeId
                    : null,
            });

            setUsuarios((atuais) => atuais.map((item) => (item.id === usuario.id ? atualizado : item)));
            setEdicao(null);
            setErroLista("");

            // Papel e unidade viram claims no login. Alterar a própria conta sem renovar a
            // sessão manteria o token do painel bloqueado, então exige novo login.
            if (usuario.id === userLogin?.id) {
                await logout();
            }
        } catch (falha) {
            if (naoAutorizado(falha)) {
                void logout();
                return;
            }

            setErroLista(falha instanceof Error ? falha.message : "Não foi possível definir o papel.");
        } finally {
            setSalvandoPapel(false);
        }
    }

    function nomeDaEscola(escolaId: number | null) {
        if (escolaId === null) return "—";

        return escolas.find((escola) => escola.id === escolaId)?.nome ?? `#${escolaId}`;
    }

    return (
        <LayoutSecretaria>
            <h1 className="mb-4 text-2xl font-bold text-vermelho">Usuários</h1>

            {erroLista && (
                <p role="alert" className="mb-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erroLista}
                </p>
            )}

            <section className="rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                <h2 className="text-lg font-semibold text-gray-800">Novo usuário</h2>
                <p className="mt-1 text-sm text-gray-500">
                    Quem define o que a conta pode fazer é a secretaria. Contas sem papel não autenticam.
                </p>

                {erroFormulario && (
                    <p role="alert" className="mt-3 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                        {erroFormulario}
                    </p>
                )}

                <form onSubmit={handleCriar} className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    <div className="flex flex-col gap-1">
                        <label htmlFor="nome" className="text-sm font-medium text-gray-700">Nome</label>
                        <input
                            id="nome"
                            required
                            value={novo.nome}
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
                            value={novo.cpf}
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
                            value={novo.nascimento}
                            onChange={(evento) => atualizarCampo("nascimento", evento.target.value)}
                            className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                        />
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="email" className="text-sm font-medium text-gray-700">E-mail</label>
                        <input
                            id="email"
                            type="email"
                            required
                            value={novo.email}
                            onChange={(evento) => atualizarCampo("email", evento.target.value)}
                            className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                        />
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="senha" className="text-sm font-medium text-gray-700">Senha</label>
                        <input
                            id="senha"
                            type="password"
                            required
                            minLength={8}
                            value={novo.senha}
                            onChange={(evento) => atualizarCampo("senha", evento.target.value)}
                            className="h-11 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo"
                        />
                    </div>

                    <div className="flex flex-col gap-1">
                        <label htmlFor="papel" className="text-sm font-medium text-gray-700">Papel</label>
                        <select
                            id="papel"
                            value={novo.papel}
                            onChange={(evento) => aoTrocarPapel(evento.target.value as PapelUsuario)}
                            className="h-11 rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo"
                        >
                            {papeisDisponiveis.map((papel) => (
                                <option key={papel} value={papel}>{rotuloPapel[papel]}</option>
                            ))}
                        </select>
                    </div>

                    {exigeEscola(novo.papel) && (
                        <div className="flex flex-col gap-1">
                            <label htmlFor="escola-usuario" className="text-sm font-medium text-gray-700">Escola</label>
                            <select
                                id="escola-usuario"
                                required
                                value={novo.escolaId}
                                onChange={(evento) => setNovo((atual) => ({
                                    ...atual,
                                    escolaId: evento.target.value === "" ? "" : Number(evento.target.value),
                                }))}
                                className="h-11 rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo"
                            >
                                <option value="">Selecione</option>
                                {escolas.map((escola) => (
                                    <option key={escola.id} value={escola.id}>{escola.nome}</option>
                                ))}
                            </select>
                        </div>
                    )}

                    <div className="flex items-end">
                        <button
                            type="submit"
                            disabled={salvando}
                            className="rounded-2xl bg-primaria px-5 py-3 text-sm font-medium text-white transition-colors hover:bg-[#b31f55] disabled:opacity-50"
                        >
                            {salvando ? "Criando..." : "Criar usuário"}
                        </button>
                    </div>
                </form>
            </section>

            <section className="mt-6 rounded-2xl bg-white p-6 shadow-[0_5px_20px_rgba(1,1,1,0.1)]">
                <h2 className="text-lg font-semibold text-gray-800">Contas da rede</h2>
                <p className="mt-1 text-sm text-gray-500">
                    Contas criadas pelo autocadastro aparecem como <strong>Sem papel</strong>. Use
                    <em> Definir papel</em> para liberar o acesso.
                </p>

                <form
                    onSubmit={(evento) => {
                        evento.preventDefault();
                        setBuscaAplicada(busca);
                    }}
                    className="mt-4 flex flex-col gap-2 sm:flex-row sm:flex-wrap"
                >
                    <label htmlFor="filtro-papel" className="sr-only">Papel</label>
                    <select
                        id="filtro-papel"
                        value={filtroPapel}
                        onChange={(evento) => setFiltroPapel(evento.target.value as PapelUsuario | "")}
                        className="h-11 w-full rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo sm:w-auto"
                    >
                        <option value="">Todos os papéis</option>
                        {papeisDisponiveis.map((papel) => (
                            <option key={papel} value={papel}>{rotuloPapel[papel]}</option>
                        ))}
                    </select>

                    <label htmlFor="filtro-escola" className="sr-only">Escola</label>
                    <select
                        id="filtro-escola"
                        value={filtroEscola}
                        onChange={(evento) => setFiltroEscola(evento.target.value === "" ? "" : Number(evento.target.value))}
                        className="h-11 w-full rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo sm:w-auto"
                    >
                        <option value="">Todas as escolas</option>
                        {escolas.map((escola) => (
                            <option key={escola.id} value={escola.id}>{escola.nome}</option>
                        ))}
                    </select>

                    <label htmlFor="filtro-situacao" className="sr-only">Situação</label>
                    <select
                        id="filtro-situacao"
                        value={filtroAtivo}
                        onChange={(evento) => setFiltroAtivo(
                            evento.target.value as "todos" | "ativos" | "desativados",
                        )}
                        className="h-11 w-full rounded-2xl border border-gray-200 bg-white px-4 text-sm outline-none focus:border-roxo sm:w-auto"
                    >
                        <option value="todos">Toda situação</option>
                        <option value="ativos">Ativas</option>
                        <option value="desativados">Desativadas</option>
                    </select>

                    <label htmlFor="filtro-busca" className="sr-only">Buscar</label>
                    <input
                        id="filtro-busca"
                        type="text"
                        value={busca}
                        onChange={(evento) => setBusca(evento.target.value)}
                        placeholder="Nome ou e-mail"
                        className="h-11 w-full flex-1 rounded-2xl border border-gray-200 px-4 text-sm outline-none focus:border-roxo sm:min-w-48"
                    />

                    <button
                        type="submit"
                        className="w-full rounded-2xl border border-roxo px-5 py-3 text-sm font-medium text-roxo transition-colors hover:bg-roxo/10 sm:w-auto"
                    >
                        Filtrar
                    </button>
                </form>

                {carregando ? (
                    <p className="mt-6 text-sm text-gray-500">Carregando...</p>
                ) : usuarios.length === 0 ? (
                    <p className="mt-6 text-sm text-gray-500">Nenhum usuário encontrado.</p>
                ) : (
                    <div className="mt-6 w-full overflow-x-auto">
                    <table className="w-full min-w-[44rem] border-collapse text-left text-sm">
                        <thead>
                            <tr className="border-b border-gray-200 text-xs uppercase tracking-wider text-gray-500">
                                <th className="py-2">Nome</th>
                                <th className="py-2">E-mail</th>
                                <th className="py-2">Papel</th>
                                <th className="py-2">Escola / Unidade</th>
                                <th className="py-2">Situação</th>
                                <th className="py-2 text-right">Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {usuarios.map((usuario) => {
                                const editando = edicao?.id === usuario.id;
                                const podeEditarPapel = Boolean(usuario.nome && usuario.email);

                                return (
                                    <tr key={usuario.id} className="border-b border-gray-100 last:border-0">
                                        <td className="py-3 pr-4">{usuario.nome ?? "—"}</td>
                                        <td className="py-3 pr-4 text-gray-600">{usuario.email ?? "—"}</td>
                                        <td className="py-3 pr-4">
                                            {editando && edicao ? (
                                                <>
                                                    <label htmlFor={`papel-${usuario.id}`} className="sr-only">Papel</label>
                                                    <select
                                                        id={`papel-${usuario.id}`}
                                                        value={edicao.papel}
                                                        onChange={(evento) => {
                                                            const papel = evento.target.value as PapelUsuario;

                                                            setEdicao({
                                                                ...edicao,
                                                                papel,
                                                                escolaId: exigeEscola(papel) ? edicao.escolaId : "",
                                                                unidadeId: papel === "AdminSecretaria" ? edicao.unidadeId : "",
                                                            });

                                                            if (papel === "AdminSecretaria") {
                                                                void carregarUnidades();
                                                            }
                                                        }}
                                                        className="h-9 rounded-xl border border-gray-200 bg-white px-2 text-sm outline-none focus:border-roxo"
                                                    >
                                                        {papeisDisponiveis.map((papel) => (
                                                            <option key={papel} value={papel}>{rotuloPapel[papel]}</option>
                                                        ))}
                                                    </select>
                                                </>
                                            ) : (
                                                usuario.papel ? rotuloPapel[usuario.papel] : "Sem papel"
                                            )}
                                        </td>
                                        <td className="py-3 pr-4 text-gray-600">
                                            {editando && edicao ? (
                                                edicao.papel === "AdminSecretaria" ? (
                                                    unidades === null ? (
                                                        <span className="text-xs text-gray-400">
                                                            Carregando unidades...
                                                        </span>
                                                    ) : (
                                                        <>
                                                            <label htmlFor={`unidade-${usuario.id}`} className="sr-only">Unidade</label>
                                                            <select
                                                                id={`unidade-${usuario.id}`}
                                                                value={edicao.unidadeId}
                                                                onChange={(evento) => setEdicao({
                                                                    ...edicao,
                                                                    unidadeId: evento.target.value === ""
                                                                        ? ""
                                                                        : Number(evento.target.value),
                                                                })}
                                                                className="h-9 max-w-full rounded-xl border border-gray-200 bg-white px-2 text-sm outline-none focus:border-roxo"
                                                            >
                                                                <option value="">Sem unidade</option>
                                                                {unidades.map((unidade) => (
                                                                    <option key={unidade.id} value={unidade.id}>
                                                                        {unidade.nome}
                                                                    </option>
                                                                ))}
                                                            </select>
                                                        </>
                                                    )
                                                ) : exigeEscola(edicao.papel) ? (
                                                    <>
                                                        <label htmlFor={`escola-${usuario.id}`} className="sr-only">Escola</label>
                                                        <select
                                                            id={`escola-${usuario.id}`}
                                                            value={edicao.escolaId}
                                                            onChange={(evento) => setEdicao({
                                                                ...edicao,
                                                                escolaId: evento.target.value === ""
                                                                    ? ""
                                                                    : Number(evento.target.value),
                                                            })}
                                                            className="h-9 rounded-xl border border-gray-200 bg-white px-2 text-sm outline-none focus:border-roxo"
                                                        >
                                                            <option value="">Selecione</option>
                                                            {escolas.map((escola) => (
                                                                <option key={escola.id} value={escola.id}>{escola.nome}</option>
                                                            ))}
                                                        </select>
                                                    </>
                                                ) : (
                                                    "—"
                                                )
                                            ) : (
                                                nomeDaEscola(usuario.escolaId)
                                            )}
                                        </td>
                                        <td className="py-3 pr-4">
                                            <span
                                                className={`rounded-full px-3 py-1 text-xs font-medium ${usuario.ativo
                                                    ? "bg-green-100 text-green-700"
                                                    : "bg-gray-200 text-gray-600"
                                                    }`}
                                            >
                                                {usuario.ativo ? "Ativa" : "Desativada"}
                                            </span>
                                        </td>
                                        <td className="py-3 text-right">
                                            <div className="flex flex-wrap justify-end gap-1">
                                                {editando ? (
                                                    <>
                                                        <button
                                                            type="button"
                                                            disabled={salvandoPapel}
                                                            onClick={() => salvarPapel(usuario)}
                                                            className="rounded-xl bg-primaria px-3 py-1.5 text-sm font-medium text-white disabled:opacity-50"
                                                        >
                                                            {salvandoPapel ? "Salvando..." : "Salvar"}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => setEdicao(null)}
                                                            className="rounded-xl px-3 py-1.5 text-sm font-medium text-gray-500 hover:bg-gray-100"
                                                        >
                                                            Cancelar
                                                        </button>
                                                    </>
                                                ) : (
                                                    <>
                                                        <button
                                                            type="button"
                                                            disabled={!podeEditarPapel}
                                                            title={podeEditarPapel
                                                                ? undefined
                                                                : "Conta sem nome ou e-mail não pode receber papel"}
                                                            onClick={() => iniciarEdicao(usuario)}
                                                            className="rounded-xl px-3 py-1.5 text-sm font-medium text-roxo hover:bg-roxo/10 disabled:opacity-40"
                                                        >
                                                            {usuario.papel ? "Alterar papel" : "Definir papel"}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => alternarAtivo(usuario)}
                                                            title={usuario.ativo
                                                                ? "Desativar esta conta (não poderá autenticar)"
                                                                : "Reativar esta conta"}
                                                            className={`flex items-center gap-1.5 rounded-xl border px-3 py-1.5 text-sm font-medium transition-colors ${usuario.ativo
                                                                ? "border-red-200 text-red-600 hover:bg-red-50"
                                                                : "border-green-200 text-green-700 hover:bg-green-50"
                                                                }`}
                                                        >
                                                            {usuario.ativo
                                                                ? <><Ban size={16} /> Desativar</>
                                                                : <><RotateCcw size={16} /> Reativar</>}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => excluir(usuario)}
                                                            title="Excluir definitivamente (bloqueado para própria conta, último admin, vínculo ou token)"
                                                            className="flex items-center gap-1.5 rounded-xl border border-red-200 px-3 py-1.5 text-sm font-medium text-red-700 transition-colors hover:bg-red-50"
                                                        >
                                                            <Trash2 size={16} /> Excluir
                                                        </button>
                                                    </>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                    </div>
                )}
            </section>
        </LayoutSecretaria>
    );
}
