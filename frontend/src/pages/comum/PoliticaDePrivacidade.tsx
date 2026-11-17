import { Link, useNavigate } from "react-router-dom";
import {
    ChevronLeft,
    Clock,
    Database,
    HeartPulse,
    Lock,
    RefreshCw,
    Share2,
    ShieldCheck,
    UserCheck,
    Users,
} from "lucide-react";
import Footer from "../../components/Footer";
import Header from "../../components/Header";

/**
 * Versão do texto. Atualizar sempre que uma seção mudar, para o usuário conseguir
 * diferenciar o que leu antes.
 */
const VERSAO = {
    numero: "1.0",
    atualizacao: "17/11/2026",
};

type Secao = {
    id: string;
    titulo: string;
    icone: typeof Database;
    paragrafos: string[];
};

/**
 * Fatos desta página verificados no código: senha só em hash (HashSenha), sessão em
 * cookie HttpOnly, isolamento por escopo (unidade/escola/vínculo), ausência de cliente
 * HTTP no backend e histórico append-only. Mudanças nesses pontos exigem revisão aqui.
 */
const SECOES: Secao[] = [
    {
        id: "dados-titular",
        titulo: "Dados que o sistema guarda por titular",
        icone: Database,
        paragrafos: [
            "Conta de usuário: nome, CPF, data de nascimento, e-mail, telefone, foto (opcional), CRN quando o perfil é de nutricionista, papel de acesso, unidade e escola de atuação e situação (ativa ou inativa). A senha não é guardada: o sistema persiste apenas um hash dela.",
            "Aluno: nome, data de nascimento, telefone, turma e série, observações, restrições e alergias alimentares, marcação de anafilaxia, escola e unidade, além do nome e do telefone do responsável informados no cadastro.",
            "Responsável: o vínculo com os alunos, que é o que define o que o portal da família mostra.",
            "Registros de uso: histórico de alterações do aluno com valor anterior e valor novo, cardápios com seu histórico de eventos, solicitações e seu histórico, ocorrências e seu histórico, feedbacks, notificações e trilha de ações administrativas. Esses registros guardam autor e data.",
        ],
    },
    {
        id: "saude",
        titulo: "Dados de saúde do aluno",
        icone: HeartPulse,
        paragrafos: [
            "Restrições alimentares, alergias, a marcação de anafilaxia e observações de saúde são dados pessoais sensíveis. Eles existem no sistema com uma finalidade única: impedir que um aluno receba alimento incompatível e permitir a substituição correta na versão adaptada do cardápio.",
            "Quem vê esses dados: a nutrição e a merenda da unidade, a administração da escola e a secretaria, sempre dentro do próprio escopo. O responsável vê apenas os filhos vinculados à sua conta. Consulta a aluno fora do escopo é recusada sem revelar se o aluno existe.",
            "O histórico de alterações dessas informações registra quem alterou, quando e qual era o valor anterior.",
        ],
    },
    {
        id: "acesso",
        titulo: "Quem acessa cada informação",
        icone: Users,
        paragrafos: [
            "Secretaria: administra contas, alunos, escolas e unidades da própria unidade e aprova pedidos de acesso.",
            "Nutrição: mantém catálogo de alimentos, restrições e cardápios, e decide solicitações da unidade.",
            "Merenda: consulta o cardápio da própria escola, registra ocorrências e abre solicitações.",
            "Escola: acompanha ocorrências e feedbacks da própria escola.",
            "Responsável: acessa somente os filhos vinculados à sua conta.",
            "Recursos de outra unidade não são devolvidos: o sistema responde como inexistente. Filtro fora do escopo autorizado é recusado. O mesmo vale para a exportação de relatórios, liberada apenas aos perfis autorizados e dentro do escopo da sessão.",
        ],
    },
    {
        id: "protecao",
        titulo: "Como as informações são protegidas",
        icone: Lock,
        paragrafos: [
            "Senha: guardada apenas como hash. A senha em texto não é persistida em nenhum momento.",
            "Sessão: mantida em cookie de autenticação marcado como HttpOnly, ou seja, inacessível a scripts da página. Desativar um usuário encerra o acesso dele na requisição seguinte.",
            "Isolamento: cada consulta é filtrada pela unidade, escola ou vínculo do usuário autenticado. A regra de permissão é declarada em uma matriz única, e o servidor recusa a operação mesmo que a tela permita o clique.",
            "CPF: validado e armazenado apenas com dígitos. É exibido em telas administrativas específicas (lista de nutricionistas, administração de contas e fila de pedidos de acesso) e no retrato de autoria do cardápio.",
            "Trilha: alterações de aluno, mudanças de status de solicitação e de ocorrência e ações administrativas ficam registradas com autor e data.",
        ],
    },
    {
        id: "compartilhamento",
        titulo: "Compartilhamento com terceiros",
        icone: Share2,
        paragrafos: [
            "O sistema não envia dados para serviços externos. Não há integração com provedores de análise, de publicidade, de e-mail marketing ou de inteligência artificial: o backend não realiza chamadas para fora.",
            "A única saída de dados é a exportação de relatórios em planilha, gerada pelo próprio sistema e entregue a quem tem perfil autorizado. A partir da exportação, o arquivo passa a ser responsabilidade da instituição que o gerou.",
        ],
    },
    {
        id: "retencao",
        titulo: "Por quanto tempo os dados ficam guardados",
        icone: Clock,
        paragrafos: [
            "Não existe rotina automática de expurgo. Os registros permanecem no banco até que sejam excluídos por uma ação administrativa.",
            "Desativar é diferente de excluir: desativar escola, unidade, aluno ou conta suspende o uso e preserva o histórico; excluir remove o registro.",
            "A exclusão é bloqueada quando ainda existe vínculo que depende do registro, por exemplo escola com usuários ou conta com vínculo de responsável. Isso evita que a exclusão deixe dados órfãos.",
            "Os históricos são append-only por desenho: foram feitos para não perder o que aconteceu, então corrigir um dado não apaga o registro anterior.",
        ],
    },
    {
        id: "direitos",
        titulo: "Direitos do titular e como exercê-los",
        icone: UserCheck,
        paragrafos: [
            "Acesso: o usuário consulta os próprios dados no perfil e, no caso do aluno, o responsável consulta os dados e o cardápio dos filhos vinculados.",
            "Correção: dados da própria conta são corrigidos no perfil. Dados do aluno são corrigidos por pedido de alteração de dados cadastrais, que registra o valor atual e o valor solicitado e só é aplicado após aprovação da secretaria.",
            "Eliminação e demais solicitações: devem ser dirigidas ao canal de atendimento da instituição. Enquanto esse canal não estiver publicado, use a área de solicitações do próprio sistema para registrar o pedido.",
            "Crianças e adolescentes: os dados do aluno são tratados para a finalidade de segurança alimentar escolar, e o exercício dos direitos é feito pelo responsável legal vinculado à conta.",
        ],
    },
];

export function PoliticaDePrivaciade() {
    const navigate = useNavigate();

    return (
        <div className="relative min-h-screen bg-[#FAF9F5]">
            <Header />

            <button
                type="button"
                onClick={() => navigate(-1)}
                aria-label="Voltar"
                className="group mx-6 flex cursor-pointer items-center justify-center"
            >
                <ChevronLeft
                    size={44}
                    className="text-roxo transition-transform duration-200 group-hover:-translate-x-2"
                />
            </button>

            <main className="mx-auto w-full max-w-4xl px-6 pb-16">
                <div className="flex items-start gap-3">
                    <ShieldCheck size={36} className="mt-1 text-roxo" />
                    <div>
                        <h1 className="text-3xl font-bold text-gray-800">
                            Política de privacidade
                        </h1>
                        <p className="text-sm text-gray-600">
                            Como o NutriEduc trata os dados pessoais de quem usa o sistema.
                        </p>
                        <p className="mt-1 flex items-center gap-2 text-xs text-gray-500">
                            <RefreshCw size={14} />
                            Versão {VERSAO.numero} — atualizada em {VERSAO.atualizacao}
                        </p>
                    </div>
                </div>

                <section className="mt-6 rounded-3xl bg-branco p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]">
                    <p className="text-sm text-gray-700">
                        Esta política descreve o que o sistema faz com os dados que recebe. Os
                        dados pertencem à instituição de ensino responsável pelo cadastro, que
                        define a finalidade e as pessoas autorizadas a acessá-los.
                    </p>

                    {/*
                     * TODO(conteudo): completar com os dados oficiais do controlador quando a
                     * instituição informar — razão social, CNPJ e canal do encarregado de dados.
                     * Não inventar nome de instituição, CNPJ nem e-mail.
                     */}
                    <div className="mt-4 rounded-2xl border border-gray-200 bg-[#FAF9F5] px-4 py-3 text-sm text-gray-700">
                        <p className="font-semibold">Controlador dos dados</p>
                        <p className="mt-1">
                            Instituição responsável:{" "}
                            <span className="text-gray-500">não configurado</span>
                        </p>
                        <p>
                            Encarregado de dados (DPO):{" "}
                            <span className="text-gray-500">não configurado</span>
                        </p>
                    </div>
                </section>

                <div className="mt-6 space-y-4">
                    {SECOES.map((secao, indice) => {
                        const Icone = secao.icone;

                        return (
                            <section
                                key={secao.id}
                                aria-labelledby={secao.id}
                                className="rounded-3xl bg-branco p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]"
                            >
                                <h2
                                    id={secao.id}
                                    className="flex items-center gap-2 text-xl font-bold text-gray-800"
                                >
                                    <Icone size={22} className="text-roxo" />
                                    {indice + 1}. {secao.titulo}
                                </h2>

                                <div className="mt-3 space-y-3 text-sm text-gray-700">
                                    {secao.paragrafos.map((texto) => (
                                        <p key={texto}>{texto}</p>
                                    ))}
                                </div>

                                {secao.id === "direitos" && (
                                    <p className="mt-3 text-sm text-gray-700">
                                        Veja também os{" "}
                                        <Link
                                            to="/termosdeuso"
                                            className="font-semibold text-roxo underline"
                                        >
                                            termos de uso
                                        </Link>{" "}
                                        e a página de{" "}
                                        <Link
                                            to="/suporte"
                                            className="font-semibold text-roxo underline"
                                        >
                                            suporte
                                        </Link>
                                        .
                                    </p>
                                )}
                            </section>
                        );
                    })}
                </div>

                <section className="mt-6 rounded-3xl border border-amber-200 bg-amber-50 p-6 text-sm text-amber-900">
                    <p className="font-semibold">Sobre a redação deste texto</p>
                    <p className="mt-1">
                        O conteúdo acima descreve o comportamento técnico atual do sistema. A
                        redação jurídica oficial e a indicação do controlador e do encarregado de
                        dados devem ser revisadas pela instituição antes da publicação como
                        política oficial.(APENAS PARA SIMULAÇÃO)
                    </p>
                </section>
            </main>
            <Footer />
        </div>
    );
}
