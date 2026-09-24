const ApiURL = "";

export async function buscarPerfil(usuarioId: number){
    const resposta = await fetch(`${ApiURL}/usuarios/${usuarioId}`);

    if (!resposta.ok) throw new Error("Erro ao buscar perfil");

    return resposta.json();
}

export async function buscarEstatisticas(usuarioId: number) {
    const resposta = await fetch(`${ApiURL}/usuarios/${usuarioId}/estatisticas`);

    if (!resposta.ok) throw new Error("Erro ao buscar estatísticas");

    return resposta.json();
}

export async function atualizarPerfil(usuarioId: number, 
    dados: {
        nome: string;
        email: string;
        telefone: string;
        foto: string;
    }
) {
    const resposta = await fetch(`${ApiURL}/usuarios/${usuarioId}`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dados)
    })

    if (!resposta.ok) throw new Error("Erro ao atualizar o perfil");

    return resposta.json();
}