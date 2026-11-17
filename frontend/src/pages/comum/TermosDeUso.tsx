import { Link, useNavigate } from "react-router-dom";
import {
    Ban,
    ChevronLeft,
    Clock,
    Download,
    FileText,
    RefreshCw,
    Scale,
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
    icone: typeof FileText;
    paragrafos: string[];
};

/**
 * As regras abaixo refletem o que o sistema realmente bloqueia: publicação de cardápio
 * exige CRN, alimento incompatível exige confirmação humana, restrição do aluno só muda
 * após aprovação e exportação de relatório é restrita por perfil. Mudança em qualquer
 * uma dessas regras exige revisão deste texto.
 */
const SECOES: Secao[] = [
    {
        id: "objeto",
        titulo: "O que estes termos cobrem",
        icone: FileText,
        paragrafos: [
            "O NutriEduc é um sistema de gestão de alimentação escolar: mantém cadastro de alunos e suas restrições alimentares, monta e publica cardápios, registra ocorrências, conduz solicitações e gera relatórios.",
            "Ao usar o sistema, você concorda com estas regras. Se não concordar, não deve usar a sua conta e deve solicitar o encerramento dela à administração da instituição.",
            "O sistema é uma ferramenta de apoio à gestão. Ele não substitui avaliação nutricional, médica ou pedagógica feita por profissional habilitado, nem dispensa a conferência humana antes de servir uma refeição.",
        ],
    },
    {
        id: "conta",
        titulo: "Conta de acesso",
        icone: UserCheck,
        paragrafos: [
            "O autocadastro cria uma conta sem perfil de acesso e registra um pedido de aprovação. A conta só passa a autenticar depois que a secretaria da unidade aprova o pedido e define o papel e o escopo.",
            "A conta é pessoal e intransferível. Não compartilhe sua senha nem deixe a sessão aberta em equipamento de uso coletivo. Toda ação registrada no sistema fica associada ao titular da conta que estava autenticada.",
            "Você é responsável pela veracidade dos dados que informa no cadastro e no perfil. Nome, e-mail e telefone desatualizados podem impedir o contato da instituição com você.",
            "A troca de senha pelo próprio usuário ainda não está disponível. Para alterar a senha, procure a administração da instituição.",
        ],
    },
    {
        id: "perfis",
        titulo: "O que cada perfil pode fazer",
        icone: Users,
        paragrafos: [
            "Secretaria: administra contas, alunos, escolas e unidades da própria unidade e aprova pedidos de acesso.",
            "Nutrição: mantém catálogo de alimentos e restrições, monta, adapta e publica cardápios, gera cardápio automático e decide solicitações. Não registra ocorrência e não abre solicitação.",
            "Merenda: consulta o cardápio da própria escola, registra ocorrências e abre solicitações. Não publica cardápio e não altera status de ocorrência.",
            "Escola: acompanha ocorrências e feedbacks da própria escola, respondendo feedback.",
            "Responsável: consulta somente os filhos vinculados à sua conta, envia feedback e pede alterações por solicitação.",
            "O que cada perfil pode fazer é definido pela instituição e aplicado pelo servidor. Se uma tela permitir uma ação que o seu perfil não autoriza, a operação ainda será recusada.",
        ],
    },
    {
        id: "uso-aceitavel",
        titulo: "Uso aceitável",
        icone: Ban,
        paragrafos: [
            "É vedado tentar acessar dados de alunos, escolas ou unidades fora do seu escopo, inclusive por alteração manual de endereço, parâmetro ou identificador na tela.",
            "É vedado usar a exportação de relatórios para finalidade diferente da gestão da alimentação escolar da instituição, ou repassar o arquivo a terceiros sem autorização.",
            "É vedado inserir dados de pessoas que não tenham relação com a escola, ou informação de saúde de aluno sem que ela tenha sido informada pelo responsável ou apurada pela equipe responsável.",
            "É vedado usar o sistema para constranger, discriminar ou expor aluno em razão de sua restrição alimentar. A restrição existe para proteger a saúde, não para justificar tratamento diferenciado.",
        ],
    },
    {
        id: "cardapio",
        titulo: "Regras de cardápio e substituição",
        icone: ShieldCheck,
        paragrafos: [
            "A publicação de cardápio exige nutricionista com CRN válido e é bloqueada quando existe item incompatível com as restrições dos alunos sem confirmação registrada, período sobreposto a outro publicado ou versão adaptada pendente de revisão.",
            "Quando o sistema aponta incompatibilidade com substituto disponível, a confirmação é uma decisão humana do nutricionista e fica registrada com autor e data. Quando não há substituto seguro, a versão adaptada fica pendente e não pode ser publicada nem servida.",
            "O cardápio do aluno com restrição é a versão adaptada publicada para a escola e o conjunto de restrições dele. Se essa versão não existir, o cardápio aparece como indisponível e o cardápio geral não é usado como substituto.",
        ],
    },
    {
        id: "disponibilidade",
        titulo: "Disponibilidade e suporte",
        icone: Clock,
        paragrafos: [
            "O sistema pode passar por manutenção, atualização ou indisponibilidade técnica. Não há garantia de funcionamento ininterrupto nem de prazo de restabelecimento definido nestes termos.",
            "O atendimento é feito pelos canais da instituição descritos na página de suporte. Dentro do sistema, pedidos e problemas podem ser registrados pela área de solicitações e pela área de feedback, ambos com autor e data.",
            "Relate imediatamente qualquer acesso indevido, credencial exposta ou dado incorreto que você identificar. Restrição alimentar registrada errada é risco direto à saúde do aluno.",
        ],
    },
    {
        id: "suspensao",
        titulo: "Suspensão e encerramento",
        icone: Scale,
        paragrafos: [
            "A instituição pode desativar uma conta, escola ou unidade. A desativação encerra o acesso na requisição seguinte e preserva o histórico do que já foi feito.",
            "A exclusão de um registro é bloqueada quando existe vínculo que depende dele, como escola com usuários ou conta de responsável com alunos vinculados. Nesse caso, o vínculo deve ser resolvido antes.",
            "O uso indevido do sistema, a tentativa de acesso fora do escopo ou a exposição de dados de aluno podem levar à suspensão da conta e às medidas administrativas cabíveis.",
        ],
    },
    {
        id: "dados",
        titulo: "Dados pessoais e conteúdo",
        icone: Download,
        paragrafos: [
            "O tratamento de dados pessoais, incluindo dados de saúde do aluno, é descrito na política de privacidade. Os dados pertencem à instituição responsável pelo cadastro.",
            "Os conteúdos do sistema, como o catálogo de alimentos e os cardápios, são de uso interno da instituição. A exportação de relatórios não transfere propriedade sobre os dados.",
        ],
    },
    {
        id: "alteracoes",
        titulo: "Alterações destes termos",
        icone: RefreshCw,
        paragrafos: [
            "Estes termos podem ser atualizados. A versão e a data de atualização aparecem no topo da página, e a continuidade do uso após uma atualização representa concordância com o texto vigente.",
            "Se uma alteração reduzir direitos ou mudar uma regra de uso relevante, a instituição deve comunicá-la pelos canais oficiais.",
        ],
    },
];

export function TermosDeUso() {
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
                    <FileText size={36} className="mt-1 text-roxo" />
                    <div>
                        <h1 className="text-3xl font-bold text-gray-800">Termos de uso</h1>
                        <p className="text-sm text-gray-600">
                            Regras de uso do NutriEduc para todos os perfis de acesso.
                        </p>
                        <p className="mt-1 flex items-center gap-2 text-xs text-gray-500">
                            <RefreshCw size={14} />
                            Versão {VERSAO.numero} — atualizada em {VERSAO.atualizacao}
                        </p>
                    </div>
                </div>

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

                                {secao.id === "dados" && (
                                    <p className="mt-3 text-sm text-gray-700">
                                        Consulte a{" "}
                                        <Link
                                            to="/politicadeprivacidade"
                                            className="font-semibold text-roxo underline"
                                        >
                                            política de privacidade
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

                {/*
                 * TODO(conteudo): completar com os dados oficiais da instituição quando
                 * informados — razão social, CNPJ e comarca/foro de eleição. Não inventar.
                 */}
                <section className="mt-6 rounded-3xl border border-amber-200 bg-amber-50 p-6 text-sm text-amber-900">
                    <p className="font-semibold">Sobre a redação deste texto</p>
                    <p className="mt-1">
                        O conteúdo acima descreve o comportamento técnico atual do sistema. A
                        redação jurídica oficial, a identificação da instituição e a indicação de
                        foro devem ser revisadas antes da publicação como termo oficial.(APENAS
                        PARA SIMULAÇÃO)
                    </p>
                </section>
            </main>

            <Footer />
        </div>
    );
}
