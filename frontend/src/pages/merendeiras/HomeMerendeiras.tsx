import { useAuth } from "../../context/authContext";
import { CardapiosPublicados } from "../publico/CardapiosPublicados";

/**
 * Área das Merendeiras (Spec 012, RF13): cardápios publicados da própria unidade, em
 * modo leitura. As demais funções planejadas (ocorrências, solicitações) pertencem às
 * specs 016 e 017.
 */
export function HomeMerendeiras() {
  const { userLogin } = useAuth();

  return (
    <CardapiosPublicados
      unidadeFixa={userLogin?.unidadeId ?? null}
      titulo="Cardápios da unidade"
    />
  );
}
