import { useAuth } from "../../context/authContext";
import { CardapiosPublicados } from "../publico/CardapiosPublicados";
import { AlertaAlunosIndisponiveis } from "./AlertaAlunosIndisponiveis";

/**
 * Área das Merendeiras (Spec 012, RF13; Spec 029, RF15): cardápios publicados da própria
 * unidade e alerta das versões adaptadas que não podem ser servidas. As demais funções
 * planejadas (ocorrências, solicitações) pertencem às specs 016 e 017.
 */
export function HomeMerendeiras() {
  const { userLogin } = useAuth();
  const unidadeId = userLogin?.unidadeId ?? null;

  return (
    <>
      <AlertaAlunosIndisponiveis unidadeId={unidadeId} />

      <CardapiosPublicados
        unidadeFixa={unidadeId}
        titulo="Cardápios da unidade"
      />
    </>
  );
}
