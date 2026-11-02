import { useEffect, useState } from "react";
import { useAuth } from "../../context/authContext";
import { naoAutorizado } from "../../services/erroApi";
import {
    definirUnidadeAtiva,
    excluirUnidade,
    listarUnidades,
    renomearUnidade,
    type UnidadeResposta,
} from "../../services/EscolaApi";
import { TabelaDeNomes, type ItemDeNome } from "../../components/TabelaDeNomes";
import { LayoutSecretaria } from "./layoutSecretaria";

/**
 * Unidades da rede (secretaria). A unidade **nasce no bootstrap do primeiro
 * administrador**, digitada na página `/bootstrap`; por isso não há botão de criar aqui.
 */
export function Unidades() {
    const { logout } = useAuth();
    const [unidades, setUnidades] = useState<UnidadeResposta[]>([]);
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");

    useEffect(() => {
        let ativo = true;

        listarUnidades()
            .then((dados) => {
                if (ativo) setUnidades(dados);
            })
            .catch((falha) => {
                if (naoAutorizado(falha)) {
                    void logout();
                    return;
                }

                console.error("Erro ao carregar unidades:", falha);

                if (ativo) {
                    setErro("Não foi possível carregar as unidades.");
                }
            })
            .finally(() => {
                if (ativo) {
                    setCarregando(false);
                }
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

    async function aoRenomear(id: number, nome: string) {
        try {
            const atualizada = await renomearUnidade(id, nome);
            setUnidades((atuais) => atuais.map((item) => (item.id === id ? atualizada : item)));
        } catch (falha) {
            return tratar(falha);
        }
    }

    async function aoAlternarAtiva(item: ItemDeNome) {
        try {
            const atualizada = await definirUnidadeAtiva(item.id, !item.ativa);
            setUnidades((atuais) => atuais.map((unidade) => (unidade.id === item.id ? atualizada : unidade)));
        } catch (falha) {
            return tratar(falha);
        }
    }

    async function aoExcluir(item: ItemDeNome) {
        try {
            await excluirUnidade(item.id);
            setUnidades((atuais) => atuais.filter((unidade) => unidade.id !== item.id));
        } catch (falha) {
            return tratar(falha);
        }
    }

    return (
        <LayoutSecretaria>
            <h1 className="mb-4 text-2xl font-bold text-vermelho">Unidades</h1>

            {erro && (
                <p role="alert" className="mb-4 rounded-xl bg-red-50 px-4 py-2 text-sm text-red-700">
                    {erro}
                </p>
            )}

            <TabelaDeNomes
                titulo="Unidades da rede"
                descricao="Criada no cadastro do primeiro administrador por token. Excluir só é permitido sem usuários, alunos ou tokens vinculados."
                rotuloVazio="Nenhuma unidade criada ainda. Ela nasce no primeiro cadastro por token."
                itens={unidades}
                carregando={carregando}
                aoRenomear={aoRenomear}
                aoAlternarAtiva={aoAlternarAtiva}
                aoExcluir={aoExcluir}
            />
        </LayoutSecretaria>
    );
}
