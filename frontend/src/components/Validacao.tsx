export function validarNome(valor: string): string | null {
  if (valor.length < 2) {
    return "Digite um nome válido.";
  }

  return null;
}

export function validarEmail(valor: string): string | null {
  if (
    valor.length < 5 ||
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(valor)
  ) {
    return "Digite um e-mail válido.";
  }

  return null;
}

export function validarInstituicao(valor: string): string | null {
  if (valor.length < 2) {
    return "Digite uma instituição válida.";
  }

  return null;
}

export function validarTelefone(valor: string): string | null {
  if (valor.length !== 10 && valor.length !== 11) {
    return "Digite um telefone válido.";
  }

  return null;
}

export function validarCategoria(valor: string): string | null {
  const categoriasValidas = [
    "opcao1",
    "opcao2",
    "opcao3",
  ];

  if (!categoriasValidas.includes(valor)) {
    return "Selecione uma categoria válida.";
  }

  return null;
}

export function validarSenha(valor: string): string | null {
  if (valor.length < 8) {
    return "A senha deve possuir pelo menos 8 caracteres.";
  }

  return null;
}

export function validarConfirmacaoSenha(
  senha: string,
  confirmacao: string,
): string | null {
  if (!confirmacao) {
    return "Confirme sua nova senha.";
  }

  if (senha !== confirmacao) {
    return "A confirmação da senha não corresponde.";
  }

  return null;
}

export function validarAniversario(aniversario: string): string | null {
  if (!aniversario) {
    return "Digite sua data de nascimento.";
  }

  return null;
}