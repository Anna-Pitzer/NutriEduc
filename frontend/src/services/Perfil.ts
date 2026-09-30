const ApiURL = "";

export async function buscarPerfil(usuarioId: number){
    const resposta = await fetch(`/api/usuario/perfil/${usuarioId}`,{
        credentials: "include",
    });

    if (!resposta.ok) throw new Error("Erro ao buscar perfil");

    return resposta.json();
}

export async function buscarEstatisticas(usuarioId: number) {
    const resposta = await fetch(`${ApiURL}/usuarios/${usuarioId}/estatisticas`);

    if (!resposta.ok) throw new Error("Erro ao buscar estatísticas");

    return resposta.json();
}

export async function atualizarPerfil(
    usuarioId: number,
    dados: {
        nome: string;
        email: string;
        telefone: string;
        foto: string;
    }
) {
    const resposta = await fetch(
        `/api/usuario/perfil/${usuarioId}`,
        {
            method: "PUT",
            credentials: "include",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                nome: dados.nome,
                email: dados.email,
                telefone: dados.telefone,
                foto: dados.foto,
            }),
        }
    );

    if (!resposta.ok) {
        const mensagem = await resposta.text();
        throw new Error(mensagem || "Erro ao atualizar o perfil.");
    }

    return resposta.json();
}