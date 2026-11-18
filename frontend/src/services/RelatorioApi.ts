import { extrairFalha, ErroRequisicao } from "./erroApi";

const ApiURL = "";

/** Catálogo fechado da Spec 019. */
export type TipoDeRelatorio =
    | "Cardapios"
    | "AlunosRestricoes"
    | "Ocorrencias"
    | "SolicitacoesFeedbacks"
    | "IndicadoresAdministrativos";

export interface FiltroRelatorio {
    de?: string;
    ate?: string;
    unidadeId?: number;
    escolaId?: number;
}

export interface RelatorioPreview {
    tipo: string;
    titulo: string;
    colunas: string[];
    linhas: string[][];
    totalLinhas: number;
    rodape?: string | null;
}

export type FormatoDeExportacao = "xlsx" | "csv";

function consulta(filtro: FiltroRelatorio, formato?: FormatoDeExportacao): string {
    const parametros = new URLSearchParams();

    if (filtro.de) parametros.set("de", filtro.de);
    if (filtro.ate) parametros.set("ate", filtro.ate);
    if (filtro.unidadeId !== undefined) parametros.set("unidadeId", String(filtro.unidadeId));
    if (filtro.escolaId !== undefined) parametros.set("escolaId", String(filtro.escolaId));
    if (formato) parametros.set("formato", formato);

    const texto = parametros.toString();
    return texto ? `?${texto}` : "";
}

export async function previsualizarRelatorio(
    tipo: TipoDeRelatorio,
    filtro: FiltroRelatorio,
): Promise<RelatorioPreview> {
    const resposta = await fetch(`${ApiURL}/api/relatorios/${tipo}/preview${consulta(filtro)}`, {
        credentials: "include",
    });

    if (!resposta.ok) {
        const { mensagem, detalhes } = await extrairFalha(resposta, "Erro ao carregar o relatório");

        throw new ErroRequisicao(mensagem, resposta.status, detalhes);
    }

    return resposta.json();
}

export async function exportarRelatorio(
    tipo: TipoDeRelatorio,
    filtro: FiltroRelatorio,
    formato: FormatoDeExportacao = "xlsx",
): Promise<void> {
    const resposta = await fetch(
        `${ApiURL}/api/relatorios/${tipo}/exportar${consulta(filtro, formato)}`,
        {
            credentials: "include",
        },
    );

    if (!resposta.ok) {
        const { mensagem, detalhes } = await extrairFalha(resposta, "Erro ao exportar o relatório");

        throw new ErroRequisicao(mensagem, resposta.status, detalhes);
    }

    const arquivo = await resposta.blob();
    const disposicao = resposta.headers.get("Content-Disposition") ?? "";
    const nome = /filename=\"?([^\";]+)/.exec(disposicao)?.[1] ?? `relatorio-${tipo}.${formato}`;

    const url = URL.createObjectURL(arquivo);
    const link = document.createElement("a");

    link.href = url;
    link.download = nome;
    link.click();
    URL.revokeObjectURL(url);
}
