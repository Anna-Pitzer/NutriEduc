/** Corpo de erro da API: mensagem e detalhes opcionais (ex.: diagnóstico da geração). */
export interface FalhaDaApi {
    mensagem: string;
    detalhes?: unknown;
}

/**
 * Extrai mensagem e detalhes do corpo `{ erro, detalhes }` definido em `arch.md` §6.
 * Respostas sem JSON válido caem na mensagem padrão do chamador.
 */
export async function extrairFalha(resposta: Response, padrao: string): Promise<FalhaDaApi> {
    const corpo = await resposta.json().catch(() => null);

    if (corpo && typeof corpo === "object" && "erro" in corpo) {
        const mensagem = (corpo as { erro?: unknown }).erro;

        if (typeof mensagem === "string" && mensagem.length > 0) {
            return { mensagem, detalhes: (corpo as { detalhes?: unknown }).detalhes };
        }
    }

    return { mensagem: padrao };
}

/** Extrai apenas a mensagem do corpo `{ erro }`; preserva o contrato dos demais serviços. */
export async function extrairErro(resposta: Response, padrao: string): Promise<string> {
    return (await extrairFalha(resposta, padrao)).mensagem;
}

/** Erro de API que preserva o status HTTP para a UI decidir (ex.: 401 → login). */
export class ErroRequisicao extends Error {
    readonly status: number;
    readonly detalhes?: unknown;

    constructor(mensagem: string, status: number, detalhes?: unknown) {
        super(mensagem);
        this.name = "ErroRequisicao";
        this.status = status;
        this.detalhes = detalhes;
    }
}

export function naoAutorizado(erro: unknown): boolean {
    return erro instanceof ErroRequisicao && erro.status === 401;
}
