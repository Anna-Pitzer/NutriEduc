import { CardapiosPublicados } from "../publico/CardapiosPublicados";

/**
 * Área dos Pais / Responsáveis (Spec 012, RF13): consulta aos cardápios publicados. O
 * responsável escolhe a unidade porque o vínculo aluno→unidade ainda não é exposto na
 * sessão; consultar o cardápio por aluno pertence à Spec 025.
 */
export function HomePais() {
  return <CardapiosPublicados titulo="Cardápios publicados" />;
}
