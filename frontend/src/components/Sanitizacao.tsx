
export function sanitizarNome(valor: string): string {
  return valor
    .replace(/[^A-Za-zÀ-ÿ\s'-]/g, "")
    .replace(/\s+/g, " ")
    .trim()
    .slice(0, 100);
}

export function sanitizarEmail(valor: string): string {
  return valor
    .trim()
    .toLowerCase()
    .slice(0, 150);
}

export function sanitizarInstituicao(valor: string): string {
  return valor
    .replace(/\+/g, " ")
    .trim()
    .slice(0, 150);
}

export function sanitizarTelefone(valor: string): string {
  return valor
    .replace(/\D/g, "")
    .slice(0, 11);
}

export function formatarTelefone(telefone: string) {
  const numeros = telefone.replace(/\D/g, "");

  if (numeros.length <= 10) {
    return numeros.replace(
      /^(\d{2})(\d{4})(\d{0,4})$/,
      "($1) $2-$3"
    );
  }

  return numeros.replace(
    /^(\d{2})(\d{5})(\d{0,4})$/,
    "($1) $2-$3"
  );
};

export function sanitizarAniversario(valor: string): string {
  return valor.trim();
}


