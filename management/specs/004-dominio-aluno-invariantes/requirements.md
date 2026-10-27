---
name: dominio-aluno-invariantes
status: created
references: V1, ADR-004, L1
---

# Requirements — Invariantes do domínio Aluno

## Objetivo

Eliminar a entidade anêmica: hoje o construtor de `Aluno` atribui dez campos sem
nenhuma validação e a classe não possui um único método de domínio. Qualquer caminho
de escrita produz estado inválido.

## Escopo

- Domínio: construtor validante e `AtualizarDados` em
  `backend/Domain/Entities/Aluno.cs`.
- Aplicação: `AlunoService` deixa de repassar dados sem tratamento.
- Fora de escopo: coleção de restrições (Spec 003), endpoint de atualização
  (Spec 001), autorização (Spec 006).

## Requisitos funcionais

- RF1: o construtor valida todos os campos e só então atribui; nenhuma propriedade é
  alterada se qualquer validação falhar.
- RF2: `AtualizarDados` recebe o mesmo conjunto de campos do construtor — inclusive as
  restrições alimentares —, aplica as mesmas invariantes e mantém a sequência
  validar → atribuir. A coleção de restrições é substituída pelo conjunto informado;
  falha em qualquer validação não altera nenhuma propriedade nem a coleção.
- RF3: invariantes de `Nome`, `Escola`, `Serie`, `NomeResponsavel`: obrigatórios, com
  trim, comprimento mínimo 2 e máximo 100 (escola até 150, série até 50).
- RF4: `Nascimento` deve ser `DateOnly` entre 1900-01-01 e a data atual.
- RF5: `Telefone` e `TelefoneResponsavel` são opcionais; quando informados, aceitam
  apenas dígitos e exigem 10 ou 11 dígitos, armazenados sem máscara.
- RF6: `Observacao` é opcional, com trim e máximo de 500 caracteres.
- RF7: `Anafilaxia` é booleano e não sofre validação adicional.
- RF8: toda violação lança `ArgumentException` com mensagem específica do campo.

## Requisitos não funcionais

- RNF1: mensagens de erro em português, distintas por campo.
- RNF2: nenhuma dependência nova; usar `DateTime.Today` apenas para o limite superior.
- RNF3: o construtor sem parâmetros (usado pelo EF Core) permanece privado e sem
  validação.

## Restrições

- Não alterar o schema do banco.
- Não alterar o formato de `DadosAlunoDTO`.
- Não duplicar regra no `AlunoService`.

## Dependências

- Nenhuma. É pré-requisito da Spec 001 e da Spec 003.

## Casos de borda

- Nome com apenas espaços → rejeitado após trim.
- Nascimento futuro → rejeitado.
- Telefone com máscara `(11) 91234-5678` → aceito e normalizado para `11912345678`.
- Telefone com 9 ou 12 dígitos → rejeitado.
- Nome com 101 caracteres → rejeitado.
- `AtualizarDados` inválido → nenhuma propriedade e nenhuma restrição alteradas
  (verificável comparando o estado antes e depois).
- `AtualizarDados` válido com nova lista de restrições → coleção antiga substituída.

## Critérios de aceitação

- [ ] Construtor rejeita cada campo obrigatório vazio com mensagem específica.
- [ ] Nascimento futuro e nascimento anterior a 1900 são rejeitados.
- [ ] Telefone com 9 e com 12 dígitos são rejeitados; com 10 e 11 são aceitos.
- [ ] Telefone com máscara é normalizado para dígitos.
- [ ] `AtualizarDados` com payload inválido não altera nenhuma propriedade nem a
  coleção de restrições.
- [ ] `AtualizarDados` válido altera todas as propriedades informadas.
- [ ] `AtualizarDados` substitui a coleção de restrições pelo conjunto informado.
- [ ] `AlunoService` não contém `if` de validação de campo.
- [ ] Testes unitários cobrem todos os casos desta seção.

## Validação

Testes unitários de domínio (`dotnet test`) e `dotnet build backend/backend.csproj`.
