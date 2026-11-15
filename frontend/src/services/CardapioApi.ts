import { extrairErro, ErroRequisicao } from "./erroApi";

const ApiURL = "";

export type SituacaoCardapio = "Rascunho" | "Publicado" | "Cancelado" | "Substituido";

/** Versão geral (da unidade) ou adaptada (por escola e conjunto de restrições). */
export type TipoCardapio = "Geral" | "Adaptado";

/** Estado da adaptação de uma versão (Spec 029, RF7). */
export type StatusAdaptacao = "Atualizado" | "Desatualizado" | "PendenteDeRevisao";

/** Período de vigência (Spec 012, RF1). Datas no formato ISO `yyyy-mm-dd`. */
export interface PeriodoCardapio {
    inicio: string;
    fim: string;
}

/** Composição nutricional devolvida pelo backend; o cliente nunca recalcula (RNF3). */
export interface ValoresItem {
    caloriasKcal: number;
    proteinasG: number;
    carboidratosG: number;
    gordurasG: number;
    fibrasG: number;
    sodioMg: number;
}

export interface ItemCardapio {
    alimentoId: number;
    alimento: string;
    gramas: number;
    valores: ValoresItem;
}

export interface RefeicaoCardapio {
    nome: string;
    ordem: number;
    itens: ItemCardapio[];
    total: ValoresItem;
}

/** Nutricionista que elaborou o cardápio (snapshot da autoria). */
export interface NutricionistaCardapio {
    id: number;
    nome: string | null;
    email: string | null;
    cpf: string | null;
    crn: string | null;
}

/** Cardápio completo, com refeições, itens e totais (RF11). */
export interface Cardapio {
    id: number;
    nome: string;
    unidadeId: number;
    situacao: SituacaoCardapio;
    periodo: PeriodoCardapio;
    cardapioDuplicadoDeId: number | null;
    /** Nulo em cardápios criados antes da regra de autoria. */
    nutricionista: NutricionistaCardapio | null;
    tipo: TipoCardapio;
    origemId: number | null;
    escolaId: number | null;
    conjuntoRestricoesChave: string | null;
    restricoes: RestricaoDaVersao[];
    statusAdaptacao: StatusAdaptacao | null;
    semRestricoes: boolean;
    substituicoes: SubstituicaoCardapio[];
    pendencias: PendenciaAdaptacao[];
    confirmacoes: ConfirmacaoIncompatibilidade[];
    refeicoes: RefeicaoCardapio[];
    total: ValoresItem;
}

/** Linha da listagem: sem itens, só metadados (RF13), com a contagem de incompatibilidade (Spec 014, RF9). */
export interface CardapioResumo {
    id: number;
    nome: string;
    unidadeId: number;
    situacao: SituacaoCardapio;
    periodo: PeriodoCardapio;
    tipo: TipoCardapio;
    origemId: number | null;
    escolaId: number | null;
    statusAdaptacao: StatusAdaptacao | null;
    totalRefeicoes: number;
    totalBloqueantes: number;
    totalAlertas: number;
}

export interface PaginaCardapios<T> {
    pagina: number;
    tamanho: number;
    total: number;
    itens: T[];
}

export interface FiltroCardapios {
    unidadeId?: number;
    data?: string;
    situacao?: SituacaoCardapio;
    pagina?: number;
    tamanho?: number;
}

export interface FiltroPublicados {
    unidadeId: number;
    data?: string;
    refeicao?: string;
    pagina?: number;
    tamanho?: number;
}

export interface ItemEntrada {
    alimentoId: number;
    gramas: number;
}

export interface RefeicaoEntrada {
    nome: string;
    ordem: number;
    itens: ItemEntrada[];
}

export interface CardapioEntrada {
    nome: string;
    unidadeId: number;
    periodo: PeriodoCardapio;
    refeicoes: RefeicaoEntrada[];
    /** Obrigatório na criação manual com CRN válido (Spec 029, RF9). */
    nutricionistaId?: number | null;
    /** Confirma alimento incompatível com substituto seguro (Spec 029, RF12). */
    confirmarIncompatibilidades?: boolean;
}

/** Restrição exibida no conjunto de uma versão adaptada (RF18). */
export interface RestricaoDaVersao {
    id: number;
    nome: string;
    categoria: string;
}

/** Substituição aplicada na versão adaptada (RF5, RF18). */
export interface SubstituicaoCardapio {
    refeicaoOrdem: number;
    refeicaoNome: string;
    alimentoOriginalId: number;
    alimentoOriginal: string;
    alimentoSubstitutoId: number;
    alimentoSubstituto: string;
    gramas: number;
    restricoesBloqueantes: string;
}

/** Motivo registrado que impede publicar ou servir a versão (RF8). */
export interface PendenciaAdaptacao {
    refeicaoOrdem: number;
    refeicaoNome: string;
    alimentoId: number | null;
    alimento: string | null;
    motivo: string;
}

/** Confirmação append-only de incompatibilidade mantida no geral (RF13). */
export interface ConfirmacaoIncompatibilidade {
    autor: string;
    criadoEm: string;
    alimentoId: number;
    alimento: string;
    escolas: string;
    conjuntos: string;
}

/** Linha do par geral↔adaptado (RF18). */
export interface CardapioAdaptadoResumo {
    id: number;
    nome: string;
    origemId: number;
    escolaId: number;
    escolaNome: string;
    conjuntoRestricoesChave: string;
    statusAdaptacao: StatusAdaptacao | null;
    situacao: SituacaoCardapio;
    totalSubstituicoes: number;
    totalPendencias: number;
    semRestricoes: boolean;
}

/** Aluno e restrições no painel da montagem manual (RF11). */
export interface AlunoNoContexto {
    alunoId: number;
    nome: string;
    restricoes: RestricaoDaVersao[];
    conjuntoRestricoesChave: string;
}

/** Escola com alunos e restrições (RF11). */
export interface EscolaNoContexto {
    escolaId: number;
    escolaNome: string;
    semRestricoes: boolean;
    alunos: AlunoNoContexto[];
}

/** Contexto de restrições por escola (RF11, RNF7). */
export interface ContextoDeRestricoes {
    unidadeId: number;
    escolas: EscolaNoContexto[];
}

/** Versão servida ao aluno ou o motivo de indisponibilidade (RF15). */
export interface CardapioServido {
    alunoId: number;
    alunoNome: string;
    escolaId: number | null;
    conjuntoRestricoesChave: string;
    disponivel: boolean;
    motivo: string | null;
    alerta: boolean;
    cardapio: Cardapio | null;
}

export interface DuplicarEntrada {
    nome?: string;
    periodo: PeriodoCardapio;
}

export type SeveridadeCompatibilidade = "Alerta" | "Bloqueante";

export interface AlunoAfetadoCompatibilidade {
    id: number;
    nome: string;
}

export interface AchadoCompatibilidade {
    refeicao: string;
    alimentoId: number;
    alimento: string;
    restricao: string;
    severidade: SeveridadeCompatibilidade;
    alunos: AlunoAfetadoCompatibilidade[];
}

/** Resposta da análise de compatibilidade (Spec 014, RF4). */
export interface Compatibilidade {
    compativel: boolean;
    totalAlunosAnalisados: number;
    bloqueantes: AchadoCompatibilidade[];
    alertas: AchadoCompatibilidade[];
    alimentosPermitidos: string[];
    alimentosNaoPermitidos: string[];
    avisos: string[];
}

async function falhar(resposta: Response, padrao: string): Promise<never> {
    throw new ErroRequisicao(await extrairErro(resposta, padrao), resposta.status);
}

function comoParametros(filtros: object): string {
    const parametros = new URLSearchParams();

    for (const [chave, valor] of Object.entries(filtros) as [
        string,
        string | number | undefined,
    ][]) {
        if (valor !== undefined && valor !== "") {
            parametros.set(chave, String(valor));
        }
    }

    const consulta = parametros.toString();

    return consulta ? `?${consulta}` : "";
}

/** Listagem interna, escopada por papel (RF8). */
export async function listarCardapios(
    filtros: FiltroCardapios = {},
): Promise<PaginaCardapios<CardapioResumo>> {
    const resposta = await fetch(`${ApiURL}/api/cardapios${comoParametros(filtros)}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os cardápios");

    return resposta.json();
}

/** Rota anônima de pais e merendeiras (RF9). */
export async function listarCardapiosPublicados(
    filtros: FiltroPublicados,
): Promise<PaginaCardapios<Cardapio>> {
    const resposta = await fetch(
        `${ApiURL}/api/cardapios/publicados${comoParametros(filtros)}`,
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar os cardápios publicados");

    return resposta.json();
}

export async function obterCardapio(id: number): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o cardápio");

    return resposta.json();
}

export async function criarCardapio(entrada: CardapioEntrada): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao criar o cardápio");

    return resposta.json();
}

export async function atualizarCardapio(
    id: number,
    entrada: CardapioEntrada,
): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao salvar o cardápio");

    return resposta.json();
}

export async function excluirCardapio(id: number): Promise<void> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}`, {
        method: "DELETE",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao excluir o cardápio");
}

export async function duplicarCardapio(
    id: number,
    entrada: DuplicarEntrada,
): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/duplicar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao duplicar o cardápio");

    return resposta.json();
}

/** Análise de compatibilidade do cardápio com as restrições dos alunos (Spec 014). */
export async function obterCompatibilidade(id: number): Promise<Compatibilidade> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/compatibilidade`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar a compatibilidade");

    return resposta.json();
}

// ---------- Publicação e histórico (Spec 015) ----------

export type EventoCardapio =
    | "Criado"
    | "Editado"
    | "Duplicado"
    | "RevisaoCriada"
    | "Publicado"
    | "Cancelado"
    | "Substituido";

export interface EventoHistoricoCardapio {
    evento: EventoCardapio;
    versao: number;
    autor: string;
    motivo: string | null;
    criadoEm: string;
}

export interface AlteracaoCardapio {
    id: number;
    cardapioId: number;
    cardapioNome: string;
    unidadeNome: string;
    evento: EventoCardapio;
    versao: number;
    autor: string;
    motivo: string | null;
    criadoEm: string;
}

export interface Pagina<T> {
    pagina: number;
    tamanho: number;
    total: number;
    itens: T[];
}

export async function publicarCardapio(id: number): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/publicar`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao publicar o cardápio");

    return resposta.json();
}

export async function cancelarCardapio(id: number, motivo: string): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/cancelar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ motivo }),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao cancelar o cardápio");

    return resposta.json();
}

export async function criarRevisaoCardapio(id: number): Promise<Cardapio> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/revisao`, {
        method: "POST",
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao criar a revisão do cardápio");

    return resposta.json();
}

export async function obterHistoricoCardapio(id: number): Promise<Pagina<EventoHistoricoCardapio>> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/historico`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o histórico do cardápio");

    return resposta.json();
}

// ---------- Geral e adaptado (Spec 029) ----------

/** Entrada da adaptação manual: escola e, opcionalmente, o conjunto exato (T6). */
export interface AdaptarCardapioEntrada {
    escolaId: number;
    /** Quando nulo, o backend adapta todos os conjuntos distintos existentes na escola. */
    restricaoIds?: number[] | null;
}

/** Alunos ativos e restrições por escola, para o painel da montagem manual (RF11). */
export async function obterContextoDeRestricoes(
    unidadeId: number,
    escolaId?: number,
): Promise<ContextoDeRestricoes> {
    const consulta = new URLSearchParams({ unidadeId: String(unidadeId) });

    if (escolaId !== undefined) {
        consulta.set("escolaId", String(escolaId));
    }

    const resposta = await fetch(`${ApiURL}/api/cardapios/contexto-restricoes?${consulta}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as restrições da unidade");

    return resposta.json();
}

/** Versões adaptadas de um geral (RF18). */
export async function listarAdaptados(id: number): Promise<CardapioAdaptadoResumo[]> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/adaptados`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as versões adaptadas");

    return resposta.json();
}

/** Adapta um geral para a escola; idempotente por (origem, escola, conjunto) (T6). */
export async function adaptarCardapio(
    id: number,
    entrada: AdaptarCardapioEntrada,
): Promise<CardapioAdaptadoResumo[]> {
    const resposta = await fetch(`${ApiURL}/api/cardapios/${id}/adaptar`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(entrada),
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao adaptar o cardápio");

    return resposta.json();
}

/** Versão servida ao aluno na data, ou `Indisponivel` (RF15). */
export async function obterCardapioDoAluno(
    alunoId: number,
    data?: string,
): Promise<CardapioServido> {
    const consulta = data ? `?data=${encodeURIComponent(data)}` : "";
    const resposta = await fetch(`${ApiURL}/api/aluno/${alunoId}/cardapio${consulta}`, {
        credentials: "include",
    });

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar o cardápio do aluno");

    return resposta.json();
}

export async function listarAlteracoesCardapios(
    desde: string,
): Promise<Pagina<AlteracaoCardapio>> {
    const resposta = await fetch(
        `${ApiURL}/api/cardapios/alteracoes?desde=${encodeURIComponent(desde)}`,
        { credentials: "include" },
    );

    if (!resposta.ok) return falhar(resposta, "Erro ao carregar as alterações");

    return resposta.json();
}
