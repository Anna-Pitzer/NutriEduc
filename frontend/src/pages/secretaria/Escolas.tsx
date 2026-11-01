import { useEffect, useState } from "react";
import { useAuth } from "../../context/authContext";
import { TabelaDeNomes, type ItemDeNome } from "../../components/TabelaDeNomes";
import { naoAutorizado } from "../../services/erroApi";
import {
    criarEscola,
    definirEscolaAtiva,
    excluirEscola,
    listarEscolas,
    renomearEscola,
    type EscolaResposta,
} from "../../services/EscolaApi";
import { LayoutSecretaria } from "./layoutSecretaria";

export function Escolas() {
    const { logout } = useAuth();
    const [escolas, setEscolas] = useState<EscolaResposta[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

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

                if (ativo) setErro("Não foi possível carregar as escolas.");
            })
            .finally(() => {
                if (ativo) setCarregando(false);
            });

        return () => {
            ativo = false;
        };
    }, [logout]);

    async function tratar(falha: unknown): Promise<never> {
        if (naoAutorizado(falha)) {
            void logout();
        }

        throw falha;
    }

    async function aoCriar(nome: string) {
        try {
            const criada = await criarEscola(nome);
            setEscolas((atuais) => [...atuais, criada]);
        } catch (falha) {
            return tratar(falha);
        }
    }

    async function aoRenomear(id: number, nome: string) {
        try {
            const atualizada = await renomearEscola(id, nome);
            setEscolas((atuais) => atuais.map((escola) => (escola.id === id ? atualizada : escola)));
        } catch (falha) {
            return tratar(falha);
        }
    }

    async function aoAlternarAtiva(item: ItemDeNome) {
        try {
            const atualizada = await definirEscolaAtiva(item.id, !item.ativa);
            setEscolas((atuais) => atuais.map((escola) => (escola.id === item.id ? atualizada : escola)));
        } catch (falha) {
            return tratar(falha);
        }
    }

    async function aoExcluir(item: ItemDeNome) {
        try {
            await excluirEscola(item.id);
            setEscolas((atuais) => atuais.filter((escola) => escola.id !== item.id));
        } catch (falha) {
            return tratar(falha);
        }
    }

    return (
        <LayoutSecretaria>
            <h1 className="mb-4 text-2xl font-bold text-vermelho">Escolas</h1>

            {erro && (
                <p role="alert" className="mb-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            <TabelaDeNomes
                titulo="Escolas da rede"
                descricao="Desativar mantém o cadastro; excluir remove de vez e só é permitido sem usuários vinculados."
                rotuloVazio="Nenhuma escola cadastrada ainda."
                itens={escolas}
                carregando={carregando}
                aoCriar={aoCriar}
                aoRenomear={aoRenomear}
                aoAlternarAtiva={aoAlternarAtiva}
                aoExcluir={aoExcluir}
            />
        </LayoutSecretaria>
    );
}
