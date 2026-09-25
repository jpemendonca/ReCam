# ADR 0014: Domínio rico e Result<T>

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

Erros esperados, como token expirado, nome inválido ou papel errado, fazem parte do fluxo normal.
Tratá-los como exceção mistura bug com regra de negócio e deixa o controle de fluxo escondido.

## Decisão

As regras moram nos métodos das entidades, que devolvem `Result` ou `Result<T>` com um
`DomainError` (código no formato `área.nome`, tipo e campos). O endpoint converte com
`ToHttpResult()` e o hub com `ToHubResult()`. Exceção fica para bug.

## Consequências

- Os erros viram ProblemDetails previsíveis na API e `HubResult` no SignalR.
- Os testes de domínio são testes de unidade simples, sem servidor.
