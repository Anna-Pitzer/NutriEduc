/**
 * Extrai a mensagem do corpo `{ erro }` definido em `arch.md` §6.
 * Respostas sem JSON válido caem na mensagem padrão do chamador.
 */
export async function extrairErro(resposta: Response, padrao: string): Promise<string> {
    const corpo = await resposta.json().catch(() => null);

    if (corpo && typeof corpo === "object" && "erro" in corpo) {
        const mensagem = (corpo as { erro?: unknown }).erro;

        if (typeof mensagem === "string" && mensagem.length > 0) {
            return mensagem;
        }
    }

    return padrao;
}
