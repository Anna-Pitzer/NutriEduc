import { Link, useNavigate } from "react-router-dom";
import {
    ChevronLeft,
    Clock,
    FileText,
    LifeBuoy,
    Mail,
    Phone,
    ShieldCheck,
} from "lucide-react";
import Footer from "../../components/Footer";
import Header from "../../components/Header";
import { useAuth } from "../../context/authContext";
import type { PapelUsuario } from "../../context/authContext";

type Pergunta = {
    pergunta: string;
    resposta: string;
};

type GrupoDeAjuda = {
    papel: PapelUsuario | "Geral";
    titulo: string;
    perguntas: Pergunta[];
};

/**
 * Conteúdo de ajuda escrito a partir do comportamento real do sistema (matriz de
 * permissões, transições de estado e validações). Ao mudar uma regra de negócio,
 * a resposta correspondente precisa ser revisada aqui.
 */
const GRUPOS: GrupoDeAjuda[] = [
    {
        papel: "Geral",
        titulo: "Todos os perfis",
        perguntas: [
            {
                pergunta: "Fiz o cadastro, mas o login é recusado. Por quê?",
                resposta:
                    "O autocadastro cria a conta sem papel e registra um pedido de acesso para a secretaria da sua unidade. A conta só passa a autenticar depois que o pedido for aprovado.",
            },
            {
                pergunta: "Como troco minha senha?",
                resposta:
                    "A troca de senha pelo próprio usuário ainda não está disponível no sistema. Solicite a alteração ao administrador da sua unidade.",
            },
            {
                pergunta: "Fui desconectado sem aviso.",
                resposta:
                    "A sessão fica no cookie do navegador. Se o seu usuário for desativado pela secretaria, o acesso é encerrado já na requisição seguinte.",
            },
            {
                pergunta: "Onde vejo os avisos do sistema?",
                resposta:
                    "No sino de notificações, na barra superior. Ele reúne avisos de cardápio publicado ou alterado, mudança de status de solicitação e de ocorrência, aluno atualizado, feedback respondido, nova solicitação, nova ocorrência e novos pedidos de acesso.",
            },
            {
                pergunta: "Meu nome ou e-mail no perfil está errado.",
                resposta:
                    "Você mesmo corrige pelo seu perfil. Já o papel e o escopo (unidade ou escola) só podem ser alterados pelo administrador da secretaria.",
            },
        ],
    },
    {
        papel: "AdminSecretaria",
        titulo: "Secretaria",
        perguntas: [
            {
                pergunta: "Por que o cadastro de aluno exige uma escola?",
                resposta:
                    "A escola é obrigatória porque a unidade do aluno é derivada dela. Aluno sem escola não participa da análise de compatibilidade do cardápio.",
            },
            {
                pergunta: "A exclusão de uma escola, unidade ou conta é recusada.",
                resposta:
                    "A exclusão é bloqueada quando ainda existe vínculo: escola com usuários, unidade com usuário, aluno ou token, conta com vínculo ou token emitido. Também não é possível excluir a própria conta nem o último administrador.",
            },
            {
                pergunta: "Como aprovo um pedido de acesso?",
                resposta:
                    "A fila fica em Secretaria › Pedidos de acesso. A aprovação grava, de uma só vez, papel, escopo, CRN e vínculos do responsável. Nenhuma aprovação concede papel administrativo.",
            },
            {
                pergunta: "O token de cadastro expirou.",
                resposta:
                    "O token vale 30 minutos e é exibido uma única vez. Se perder o prazo, gere outro na área da secretaria.",
            },
            {
                pergunta: "Como vinculo um responsável a um aluno?",
                resposta:
                    "Pela tela de alunos: além do contato manual, ela mostra as contas vinculadas e permite vincular ou desvincular. A conta de responsável não existe sem ao menos um aluno vinculado.",
            },
        ],
    },
    {
        papel: "Nutricionista",
        titulo: "Nutrição",
        perguntas: [
            {
                pergunta: "Por que não consigo publicar o cardápio?",
                resposta:
                    "A publicação é bloqueada em três casos: CRN inválido no seu perfil, item incompatível sem confirmação registrada, ou período que se sobrepõe a outro cardápio já publicado da unidade.",
            },
            {
                pergunta: "O que é o cardápio adaptado por escola?",
                resposta:
                    "Ao criar um cardápio geral, o sistema gera uma versão para cada combinação de escola e conjunto de restrições da unidade. Se não encontrar substituto seguro dentro da tolerância, a versão fica pendente de revisão e não pode ser publicada nem servida.",
            },
            {
                pergunta: "Por que o adaptado ficou desatualizado?",
                resposta:
                    "Editar um cardápio geral marca as versões adaptadas dele como desatualizadas. Enquanto isso, a publicação do geral é bloqueada até que as versões sejam refeitas.",
            },
            {
                pergunta: "Por que não consigo registrar ocorrência?",
                resposta:
                    "A ocorrência é registrada pela merendeira, pelo responsável ou pelo administrador da escola. A nutrição acompanha e lê as ocorrências da unidade, mas não abre nem muda status.",
            },
            {
                pergunta: "Por que não consigo abrir uma solicitação?",
                resposta:
                    "A nutrição não abre solicitação: ela decide as que chegam. A aprovação ou recusa pode ser feita direto da solicitação aberta, e o registro de análise entra no histórico junto com a decisão.",
            },
            {
                pergunta: "Como funciona a geração automática?",
                resposta:
                    "Os parâmetros ficam por unidade, na tela de parâmetros de geração. A geração usa você, nutricionista da sessão, como autora e cria o geral com as versões adaptadas na mesma operação.",
            },
        ],
    },
    {
        papel: "Merendeira",
        titulo: "Merenda",
        perguntas: [
            {
                pergunta: "Por que o formulário não pede a escola?",
                resposta:
                    "A escola vem da sua sessão. Você sempre trabalha no escopo da sua escola, e a unidade é derivada dela.",
            },
            {
                pergunta: "Até quando posso cancelar uma ocorrência?",
                resposta:
                    "Somente o autor pode cancelar, e apenas no mesmo dia do registro (considerando o horário de Brasília). Depois disso, a ocorrência segue o fluxo normal de análise.",
            },
            {
                pergunta: "Por que não consigo mudar o status da ocorrência?",
                resposta:
                    "A mudança de status é do administrador da escola. Para a merenda, a ocorrência é registro e acompanhamento.",
            },
            {
                pergunta: "Onde vejo o cardápio do dia?",
                resposta:
                    "Na aba de cardápio. O painel mostra o cardápio geral publicado da sua unidade na data e a quantidade de alunos com restrição em cada item.",
            },
        ],
    },
    {
        papel: "AdminEscola",
        titulo: "Escola",
        perguntas: [
            {
                pergunta: "Quem muda o status de uma ocorrência?",
                resposta:
                    "O administrador da escola. O fluxo é: aberta, em análise, resolvida e arquivada, e é possível retornar de em análise para aberta. A ocorrência não é apagada; encerrar significa arquivar.",
            },
            {
                pergunta: "Como respondo um feedback recebido?",
                resposta:
                    "Na tela de feedbacks, pela ação de responder. Quem encerra o feedback é o autor que o enviou.",
            },
            {
                pergunta: "Posso criar ou excluir usuários?",
                resposta:
                    "Não. Você consulta os usuários da própria escola, mas criar, editar e desativar contas é da secretaria da unidade.",
            },
        ],
    },
    {
        papel: "Responsavel",
        titulo: "Responsáveis",
        perguntas: [
            {
                pergunta: "Não vejo meu filho no portal.",
                resposta:
                    "O portal mostra apenas os alunos vinculados à sua conta. Se faltar algum, peça à secretaria para vincular. Consultas fora do seu vínculo não são respondidas.",
            },
            {
                pergunta: "Como incluo uma alergia ou restrição?",
                resposta:
                    "Somente por solicitação, na área de pais. O cadastro do aluno só muda depois da aprovação, e o pedido permite informar se houve reação anafilática.",
            },
            {
                pergunta: "O pedido de alergia foi recusado por já existir outro.",
                resposta:
                    "O sistema não aceita dois pedidos abertos para o mesmo aluno e a mesma restrição. Ele devolve o pedido existente para que você acompanhe a decisão em vez de abrir outro.",
            },
            {
                pergunta: "O cardápio do meu filho aparece indisponível.",
                resposta:
                    "O aluno com restrição recebe a versão adaptada publicada para a escola e o conjunto de restrições dele. Se essa versão não existir ou não estiver publicada, o cardápio fica indisponível: o geral não é usado como substituto.",
            },
            {
                pergunta: "Posso corrigir os dados cadastrais do meu filho?",
                resposta:
                    "Pelo pedido de alteração de dados cadastrais. A mudança é aplicada pela secretaria após aprovação, e o pedido registra o valor atual e o valor solicitado.",
            },
        ],
    },
];

export function Suporte() {
    const { userLogin } = useAuth();
    const navigate = useNavigate();
    const papelAtual = userLogin?.papel ?? null;

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
                <div className="flex items-center gap-3">
                    <LifeBuoy size={36} className="text-roxo" />
                    <div>
                        <h1 className="text-3xl font-bold text-gray-800">Suporte</h1>
                        <p className="text-sm text-gray-600">
                            Respostas para as dúvidas mais comuns de cada perfil de acesso.
                        </p>
                    </div>
                </div>

                <section
                    aria-labelledby="faq-titulo"
                    className="mt-6 rounded-3xl bg-branco p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]"
                >
                    <h2 id="faq-titulo" className="text-2xl font-bold text-gray-800">
                        Perguntas frequentes
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        O grupo do seu perfil já aparece aberto. Clique nos demais para ver as
                        respostas.
                    </p>

                    <div className="mt-4 space-y-3">
                        {GRUPOS.map((grupo) => {
                            const abertoPorPadrao =
                                grupo.papel === "Geral" || grupo.papel === papelAtual;
                            const ehMeuPerfil = grupo.papel === papelAtual;

                            return (
                                <details
                                    key={grupo.papel}
                                    open={abertoPorPadrao}
                                    className="rounded-2xl border border-gray-200 bg-white px-4 py-3"
                                >
                                    <summary className="cursor-pointer text-lg font-semibold text-gray-800">
                                        {grupo.titulo}
                                        {ehMeuPerfil && (
                                            <span className="ml-2 rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                                                seu perfil
                                            </span>
                                        )}
                                    </summary>

                                    <dl className="mt-3 space-y-4">
                                        {grupo.perguntas.map((item) => (
                                            <div key={item.pergunta}>
                                                <dt className="text-sm font-semibold text-gray-800">
                                                    {item.pergunta}
                                                </dt>
                                                <dd className="mt-1 text-sm text-gray-600">
                                                    {item.resposta}
                                                </dd>
                                            </div>
                                        ))}
                                    </dl>
                                </details>
                            );
                        })}
                    </div>
                </section>

                <section
                    aria-labelledby="contato-titulo"
                    className="mt-6 rounded-3xl bg-branco p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]"
                >
                    <h2 id="contato-titulo" className="text-2xl font-bold text-gray-800">
                        Atendimento
                    </h2>

                    {/*
                     * TODO(conteudo): o canal oficial de atendimento ainda não foi definido.
                     * Substituir os três valores abaixo por e-mail, telefone e horário reais
                     * quando a instituição informar. Não inventar dados de contato.
                     */}
                    <div className="mt-3 space-y-2 text-sm text-gray-700">
                        <p className="flex items-center gap-2">
                            <Mail size={18} className="text-roxo" />
                            <span className="font-semibold">E-mail:</span>
                            <span className="text-gray-500">não configurado</span>
                        </p>
                        <p className="flex items-center gap-2">
                            <Phone size={18} className="text-roxo" />
                            <span className="font-semibold">Telefone:</span>
                            <span className="text-gray-500">não configurado</span>
                        </p>
                        <p className="flex items-center gap-2">
                            <Clock size={18} className="text-roxo" />
                            <span className="font-semibold">Horário:</span>
                            <span className="text-gray-500">não configurado</span>
                        </p>
                    </div>

                    <div className="mt-4 rounded-2xl border border-gray-200 bg-[#FAF9F5] px-4 py-3 text-sm text-gray-700">
                        <p className="font-semibold">Enquanto o canal não estiver disponível</p>
                        <p className="mt-1">
                            Use os canais dentro do próprio sistema: abra uma{" "}
                            <Link to="/solicitacoes" className="font-semibold text-roxo underline">
                                solicitação
                            </Link>{" "}
                            para pedidos e correções, ou envie um{" "}
                            <Link to="/feedbacks" className="font-semibold text-roxo underline">
                                feedback
                            </Link>{" "}
                            para elogios, dúvidas e problemas de uso. Ambos ficam registrados com
                            autor e data.
                        </p>
                    </div>
                </section>

                <section
                    aria-labelledby="links-titulo"
                    className="mt-6 rounded-3xl bg-branco p-6 shadow-[0_8px_30px_rgba(11,102,25,0.08)]"
                >
                    <h2 id="links-titulo" className="text-2xl font-bold text-gray-800">
                        Documentos e canais
                    </h2>

                    <ul className="mt-3 space-y-2 text-sm">
                        <li>
                            <Link
                                to="/termosdeuso"
                                className="flex items-center gap-2 font-semibold text-roxo underline"
                            >
                                <FileText size={18} />
                                Termos de uso
                            </Link>
                        </li>
                        <li>
                            <Link
                                to="/politicadeprivacidade"
                                className="flex items-center gap-2 font-semibold text-roxo underline"
                            >
                                <ShieldCheck size={18} />
                                Política de privacidade
                            </Link>
                        </li>
                    </ul>
                </section>
            </main>

            <Footer />
        </div>
    );
}
