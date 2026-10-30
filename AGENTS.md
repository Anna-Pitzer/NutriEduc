# Project Agent Rules

Add project-specific rules here. The DsCode canonical contract is loaded automatically and is not copied into this file.

## Permissão antes de agir

Nunca altere o projeto sem autorização explícita do usuário.

- **Exige permissão:** criar, editar ou apagar arquivos; `npm install`; `dotnet build`/`restore`;
  migrations; `git add`/`commit`/`reset`/`checkout`; instalar ferramentas; qualquer comando
  que escreva no disco.
- **Não exige permissão:** leitura de arquivos, `git status`/`diff`/`log`, listar diretórios,
  buscar padrões e testes que apenas reportam resultado.
- Em tarefas de vários passos, apresente o plano e aguarde o "pode ir" antes do primeiro passo
  de escrita; repita a confirmação antes de cada passo que altere estado.
